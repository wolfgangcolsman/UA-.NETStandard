# ZONTAL-patched fork of UA-.NETStandard

This repository is a fork of the
[OPC Foundation UA-.NETStandard](https://github.com/OPCFoundation/UA-.NETStandard)
stack, maintained by ZONTAL. It is **not** an official OPC Foundation release.

- Base: OPC Foundation tag `2.0.1`
  (commit `07e62f72500cc0923a2bab940653c8c1116fa286`).
- Packages: the package ids are unchanged (`OPCFoundation.NetStandard.Opc.Ua.*`);
  every package built from this fork carries the version `2.0.1-zontal.1`.
- Licence: the OPC Foundation MIT License 1.00 ([LICENSE.txt](LICENSE.txt))
  applies to this fork unchanged. All copyright notices of the OPC Foundation
  and of other contributors are retained; the patches are contributed under
  the same licence.
- Assemblies are **not strong-name signed** and **not Authenticode signed**:
  the OPC Foundation keys are not available to this fork. See
  [Strong naming](#strong-naming).
- Dependency: the Raft engine packages `RaftCs`, `RaftCs.Transport`,
  `RaftCs.Transport.NanoMsg`, and `RaftCs.Storage.File` are the upstream
  release `1.1.4` of github.com/marcschier/raft-cs from nuget.org, like every
  other dependency; [NuGet.config](NuGet.config) is the one of tag `2.0.1`.

## Structure

Every patch is a single commit on its own branch `zontal/2.0.1-patches/<name>`,
created from tag `2.0.1`, with these exceptions: patch 7b is created from
patch 7, whose seam it completes, and patch 8c is created from patch 8c′, on
which it builds. The release branch `zontal/2.0.1` is tag `2.0.1` with all
patch branches merged, plus the fork's own files (this notice, the README
section, version.json, and the `zontal-pack` and Copilot review workflows);
patch 7b carries the RaftCs version in Directory.Packages.props. The tag
`2.0.1-zontal.1` marks the packaged state.

Patches that were merged upstream are backports of their squash commits on
upstream `master`; a patch whose pull request was open when it was backported
carries the net change of the reviewed pull request head
(`git diff $(git merge-base master <head>) <head>`). Upstream `master` changed
since `2.0.1`; a backport carries only the intent of the pull request on the
`2.0.1` code and none of the unrelated `master` changes. The commit message of
each backport names its source commit and every adaptation. The `fix/<name>`
branches of this repository are the heads of open upstream pull requests and
are not part of the release; the `zontal/2.0.1-patches/<name>` branches are
their backports.

## Patches

| # | Branch (`zontal/2.0.1-patches/…`) | Commit | Change | Upstream | Backported from |
|---|--------|--------|--------|----------|-----------------|
| 1 | `sourcegen-nodeset-variable-without-value` | `d749be419` | Source generator: a Variable or VariableType imported from a NodeSet2 without a `Value` element gets a null Value instead of the default value of its DataType (OPC 10000-6 F.5), and does not inherit the Value of its TypeDefinition, supertype, or the InstanceDeclaration it overrides. ModelDesign inputs are unchanged. The Robotics and Vision servers publish values for the properties their models declare without a Value. | [#4798](https://github.com/OPCFoundation/UA-.NETStandard/pull/4798), merged | `fc0687a2d` |
| 2 | `sourcegen-external-reference-targets` | `c22a0d4a2` | Source generator: a NodeSet2 reference to a node of another model is kept when that model comes from a referenced assembly (whose dependency metadata carries types only), e.g. a device organized by DI's `DeviceSet`. It used to be dropped without a diagnostic. | [#4796](https://github.com/OPCFoundation/UA-.NETStandard/pull/4796), merged | `2de64d7d9` |
| 3 | `sourcegen-nodeset-address-space-only` | `fb89a7901` | Source generator: new option `ModelSourceGeneratorEmitNodeSetNodesOnly` (`GeneratorOptions.EmitNodeSetNodesOnly`), off by default. When on, the predefined nodes generated for a NodeSet2 input are exactly the NodeSet2's own nodes. Typed state classes and `CreateInstanceOf*` factories are unchanged. | [#4797](https://github.com/OPCFoundation/UA-.NETStandard/pull/4797), merged | `716858aa7` |
| 4 | `pkcs11-store-add-delete` | `d6c3444a4` | `Pkcs11CertificateStore.AddAsync` writes the certificate object to the token (linked by CKA_ID to the key it belongs to; a private key is never written) instead of silently dropping it; `DeleteAsync` destroys the certificate objects with the thumbprint (keys stay on the token) instead of always throwing. A certificate without a CKA_ID is attached only to a key that signs a challenge the certificate verifies. | [#4792](https://github.com/OPCFoundation/UA-.NETStandard/pull/4792), merged; follow-up [#4826](https://github.com/OPCFoundation/UA-.NETStandard/pull/4826), open | `1b7db89f2` and the net change of `56135b189` (#4826) |
| 5 | `pubsub-published-datasets-parent` | `ff32edb2b` | `PubSubNodeManager` places PublishedDataSet and DataSetFolder objects under `PublishSubscribe.PublishedDataSets` (i=17371) instead of under `DataSetFolderType`'s `<DataSetFolderName>` InstanceDeclaration (i=14478), and binds the folder methods on that instance. | [#4793](https://github.com/OPCFoundation/UA-.NETStandard/pull/4793), merged | `12a76ade5` |
| 6 | `server-subscription-restore-errors` | `65facdfe8` | `SubscriptionManager.RestoreSubscriptionsAsync` surfaces a failed restore of durable subscriptions instead of logging and dropping it, and rolls back what it restored. `SubscriptionManager.ContinueOnRestoreFailure` restores the old behaviour. A restored event item that its node manager rejects is disposed. | [#4795](https://github.com/OPCFoundation/UA-.NETStandard/pull/4795) and [#4828](https://github.com/OPCFoundation/UA-.NETStandard/pull/4828), merged | net change of `d145a281a` (`768b39e1b` and `4a7dfe741`) |
| 7 | `raft-kv-store-snapshot` | `ef04f07ac` | `IRaftSnapshotManager` (implemented by `RaftSharedKeyValueStore` and `HybridSharedKeyValueStore`, registered by the redundancy builders): `CreateSnapshotAsync` captures the store at a barrier, `RestoreSnapshotAsync` restores it before the store applies the log. The store's half of the snapshot and compaction seam. | [#4794](https://github.com/OPCFoundation/UA-.NETStandard/pull/4794), merged | `4e77ba3f8` |
| 7b | `raft-engine-snapshot` (from 7) | `c01be180d` | The engine half of patch 7, on RaftCs `1.1.4`: `IRaftSnapshotConsensus` (committed items with log indices and snapshots, `CompactAsync`), implemented by `DefaultRaftConsensus`. Over it, `RaftSharedKeyValueStore` tracks `AppliedIndex`, replaces its state with a snapshot the engine delivers (a lagging replica installing the leader's snapshot, or the first item after a restart) and publishes the differences to watchers, and with a compaction interval snapshots its state into the engine, which discards the log behind it. | Proposed as drafts [#4835](https://github.com/OPCFoundation/UA-.NETStandard/pull/4835) (RaftCs 1.1.4) and [#4836](https://github.com/OPCFoundation/UA-.NETStandard/pull/4836); this patch is the revision of `2.0.0-zontal.2`, not the later revision of #4836. | — |
| 8a | `trustlist-validate-public` | `64aa1c108` | `TrustList.ValidateNewTrustListAsync` is public. | [#4799](https://github.com/OPCFoundation/UA-.NETStandard/pull/4799), merged | `9746c62b4` |
| 8b | `client-uafileinfo-public-constructor` | `b6ebdf17c` | Public `UaFileInfo` constructors, so a `FileSystemClient` can address a file by its NodeId, inside a known directory or detached. | [#4790](https://github.com/OPCFoundation/UA-.NETStandard/pull/4790), merged | `d40843c23` |
| 8c′ | `reconnect-endpoint-security` | `51771853d` | Reconnect and failover keep the message-security mode and policy of the endpoint in use: a refresh or failover target that would change them fails the attempt instead of selecting another security configuration. Prerequisite of 8c. | [#4809](https://github.com/OPCFoundation/UA-.NETStandard/pull/4809), merged | `1f6700c30` |
| 8c | `managed-session-rediscovery-validation` (from 8c′) | `c35816899` | `RediscoveredEndpointValidator` (`ManagedSessionOptions.ValidateRediscoveredEndpoint`, `ManagedSessionBuilder.WithRediscoveredEndpointValidator`, or registered for dependency injection): accepts or rejects the endpoint a reconnect rediscovers, on a detached copy, before it replaces the endpoint in use. Each `ManagedSession` works on its own copy of its endpoint. | [#4791](https://github.com/OPCFoundation/UA-.NETStandard/pull/4791), merged | net change of `4279d6d47` (equal to `844135190` outside the adapted files) |

Hunks of the upstream changes that do not apply to `2.0.1`, because the files
or the code they change were added on `master` after `2.0.1`:

- #4798: `docs/Extrusion.md`, `src/Opc.Ua.Extrusion.Server/*` and
  `tests/Opc.Ua.Extrusion.Tests/*` (the Extrusion companion); the dependency
  flag in `ModelDependencyV1.cs` is added by hand, because `master`
  reorganized the child serialization.
- #4795: `Subscription.RestoreMonitoredItemsAsync` keeps `2.0.1`'s item
  registration (`master`'s `OnMonitoredItemAdded` hook).
- #4809: `docs/Sessions.md` takes only the paragraph on `UpdateBeforeConnect`;
  the section on generation-bound client requests that follows it on `master`
  belongs to another change.
- #4791: `tests/Opc.Ua.Tools.Tests/Mcp/Core/McpGenericGenerationTestContext.cs`
  and `tests/Opc.Ua.Tools.Tests/Mcp/Gds/GdsManagedSession.cs`; in
  `ManagedSession.cs` the connect path assigns `m_session` where `master` calls
  `SetBindingSession`, and the logger event `ManagedSession + 31` is added
  without `master`'s `+ 29` and `+ 30`.

## Changes since 2.0.0-zontal.2

`2.0.0-zontal.2` is described by the NOTICE.md at its tag, including its
changes since `2.0.0-zontal.1`.

- **Base:** OPC Foundation tag `2.0.1` instead of `2.0.0`. Code that uses the
  APIs the patches add is not affected; the changes of `2.0.1` itself are the
  OPC Foundation's (see its release notes).
- **RaftCs `1.1.4`** from nuget.org instead of ZONTAL's `1.2.0-zontal.3`. The
  upstream release carries the committed-entry-index, compaction and snapshot
  changes that `1.2.0-zontal.3` carried (marcschier/raft-cs#9, #10 and #11),
  with the same public API, the same assembly identity (`Raft` `1.1.0.0`, same
  strong-name key) and the same storage format; it also fails queued requests
  when the node stops. The ZONTAL NuGet feed is no longer needed to build this
  repository.
- **1 (NodeSet2 Value):** the backport of the merged pull request; compared
  with `2.0.0-zontal.2` it adds the Vision server's values for the mandatory
  Perception properties its model declares without a Value
  (`VisionMandatoryProperties`).
- **2, 3, 7, 8c′:** the upstream squash commits apply to `2.0.1` unchanged
  (8c′ apart from the documentation hunk listed above); the adaptations the
  `2.0.0` backports needed (nullable annotations, context) are gone.
- **6 (subscription restore):** `MonitoredItem` keeps the shared default
  `DataChangeFilter` that `2.0.1` contains, and the tests take `master`'s
  nullable annotations, because `2.0.1` enables nullable reference types for
  every project.
- **7b (engine snapshot):** the compaction tests take nullable annotations, as
  in #4836.
- **4 (PKCS#11):** the follow-up #4826 at its current head instead of its first
  revision: a certificate without a CKA_ID is attached only to a key that signs
  a challenge the certificate verifies, trying every key with the inferred
  CKA_ID and then the keys the configured object label or id selects (RSA falls
  back to PSS when the token refuses PKCS#1 v1.5), and an RSA key whose modulus
  the token stores with leading zero bytes is found.
- **5, 8a, 8b, 8c:** unchanged.
- **On-disk format and rollback:** `RaftCs.Storage.File` `1.1.4` writes the
  record kinds and layout of `1.2.0-zontal.2` and `1.2.0-zontal.3`, so a
  Redundancy store moves between `2.0.0-zontal.2` and `2.0.1-zontal.1` in both
  directions. A store written by `1.2.0-zontal.2` or later still cannot be
  opened by `1.2.0-zontal.1` (`2.0.0-zontal.1`).

## Verification

Before the release, the test projects of every patched area ran in full on
`net10.0` (Windows, .NET SDK 10.0.302) against the release state:

| Test project | Passed | Skipped |
|---|---|---|
| `Opc.Ua.SourceGeneration.Core.Tests` | 4283 | 8 |
| `Opc.Ua.SourceGeneration.Tests` | 270 | 0 |
| `Opc.Ua.Di.Tests` | 460 | 0 |
| `Opc.Ua.Security.Pkcs11.Tests` | 120 | 10 (need a SoftHSM2 token) |
| `Opc.Ua.PubSub.Server.Tests` | 187 | 0 |
| `Opc.Ua.Server.Tests` | 8472 | 5 |
| `Opc.Ua.Client.Tests` | 2808 | 0 |
| `Opc.Ua.Redundancy.Server.Tests` | 931 | 0 |
| `Opc.Ua.Redundancy.Client.Tests` | 134 | 0 |
| `Opc.Ua.Redundancy.Kubernetes.Tests` | 103 | 0 |
| `Opc.Ua.Redundancy.PubSub.Tests` | 45 | 0 |
| `Opc.Ua.Vision.Tests` | 485 | 1 |
| `Opc.Ua.Robotics.Tests` | 661 | 0 |

No test failed. The `zontal-pack` workflow runs the tests of the patches again
on Linux, the PKCS#11 tests against a SoftHSM2 token, and packs every package
for all its target frameworks.

## Known, unpatched limitations

The following behaviours were recorded as known limitations for ZONTAL's use
against `2.0.0` and are deliberately **left as they are** in this fork; they
have not been re-assessed against the changes of `2.0.1`:

- the lockout constants;
- the secure channel lockout;
- the cap on subscriptions per session;
- `DistributedSessionManager` being sealed, and the `SessionId` it assigns;
- the registry that `CertificateStoreIdentifier.OpenStore` resolves store
  types through;
- `HistoryClient` accepting history pages with an Uncertain status.

## Strong naming

The OPC Foundation builds its packages with its strong-name key
(`OPCFOUNDATION_NETSTANDARD_SNK_BASE64`) and Authenticode-signs them. That key
is not available to this fork, and the packages are built **without**
`SignAssembly`. Consequences:

- The assemblies have no public key token. Their identity differs from the
  nuget.org `2.0.1` assemblies (`PublicKeyToken` of the OPC Foundation key), so
  an assembly compiled against the nuget.org packages does not bind to these
  on .NET Framework; on .NET (Core) binding by name and version works.
- Use all `OPCFoundation.NetStandard.Opc.Ua.*` packages of one application from
  this fork; do not mix them with the nuget.org packages.
- A strong-named consumer on .NET Framework cannot reference these assemblies.
- `InternalsVisibleTo` entries carry no public key, which is consistent with
  unsigned assemblies.
