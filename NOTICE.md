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

Every patch is a single commit on its own branch, created from tag `2.0.0`.
The release branch `zontal/2.0.0` is tag `2.0.0` with all patch branches merged
and the tag `2.0.0-zontal.1` marks the packaged state.

## Patches

| # | Branch | Change |
|---|--------|--------|
| 1 | `fix/sourcegen-nodeset-variable-without-value` | Source generator: a Variable or VariableType imported from a NodeSet2 without a `Value` element gets a null Value instead of the default value of its DataType (OPC 10000-6 F.5). ModelDesign inputs are unchanged. |
| 2 | `fix/sourcegen-external-reference-targets` | Source generator: a NodeSet2 reference to a node of another model is kept when that model comes from a referenced assembly (whose dependency metadata carries types only), e.g. a device organized by DI's `DeviceSet`. It used to be dropped without a diagnostic. |
| 3 | `fix/sourcegen-nodeset-address-space-only` | Source generator: new option `ModelSourceGeneratorEmitNodeSetNodesOnly` (`GeneratorOptions.EmitNodeSetNodesOnly`), off by default. When on, the predefined nodes generated for a NodeSet2 input are exactly the NodeSet2's own nodes: a subtype no longer gets copies of its supertypes' Mandatory InstanceDeclarations with NodeIds the NodeSet2 never assigned. Typed state classes and `CreateInstanceOf*` factories are unchanged. |
| 4 | `fix/pkcs11-store-add-delete` | `Pkcs11CertificateStore.AddAsync` writes the certificate object to the token (linked by CKA_ID to the key it belongs to; a private key is never written) instead of silently dropping it; `DeleteAsync` destroys the certificate objects with the thumbprint (keys stay on the token) instead of always throwing. |
| 5 | `fix/pubsub-published-datasets-parent` | `PubSubNodeManager` places PublishedDataSet and DataSetFolder objects under `PublishSubscribe.PublishedDataSets` (i=17371) instead of under `DataSetFolderType`'s `<DataSetFolderName>` InstanceDeclaration (i=14478). |
| 6 | `fix/server-subscription-restore-errors` | `SubscriptionManager.RestoreSubscriptionsAsync` surfaces a failed restore of durable subscriptions instead of logging and dropping it. `SubscriptionManager.ContinueOnRestoreFailure` restores the old behaviour. |
| 7 | `fix/raft-kv-store-snapshot` | `RaftSharedKeyValueStore.CreateSnapshotAsync` / `RestoreSnapshot`: the store's half of the snapshot and compaction seam. An application snapshots the store (after a barrier commit), compacts its consensus log, and on restart restores the snapshot before the store applies the remaining log. The Raft engine parts are not in this repository; see [below](#known-unpatched-limitations). |
| 8a | `fix/trustlist-validate-public` | `TrustList.ValidateNewTrustListAsync` is public. |
| 8b | `fix/client-uafileinfo-public-constructor` | The `UaFileInfo` constructor is public, so a `FileSystemClient` can address a file by its NodeId. |
| 8c | `fix/managed-session-rediscovery-validation` | `ManagedSession.ValidateRediscoveredEndpoint`: a hook that accepts or rejects the endpoint a reconnect rediscovers before it replaces the endpoint in use. |

## Known, unpatched limitations

The following behaviours of 2.0.0 are known limitations for ZONTAL's use and
are deliberately **left as they are** in this fork:

- the lockout constants;
- the secure channel lockout;
- the cap on subscriptions per session;
- `DistributedSessionManager` being sealed, and the `SessionId` it assigns;
- the registry that `CertificateStoreIdentifier.OpenStore` resolves store
  types through;
- `HistoryClient` accepting history pages with an Uncertain status;
- the Raft engine half of item 7: ZONTAL's review found that
  `RaftNode.ApplyEntry` never hands a snapshot to the application and that
  `FileRaftStorage.Compact` never shrinks the log file. Both types belong to
  the external `RaftCs` packages (`RaftCs`, `RaftCs.Storage.File` 1.1.2,
  whose package metadata names the repository github.com/marcschier/crdt),
  not to this repository, so they cannot be patched here. The store-side seam
  (patch 7) is in place; the engine must still deliver snapshots and applied
  log indices, which `IRaftConsensus` does not carry.

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
