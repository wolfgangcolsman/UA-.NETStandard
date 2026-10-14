# ZONTAL-patched fork of UA-.NETStandard

This repository is a fork of the
[OPC Foundation UA-.NETStandard](https://github.com/OPCFoundation/UA-.NETStandard)
stack, maintained by ZONTAL. It is **not** an official OPC Foundation release.

- Base: OPC Foundation tag `2.0.0`
  (commit `bc5da1006931c695a43b0f6d349f70874b0f1407`).
- Packages: the package ids are unchanged (`OPCFoundation.NetStandard.Opc.Ua.*`);
  every package built from this fork carries the version `2.0.0-zontal.1`.
- Licence: the OPC Foundation MIT License 1.00 ([LICENSE.txt](LICENSE.txt))
  applies to this fork unchanged. All copyright notices of the OPC Foundation
  and of other contributors are retained; the patches are contributed under
  the same licence.
- Assemblies are **not strong-name signed** and **not Authenticode signed**:
  the OPC Foundation keys are not available to this fork. See
  [Strong naming](#strong-naming).
- Dependency: the Raft engine packages `RaftCs`, `RaftCs.Transport`,
  `RaftCs.Transport.NanoMsg`, and `RaftCs.Storage.File` are ZONTAL's patched
  `1.2.0-zontal.1` from github.com/zontal/raft-cs (upstream
  github.com/marcschier/raft-cs `v1.1.3` with the snapshot and compaction
  patches; same package ids and strong-name identity as `RaftCs` 1.1.x).
  [NuGet.config](NuGet.config) takes them from the static feed
  https://zontal.github.io/nuget-feed/index.json, which needs no credentials.

Every patch is a single commit on its own branch, created from tag `2.0.0`
(patch 7b is created from patch 7, whose seam it completes).
The release branch `zontal/2.0.0` is tag `2.0.0` with all patch branches merged
and the tag `2.0.0-zontal.1` marks the packaged state.

## Patches

| # | Branch | Change | Upstream |
|---|--------|--------|----------|
| 1 | `fix/sourcegen-nodeset-variable-without-value` | Source generator: a Variable or VariableType imported from a NodeSet2 without a `Value` element gets a null Value instead of the default value of its DataType (OPC 10000-6 F.5). ModelDesign inputs are unchanged. | [OPCFoundation/UA-.NETStandard#4798](https://github.com/OPCFoundation/UA-.NETStandard/pull/4798) |
| 2 | `fix/sourcegen-external-reference-targets` | Source generator: a NodeSet2 reference to a node of another model is kept when that model comes from a referenced assembly (whose dependency metadata carries types only), e.g. a device organized by DI's `DeviceSet`. It used to be dropped without a diagnostic. | [OPCFoundation/UA-.NETStandard#4796](https://github.com/OPCFoundation/UA-.NETStandard/pull/4796) |
| 3 | `fix/sourcegen-nodeset-address-space-only` | Source generator: new option `ModelSourceGeneratorEmitNodeSetNodesOnly` (`GeneratorOptions.EmitNodeSetNodesOnly`), off by default. When on, the predefined nodes generated for a NodeSet2 input are exactly the NodeSet2's own nodes: a subtype no longer gets copies of its supertypes' Mandatory InstanceDeclarations with NodeIds the NodeSet2 never assigned. Typed state classes and `CreateInstanceOf*` factories are unchanged. | [OPCFoundation/UA-.NETStandard#4797](https://github.com/OPCFoundation/UA-.NETStandard/pull/4797) |
| 4 | `fix/pkcs11-store-add-delete` | `Pkcs11CertificateStore.AddAsync` writes the certificate object to the token (linked by CKA_ID to the key it belongs to; a private key is never written) instead of silently dropping it; `DeleteAsync` destroys the certificate objects with the thumbprint (keys stay on the token) instead of always throwing. | [OPCFoundation/UA-.NETStandard#4792](https://github.com/OPCFoundation/UA-.NETStandard/pull/4792) |
| 5 | `fix/pubsub-published-datasets-parent` | `PubSubNodeManager` places PublishedDataSet and DataSetFolder objects under `PublishSubscribe.PublishedDataSets` (i=17371) instead of under `DataSetFolderType`'s `<DataSetFolderName>` InstanceDeclaration (i=14478). | [OPCFoundation/UA-.NETStandard#4793](https://github.com/OPCFoundation/UA-.NETStandard/pull/4793) |
| 6 | `fix/server-subscription-restore-errors` | `SubscriptionManager.RestoreSubscriptionsAsync` surfaces a failed restore of durable subscriptions instead of logging and dropping it. `SubscriptionManager.ContinueOnRestoreFailure` restores the old behaviour. | [OPCFoundation/UA-.NETStandard#4795](https://github.com/OPCFoundation/UA-.NETStandard/pull/4795) |
| 7 | `fix/raft-kv-store-snapshot` | `RaftSharedKeyValueStore.CreateSnapshotAsync` / `RestoreSnapshot`: the store's half of the snapshot and compaction seam. An application snapshots the store (after a barrier commit), compacts its consensus log, and on restart restores the snapshot before the store applies the remaining log. The engine half is patch 7b. | [OPCFoundation/UA-.NETStandard#4794](https://github.com/OPCFoundation/UA-.NETStandard/pull/4794) |
| 7b | `fix/raft-engine-snapshot` | The engine half of patch 7, on RaftCs `1.2.0-zontal.1`: `IRaftSnapshotConsensus` (committed items with log indices and snapshots, `CompactAsync`), implemented by `DefaultRaftConsensus`. Over it, `RaftSharedKeyValueStore` tracks `AppliedIndex`, replaces its state with a snapshot the engine delivers (a lagging replica installing the leader's snapshot, or the first item after a restart) and publishes the differences to watchers, and with `compactionInterval` snapshots its state into the engine, which discards the log behind it (`FileRaftStorage` shrinks its file). | Held back until an upstream RaftCs release provides the snapshot API it uses. |
| 8a | `fix/trustlist-validate-public` | `TrustList.ValidateNewTrustListAsync` is public. | [OPCFoundation/UA-.NETStandard#4799](https://github.com/OPCFoundation/UA-.NETStandard/pull/4799) |
| 8b | `fix/client-uafileinfo-public-constructor` | The `UaFileInfo` constructor is public, so a `FileSystemClient` can address a file by its NodeId. | [OPCFoundation/UA-.NETStandard#4790](https://github.com/OPCFoundation/UA-.NETStandard/pull/4790) |
| 8c | `fix/managed-session-rediscovery-validation` | `ManagedSession.ValidateRediscoveredEndpoint`: a hook that accepts or rejects the endpoint a reconnect rediscovers before it replaces the endpoint in use. | [OPCFoundation/UA-.NETStandard#4791](https://github.com/OPCFoundation/UA-.NETStandard/pull/4791) |

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
