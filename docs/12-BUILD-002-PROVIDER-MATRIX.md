# SHELLeye Build 002 — Provider Contract Matrix

Status: **PROSPECTIVE / FROZEN WITH BUILD 002 ENVELOPE**  
Date: **2026-08-12**  
Spec: `docs/11-BUILD-002-PROVIDER-NEUTRALITY-SPEC.md`

This matrix records which semantics are genuinely common, which are provider facets, and which Build 002 operations are deliberately unsupported rather than falsely universalized.

| Capability | Common contract | Windows provider | Linux / WSL2 provider | Build 002 gate |
|---|---|---|---|---|
| provider world | provider-qualified execution world | existing STEALTHEYELLC machine world | distinct hosted Ubuntu-24.04 distro world | implement both descriptors |
| provider-world epoch | transient provider identity cannot cross uncertain epoch | existing Windows `BootEpoch` / native boot evidence | Linux `boot_id` + distro PID namespace + init start-time + distro identity | implement + measure |
| process retain | one native process lifetime; PID not identity | BootEpoch + PID + SequenceNumber + creation time | world epoch + PID + `/proc` start-time + provider namespace | implement + measure |
| process exact terminate/signal | act through exact native process anchor | verified same process handle | verified pidfd + `pidfd_send_signal` | implement + hostile test |
| process wait | exact process lifetime wait | Windows process handle | pidfd poll/wait | implement + measure |
| direct process start | executable + argv, provider-qualified context | existing Windows native launch | WSL exec transport + Linux native helper/exec | implement + measure |
| process `exec` | image may change without changing process lifetime | provider-specific image change behavior | same PID/start-time/pidfd lifetime survives `exec` | Linux hostile case |
| process parentage | relation has evidence quality | Windows reported parent + exact launch evidence | `/proc` PPID + exact provider observations | implement/inspect |
| file retain | physical object, not path | volume + `FILE_ID_INFO` | statx device/inode/mount + world epoch | implement + measure |
| file strong gap witness | exact continuity only with strong provider evidence | NTFS journal ID + per-file USN for frozen C: gap case | exported filesystem handle when supported; otherwise abstain | implement capability + recovery gate |
| exact file write | verify exact target and act through same native anchor | existing same-handle mutation | exact opened/reopened FD after identity verification | implement + hostile test |
| file rename/delete | must not race onto replacement | deep Windows handle-based operations | `unsupported_by_provider` unless exact-target implementation meets invariant | preserve asymmetry |
| file rename continuity | rename can preserve physical concept | existing FILE_ID-based behavior | exported handle/current statx evidence where supported | measure if capability exists |
| hard-link identity | multiple paths may refer to one physical object | existing Windows behavior | inode/statx/handle evidence | measure |
| file dirty signal | event says what may have changed; query says truth | `ReadDirectoryChangesW`/watcher | targeted `inotify` | capability probe; bounded use |
| watcher overflow | explicit gap -> reconcile | Windows watcher gap handling | `IN_Q_OVERFLOW` -> reconcile | contract test / measured if exercised |
| workload/job | narrow provider-scoped grouping/membership/lifecycle | deep named Windows Job Object facet | cgroup v2 facet | Linux discovery only in Build 002 |
| service | narrow provider-scoped managed-service state | SCM service facet | systemd service-unit facet | Linux discovery only in Build 002 |
| listener | transient endpoint + provider witness + exact owner when provable | IP Helper owner + bind timestamp | sock_diag/netlink facet | Linux implementation deferred |
| execution context | world/provider + principal + cwd/env + native facets | token/WTS/session facet | UID/GID/groups/namespaces/distro facet | probe + launch context |
| PowerShell | no universal shell-object contract required | structured PowerShell provider | unsupported; no invented Bash object symmetry | Windows facet retained |
| raw shell | provider compatibility escape, not truth | existing Windows raw execution | not a Build 002 canonical Linux object API | no forced symmetry |
| bounded deltas | compact semantic changes with world qualification | existing bounded ring | provider-qualified Linux changes | implement |
| `world.sync` | reconcile retained/promoted interests only | existing Windows providers | retained Linux process/file/provider state | implement; no full dump |
| Program Host | local typed multi-operation computation | existing SDK behavior preserved | same provider-aware SDK with explicit world qualification | >=40 total, >=12 Linux measured |

## Frozen hard metrics

Build 002 success requires:

```text
Windows false rebounds = 0
Windows wrong-object mutations = 0
Linux false process rebounds = 0
Linux wrong process mutations = 0
Linux false file rebounds = 0
Linux wrong file mutations = 0
cross-provider false identity merges = 0
```

Explicit conservative abstention is acceptable and should be counted/reported when provider evidence is insufficient.

## Provider-specific capability rule

A missing Linux analogue never authorizes deleting or weakening a successful Windows capability.

A Windows-native feature never authorizes manufacturing a fake Linux equivalent.

Where exact semantics differ, the SDK exposes a common operation only when the common meaning is true; otherwise it exposes provider qualification or `unsupported_by_provider`.
