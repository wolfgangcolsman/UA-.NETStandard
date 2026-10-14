/* ========================================================================
 * Copyright (c) 2005-2026 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

// CA2007: tests run without a SynchronizationContext; ConfigureAwait(false)
// adds noise without a behavioural benefit. Disabled file-level for the suite.
#pragma warning disable CA2007

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Opc.Ua.Redundancy;
using Raft;
using Raft.Configuration;
using Raft.Storage;
using Raft.Storage.File;
using Raft.Transport;

namespace Opc.Ua.Server.Tests.Redundancy
{
    /// <summary>
    /// A <see cref="RaftSharedKeyValueStore"/> over the RaftCs engine bounds the Raft log: it snapshots its state
    /// into the engine, which discards the log behind the snapshot, sends the snapshot to a lagging replica, and
    /// hands it back to the store after a restart, followed by the log after it.
    /// </summary>
    [TestFixture]
    [Category("Distributed")]
    public sealed class RaftSharedKeyValueStoreCompactionTests
    {
        [Test]
        public async Task CompactionBoundsTheRaftLogAsync()
        {
            await using var network = new InMemoryNetwork();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var storage = new MemoryStorage(new ConfState(new ulong[] { 1 }));
            var store = new RaftSharedKeyValueStore(
                CreateSingleNode(network, storage), ownsConsensus: true, compactionInterval: 10);
            try
            {
                for (int ii = 0; ii < 35; ii++)
                {
                    await store.SetAsync(Key(ii), Value(ii), cts.Token);
                }

                // The leader's empty entry is index 1, so the 30th command is index 31.
                await WaitUntilAsync(() => storage.FirstIndex() > 31, cts.Token);
                Snapshot snapshot = storage.LatestSnapshot();
                Assert.That(snapshot, Is.Not.Null);
                Assert.That(storage.FirstIndex(), Is.EqualTo(snapshot.Metadata.Index + 1));
                Assert.That(store.AppliedIndex, Is.GreaterThanOrEqualTo(36UL));
                for (int ii = 0; ii < 35; ii++)
                {
                    (bool found, ByteString value) = await store.TryGetAsync(Key(ii), cts.Token);
                    Assert.That(found, Is.True);
                    Assert.That(value.ToArray(), Is.EqualTo(Value(ii).ToArray()));
                }
            }
            finally
            {
                await store.DisposeAsync();
            }
        }

        [Test]
        public async Task LaggingReplicaInstallsTheLeaderSnapshotAsync()
        {
            await using var network = new InMemoryNetwork();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            ulong[] ids = [1, 2, 3];
            var storages = new MemoryStorage[ids.Length];
            var nodes = new DefaultRaftConsensus[ids.Length];
            var stores = new RaftSharedKeyValueStore[ids.Length];
            for (int ii = 0; ii < ids.Length; ii++)
            {
                storages[ii] = new MemoryStorage(new ConfState(ids));
                nodes[ii] = DefaultRaftConsensus.CreateCluster(
                    ids[ii],
                    network.CreateNode(ids[ii]),
                    storages[ii],
                    new RaftNodeOptions { TickInterval = TimeSpan.FromMilliseconds(10) },
                    config =>
                    {
                        config.ElectionTick = 10;
                        config.HeartbeatTick = 1;
                        config.PreVote = true;
                        config.RandomizedElectionTimeout = (int)(ids[ii] * 6);
                    },
                    readyTimeout: TimeSpan.FromSeconds(25));
            }

            try
            {
                await Task.WhenAll(Array.ConvertAll(nodes, n => n.StartAsync(cts.Token).AsTask()));
                int leader = await WaitForLeaderAsync(nodes, cts.Token);
                int lagging = leader == ids.Length - 1 ? 0 : ids.Length - 1;
                for (int ii = 0; ii < ids.Length; ii++)
                {
                    // The lagging replica never compacts, so a snapshot in its storage is the leader's.
                    stores[ii] = new RaftSharedKeyValueStore(
                        nodes[ii],
                        ownsConsensus: false,
                        commitTimeout: TimeSpan.FromSeconds(20),
                        compactionInterval: ii == lagging ? 0 : 5);
                }

                // Watch the lagging replica; a change it applies itself proves the watcher is registered.
                var expected = new HashSet<string>();
                for (int ii = 0; ii < 20; ii++)
                {
                    expected.Add(Key(ii));
                }
                var watched = new HashSet<string>();
                var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task watch = WatchAsync(stores[lagging], watched, expected, ready, cts.Token);
                for (int ii = 0; !ready.Task.IsCompleted; ii++)
                {
                    await stores[lagging].SetAsync("k-ready", Value(ii), cts.Token);
                    await Task.WhenAny(ready.Task, Task.Delay(100, cts.Token));
                }

                // Cut the replica off, commit on the other two, and let them compact behind it.
                network.SetPartition([ids[lagging]]);
                for (int ii = 0; ii < 20; ii++)
                {
                    await stores[leader].SetAsync(Key(ii), Value(ii), cts.Token);
                }
                await WaitUntilAsync(() => storages[leader].FirstIndex() > 20, cts.Token);
                network.Heal();

                for (int ii = 0; ii < 20; ii++)
                {
                    ByteString value = await WaitForValueAsync(stores[lagging], Key(ii), cts.Token);
                    Assert.That(value.ToArray(), Is.EqualTo(Value(ii).ToArray()));
                }
                Assert.That(storages[lagging].LatestSnapshot(), Is.Not.Null);
                Assert.That(stores[lagging].AppliedIndex, Is.GreaterThan(storages[lagging].LatestSnapshot().Metadata.Index));

                // The watcher of the lagging replica sees every key the snapshot brought in.
                await watch;
                Assert.That(watched.IsSupersetOf(expected), Is.True);
            }
            finally
            {
                foreach (RaftSharedKeyValueStore store in stores)
                {
                    if (store != null)
                    {
                        await store.DisposeAsync();
                    }
                }
                foreach (DefaultRaftConsensus node in nodes)
                {
                    await node.DisposeAsync();
                }
            }
        }

        [Test]
        public async Task RestartRestoresTheStateFromTheSnapshotAndTheLogAsync()
        {
            string directory = Path.Combine(Path.GetTempPath(), "raft-kv-" + Guid.NewGuid().ToString("N"));
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try
            {
                await using (var network = new InMemoryNetwork())
                {
                    using var storage = new FileRaftStorage(new FileRaftStorageOptions(directory) { Fsync = false });
                    storage.SetConfState(new ConfState(new ulong[] { 1 }));
                    await using var store = new RaftSharedKeyValueStore(
                        CreateSingleNode(network, storage), ownsConsensus: true, compactionInterval: 5);
                    for (int ii = 0; ii < 12; ii++)
                    {
                        await store.SetAsync(Key(ii), Value(ii), cts.Token);
                    }

                    // The 10th command is index 11: the snapshot covers it, the log holds the rest.
                    await WaitUntilAsync(() => storage.FirstIndex() > 11, cts.Token);
                }

                await using (var network = new InMemoryNetwork())
                {
                    using var storage = new FileRaftStorage(new FileRaftStorageOptions(directory) { Fsync = false });
                    Assert.That(storage.LatestSnapshot(), Is.Not.Null);
                    Assert.That(storage.FirstIndex(), Is.GreaterThan(11UL));
                    await using var store = new RaftSharedKeyValueStore(
                        CreateSingleNode(network, storage), ownsConsensus: true, compactionInterval: 5);
                    for (int ii = 0; ii < 12; ii++)
                    {
                        (bool found, ByteString value) = await store.TryGetAsync(Key(ii), cts.Token);
                        Assert.That(found, Is.True, Key(ii));
                        Assert.That(value.ToArray(), Is.EqualTo(Value(ii).ToArray()));
                    }
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }

        [Test]
        public void CompactionNeedsASnapshottingConsensus()
        {
            Assert.That(
                () => new RaftSharedKeyValueStore(new InProcessRaftConsensus(), compactionInterval: 5),
                Throws.ArgumentException);
        }

        private static DefaultRaftConsensus CreateSingleNode(InMemoryNetwork network, IRaftWritableStorage storage)
        {
            return DefaultRaftConsensus.CreateCluster(
                1,
                network.CreateNode(1),
                storage,
                new RaftNodeOptions { TickInterval = TimeSpan.FromMilliseconds(5) },
                config => config.ElectionTick = 3);
        }

        private static string Key(int ii)
        {
            return "k" + ii.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static ByteString Value(int ii)
        {
            return ByteString.From(new byte[] { (byte)ii, (byte)(ii + 1) });
        }

        private static async Task WatchAsync(
            RaftSharedKeyValueStore store,
            HashSet<string> keys,
            HashSet<string> expected,
            TaskCompletionSource<bool> ready,
            CancellationToken ct)
        {
            await foreach (KeyValueChange change in store.WatchAsync("k", ct))
            {
                if (change.Kind == KeyValueChangeKind.Set)
                {
                    keys.Add(change.Key);
                    ready.TrySetResult(true);
                }
                if (keys.IsSupersetOf(expected))
                {
                    return;
                }
            }
        }

        private static async Task<int> WaitForLeaderAsync(DefaultRaftConsensus[] nodes, CancellationToken ct)
        {
            while (true)
            {
                for (int ii = 0; ii < nodes.Length; ii++)
                {
                    if (nodes[ii].IsLeader)
                    {
                        return ii;
                    }
                }
                await Task.Delay(20, ct);
            }
        }

        private static async Task<ByteString> WaitForValueAsync(
            RaftSharedKeyValueStore store,
            string key,
            CancellationToken ct)
        {
            while (true)
            {
                (bool found, ByteString value) = await store.TryGetAsync(key, ct);
                if (found)
                {
                    return value;
                }
                await Task.Delay(10, ct);
            }
        }

        private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken ct)
        {
            while (!condition())
            {
                await Task.Delay(10, ct);
            }
        }
    }
}