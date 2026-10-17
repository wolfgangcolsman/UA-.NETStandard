# ZONTAL-patched fork of UA-.NETStandard

This repository is a fork of the
[OPC Foundation UA-.NETStandard](https://github.com/OPCFoundation/UA-.NETStandard)
stack, maintained by ZONTAL. It is **not** an official OPC Foundation release.

- Base: OPC Foundation tag `2.0.0`
  (commit `bc5da1006931c695a43b0f6d349f70874b0f1407`).
- Packages: the package ids are unchanged (`OPCFoundation.NetStandard.Opc.Ua.*`);
  every package built from this fork carries the version `2.0.0-zontal.2`.
- Licence: the OPC Foundation MIT License 1.00 ([LICENSE.txt](LICENSE.txt))
  applies to this fork unchanged. All copyright notices of the OPC Foundation
  and of other contributors are retained; the patches are contributed under
  the same licence.
- Assemblies are **not strong-name signed** and **not Authenticode signed**:
  the OPC Foundation keys are not available to this fork. See
  [Strong naming](#strong-naming).
- Dependency: the Raft engine packages `RaftCs`, `RaftCs.Transport`,
  `RaftCs.Transport.NanoMsg`, and `RaftCs.Storage.File` are ZONTAL's patched
  `1.2.0-zontal.3` from github.com/zontal/raft-cs (upstream
  github.com/marcschier/raft-cs `v1.1.3` with the committed-entry-index,
  compaction and snapshot patches; same package ids and strong-name identity
  as `RaftCs` 1.1.x; see the NOTICE.md of that repository).
  [NuGet.config](NuGet.config) takes them from the static feed
  https://zontal.github.io/nuget-feed/index.json, which needs no credentials.

## Structure

Every patch is a single commit on its own branch `zontal/2.0.0-patches/<name>`,
created from tag `2.0.0`, with these exceptions: patch 7b is created from
patch 7, whose seam it completes, and patch 8c is created from patch 8c′, on
which it builds. The release branch `zontal/2.0.0` is tag `2.0.0` with all
patch branches merged, plus the fork's own files (this notice, the README
section, version.json, and the `zontal-pack` and Copilot review workflows);
patch 7b carries the RaftCs versions in Directory.Packages.props and the
static feed in NuGet.config. The tag `2.0.0-zontal.2` marks the
packaged state; the earlier tag `2.0.0-zontal.1` stays reachable from the
release branch.

Patches that were proposed upstream are backports of the **reviewed** upstream
revision: for a merged pull request the squash commit on upstream `master`, for
an open one the net change of its head against `master`
(`git diff $(git merge-base master <head>) <head>`). Upstream `master` changed
since `2.0.0`; a backport carries only the intent of the pull request on the
`2.0.0` code and none of the unrelated `master` changes. The commit message of
each backport names its source commit and every adaptation. The `fix/<name>`
branches of this repository are the heads of the upstream pull requests and are
not part of the release; the `zontal/2.0.0-patches/<name>` branches are their
backports.

## Patches

| # | Branch (`zontal/2.0.0-patches/…`) | Commit | Change | Upstream | Backported from |
|---|--------|--------|--------|----------|-----------------|
| 1 | `sourcegen-nodeset-variable-without-value` | `203f8faff` | Source generator: a Variable or VariableType imported from a NodeSet2 without a `Value` element gets a null Value instead of the default value of its DataType (OPC 10000-6 F.5), and does not inherit the Value of its TypeDefinition, supertype, or the InstanceDeclaration it overrides. ModelDesign inputs are unchanged. | [#4798](https://github.com/OPCFoundation/UA-.NETStandard/pull/4798), open (head `fix/sourcegen-nodeset-variable-without-value`) | net change of `12355115c` |
| 2 | `sourcegen-external-reference-targets` | `d31284645` | Source generator: a NodeSet2 reference to a node of another model is kept when that model comes from a referenced assembly (whose dependency metadata carries types only), e.g. a device organized by DI's `DeviceSet`. It used to be dropped without a diagnostic. | [#4796](https://github.com/OPCFoundation/UA-.NETStandard/pull/4796), merged | `2de64d7d9` |
| 3 | `sourcegen-nodeset-address-space-only` | `0e12e5e47` | Source generator: new option `ModelSourceGeneratorEmitNodeSetNodesOnly` (`GeneratorOptions.EmitNodeSetNodesOnly`), off by default. When on, the predefined nodes generated for a NodeSet2 input are exactly the NodeSet2's own nodes. Typed state classes and `CreateInstanceOf*` factories are unchanged. | [#4797](https://github.com/OPCFoundation/UA-.NETStandard/pull/4797), merged | `716858aa7` |
| 4 | `pkcs11-store-add-delete` | `b1cc29730` | `Pkcs11CertificateStore.AddAsync` writes the certificate object to the token (linked by CKA_ID to the key it belongs to; a private key is never written) instead of silently dropping it; `DeleteAsync` destroys the certificate objects with the thumbprint (keys stay on the token) instead of always throwing. | [#4792](https://github.com/OPCFoundation/UA-.NETStandard/pull/4792), merged; follow-up [#4826](https://github.com/OPCFoundation/UA-.NETStandard/pull/4826), open | `1b7db89f2` and `820ba8a3c` |
| 5 | `pubsub-published-datasets-parent` | `9703d8578` | `PubSubNodeManager` places PublishedDataSet and DataSetFolder objects under `PublishSubscribe.PublishedDataSets` (i=17371) instead of under `DataSetFolderType`'s `<DataSetFolderName>` InstanceDeclaration (i=14478), and binds the folder methods on that instance. | [#4793](https://github.com/OPCFoundation/UA-.NETStandard/pull/4793), merged | `12a76ade5` |
| 6 | `server-subscription-restore-errors` | `08635421d` | `SubscriptionManager.RestoreSubscriptionsAsync` surfaces a failed restore of durable subscriptions instead of logging and dropping it, and rolls back what it restored. `SubscriptionManager.ContinueOnRestoreFailure` restores the old behaviour. | [#4795](https://github.com/OPCFoundation/UA-.NETStandard/pull/4795), open (head `fix/server-subscription-restore-errors`) | net change of `d145a281a` |
| 7 | `raft-kv-store-snapshot` | `2153cff90` | `IRaftSnapshotManager` (implemented by `RaftSharedKeyValueStore` and `HybridSharedKeyValueStore`, registered by the redundancy builders): `CreateSnapshotAsync` captures the store at a barrier, `RestoreSnapshotAsync` restores it before the store applies the log. The store's half of the snapshot and compaction seam. | [#4794](https://github.com/OPCFoundation/UA-.NETStandard/pull/4794), merged | `4e77ba3f8` |
| 7b | `raft-engine-snapshot` (from 7) | `e28a5d4f0` | The engine half of patch 7, on RaftCs `1.2.0-zontal.3`: `IRaftSnapshotConsensus` (committed items with log indices and snapshots, `CompactAsync`), implemented by `DefaultRaftConsensus`. Over it, `RaftSharedKeyValueStore` tracks `AppliedIndex`, replaces its state with a snapshot the engine delivers (a lagging replica installing the leader's snapshot, or the first item after a restart) and publishes the differences to watchers, and with a compaction interval snapshots its state into the engine, which discards the log behind it. | Not proposed: held back until an upstream RaftCs release provides the snapshot API it uses. | — |
| 8a | `trustlist-validate-public` | `25ac1ebe1` | `TrustList.ValidateNewTrustListAsync` is public. | [#4799](https://github.com/OPCFoundation/UA-.NETStandard/pull/4799), merged | `9746c62b4` |
| 8b | `client-uafileinfo-public-constructor` | `5d0d06993` | Public `UaFileInfo` constructors, so a `FileSystemClient` can address a file by its NodeId, inside a known directory or detached. | [#4790](https://github.com/OPCFoundation/UA-.NETStandard/pull/4790), merged | `d40843c23` |
| 8c′ | `reconnect-endpoint-security` | `45f6d1630` | Reconnect and failover keep the message-security mode and policy of the endpoint in use: a refresh or failover target that would change them fails the attempt instead of selecting another security configuration. Prerequisite of 8c. | [#4809](https://github.com/OPCFoundation/UA-.NETStandard/pull/4809), merged | `1f6700c30` |
| 8c | `managed-session-rediscovery-validation` (from 8c′) | `8345d9770` | `RediscoveredEndpointValidator` (`ManagedSessionOptions.ValidateRediscoveredEndpoint`, `ManagedSessionBuilder.WithRediscoveredEndpointValidator`, or registered for dependency injection): accepts or rejects the endpoint a reconnect rediscovers, on a detached copy, before it replaces the endpoint in use. Each `ManagedSession` works on its own copy of its endpoint. | [#4791](https://github.com/OPCFoundation/UA-.NETStandard/pull/4791), open (head `fix/managed-session-rediscovery-validation`) | net change of `4279d6d47` |

Hunks of the upstream changes that do not apply to `2.0.0`, because the files
were added on `master` after `2.0.0`:

- #4798: `docs/Extrusion.md`, `src/Opc.Ua.Extrusion.Server/*` (the explicit
  `MaterialDensityUnit` of the Extrusion server) and `tests/Opc.Ua.Extrusion.Tests/*`.
  No companion or consumer that exists at `2.0.0` relied on a Value inherited
  from a TypeDefinition. The generated code of every source-generated model of
  `2.0.0` was compared with and without the change: the Variables and
  VariableTypes their NodeSet2 declares without a Value now start out null.
  The tests of the companion models and consumers pass with it:
  AI, AMB, DI, ISA-95, Machinery, OpenUSD, Positioning,
  Pumps, Robotics, Scales, Vision, Visual Inspection, WoT Connectivity,
  xRegistry, the information model, the server and complex type tests, and the
  Redundancy samples.
- #4791: `tests/Opc.Ua.Tools.Tests/Mcp/Core/McpGenericGenerationTestContext.cs`
  and `tests/Opc.Ua.Tools.Tests/Mcp/Gds/GdsManagedSession.cs`.

## Changes since 2.0.0-zontal.1

`2.0.0-zontal.1` contained the first revision of each patch; `2.0.0-zontal.2`
contains the revisions after upstream review, plus patch 8c′, and RaftCs
`1.2.0-zontal.3` instead of `1.2.0-zontal.1`. Public members that
`2.0.0-zontal.1` added and that the review changed are listed as **API**; code
written against `2.0.0` is not affected, code written against those
`2.0.0-zontal.1` additions may need changes.

- **1 (NodeSet2 Value):** a NodeSet2 Variable or VariableType without a Value
  no longer inherits the Value of its TypeDefinition, supertype, or the
  InstanceDeclaration it overrides (`2.0.0-zontal.1` only stopped implying the
  DataType default). The dependency metadata of a generated assembly carries the
  flag, so models that reference it see the same null Value; older readers
  ignore the flag. `Opc.Ua.Robotics.Server` now initializes the intent's
  `Result` and `CurrentPose`, which its model declares without a Value.
- **2 (external reference targets):** the NodeId of a reference target in a
  namespace the consuming model does not declare is emitted with that
  namespace URI as an escaped string literal, and two external targets whose
  identifiers map to the same name (`i=5001` and `s=5001`) get distinct
  stand-ins instead of being merged into one.
- **3 (NodeSet2 nodes only):** an undeclared child is left out only of the
  predefined nodes of a NodeSet2 instance; the factories of a type still create
  it for a runtime instance, and references to omitted nodes are dropped.
  **API:** the `IModelDesign.IsSourceNodeSet` member of `2.0.0-zontal.1` is
  removed; the generator reads the provenance internally.
- **4 (PKCS#11):** a certificate is linked to a key only when the key algorithm,
  its parameters (the EC curve) and the key value match; RSA keys also match on
  the public exponent. A certificate without a CKA_ID is paired with the key
  that carries its public key; a key selected only by the configured object
  label or id must first sign a challenge that the certificate verifies, and
  without such a filter no key is attached (`2.0.0` attached the first key of
  the token).
- **5 (PubSub PublishedDataSets):** the Optional `DataSetFolderType` methods of
  `PublishSubscribe.PublishedDataSets` (i=17372 … i=17404) are materialized
  with their standard instance NodeIds and bound; the type declaration's
  methods are no longer bound. `AddPublishedEvents` returns its outputs in the
  declared order (ConfigurationVersion, DataSetNodeId).
- **6 (subscription restore):** a failed restore is rolled back, so a retry
  starts from the stored state. Monitored item queues that implement the new
  `IRestorableMonitoredItemQueue` are restored in a staged step and written to
  storage only after every subscription was restored; the mirroring queues of
  `Opc.Ua.Redundancy.Server` implement it; the DurableSubscription quickstart
  keeps its stored files until the restore completes. A restored event item
  that its node manager rejects is disposed.
- **7 (store snapshot):** **API:** `RaftSharedKeyValueStore.CreateSnapshotAsync`
  returns a `RaftKeyValueSnapshot` (position `LastIncludedCommand` and `Data`)
  instead of the encoded bytes, and `RestoreSnapshot` is replaced by
  `RestoreSnapshotAsync(snapshot, RaftSnapshotLogReplay, ct)`, which must be
  told whether the consensus replays the log from the first command. The
  snapshot encoding carries the command position and is decoded strictly. Both
  are also reachable as `IRaftSnapshotManager`, which the redundancy builders
  register.
- **7b (engine snapshot):** **API:** the compaction interval moved from an
  optional parameter of the existing `RaftSharedKeyValueStore` constructor to a
  new overload `RaftSharedKeyValueStore(consensus, ownsConsensus, commitTimeout,
  compactionInterval)`, so the `2.0.0` constructor is unchanged; code compiled
  against `2.0.0-zontal.1` binds to the new overload. The engine snapshot now
  carries patch 7's encoding with the command position behind a format marker,
  so the commands after it keep their positions on every replica. A snapshot
  written by the compaction of `2.0.0-zontal.1` is refused with a
  `FormatException` (the store's applier stops) instead of being misread; such
  a replica's Redundancy store must be rebuilt (deleted and resynchronized from
  the cluster). `DefaultRaftConsensus.CompactAsync` no longer waits for the
  applied index (RaftCs `1.2.0-zontal.3` advances it before it queues the
  commands).
- **8a (TrustList):** **API:** the public method takes `ArrayOf<Certificate>` and
  `ArrayOf<X509CRL>` lists instead of certificate and CRL collections, and it
  also validates the certificates retained from stores that the TrustList does
  not replace.
- **8b (UaFileInfo):** **API:** the public constructors are
  `UaFileInfo(UaDirectoryInfo parent, NodeId, QualifiedName)` and
  `UaFileInfo(FileSystemClient owner, NodeId, QualifiedName)` (a detached file,
  which cannot be deleted, moved or copied) instead of the five-parameter
  constructor of `2.0.0-zontal.1`.
- **8c′ and 8c (reconnect):** a refresh or failover target with another security
  mode or policy fails the reconnect attempt (8c′). The rediscovered endpoint is
  validated on a detached copy and published to the shared `ConfiguredEndpoint`
  only after it was accepted; each `ManagedSession` works on its own copy of its
  endpoint. The validator (unchanged `RediscoveredEndpointValidator` delegate
  and `ManagedSession.ValidateRediscoveredEndpoint` property) can now also be
  installed before the session connects, through
  `ManagedSessionOptions.ValidateRediscoveredEndpoint`,
  `ManagedSessionBuilder.WithRediscoveredEndpointValidator` or dependency
  injection; it is called after the security mode and policy check and
  receives the endpoint this session last accepted.
- **RaftCs `1.2.0-zontal.3`:** `Committed` skips snapshot items (the stores
  read `CommittedItems`), `CompactAsync` refuses to compact past the latest
  snapshot, a snapshot index must be at least 1, the `Count` of `Committed`
  counts commands only, a snapshot is installed crash-atomically, and the
  membership is stored with the index it reflects (`IRaftConfStateIndexStorage`).
- **Packages:** `OPCFoundation.NetStandard.Opc.Ua.Redundancy.Client` is published
  as well, because patch 7 registers the snapshot manager in its builder.
- **On-disk format and rollback:** `RaftCs.Storage.File` `1.2.0-zontal.2` and
  later open stores written by `1.2.0-zontal.1`. After such a store was
  compacted, received or created a snapshot, or recorded a membership change, a
  store written by RaftCs `1.2.0-zontal.2` or `1.2.0-zontal.3` cannot be opened
  by `1.2.0-zontal.1`, which refuses it rather than misread it. Rolling an
  application back from OPC UA `2.0.0-zontal.2` to `2.0.0-zontal.1` is
  therefore refused for such Redundancy stores; roll back with a fresh store.

## Known, unpatched limitations

The following behaviours of 2.0.0 are known limitations for ZONTAL's use and
are deliberately **left as they are** in this fork:

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
  nuget.org `2.0.0` assemblies (`PublicKeyToken` of the OPC Foundation key), so
  an assembly compiled against the nuget.org packages does not bind to these
  on .NET Framework; on .NET (Core) binding by name and version works.
- Use all `OPCFoundation.NetStandard.Opc.Ua.*` packages of one application from
  this fork; do not mix them with the nuget.org packages.
- A strong-named consumer on .NET Framework cannot reference these assemblies.
- `InternalsVisibleTo` entries carry no public key, which is consistent with
  unsigned assemblies.
