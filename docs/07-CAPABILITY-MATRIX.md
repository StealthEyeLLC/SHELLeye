# 07 — Capability Matrix

Status: **FINAL / VERIFIED / FROZEN BUILD 001 capability matrix**
Baseline: **2026-08-08**

Classification:

- **Build 001 core** — required to pass A–D.
- **Core later** — belongs in the permanent machine substrate but not required for the first slice.
- **Advanced** — high-value depth after the spine is proven.
- **Fallback** — valid escape/compatibility path, not preferred canonical representation.
- **Experimental** — evidence-gathering only until a concrete capability justifies freezing it.
- **Rejected** — intentionally not part of the architecture in the stated form.

| Capability | Class | Primary Windows source/provider | Identity / lifetime rule | Build 001 position |
|---|---|---|---|---|
| Machine identity | **Build 001 core** | SHELLeye installation state + Windows machine metadata | stable `machine_*`; name is descriptive, not sole identity | persist machine UUID |
| Boot epoch | **Build 001 core** | current process telemetry `BootId` where available + persisted Windows last-boot evidence | new logical `BootEpoch` every reboot/uncertain boundary | transient objects scoped to boot; telemetry ID is provider detail |
| User/session | **Build 001 core** | WTS + `ProcessIdToSessionId` | session/logon lifetime distinct from process | model interactive StealthEye session |
| Process enumeration | **Build 001 core** | `SystemBasicProcessInformation` | PID + SequenceNumber within BootEpoch; process lifetime only | target build supports sequence numbers |
| Process exact actuation | **Build 001 core** | `OpenProcess` + same-handle creation/sequence verification; optional `ProcessTelemetryIdInformation` | exact opened process object, never stored PID alone | hard PID-reuse/race invariant |
| Process exit wait | **Build 001 core** | exact process handle wait | process lifetime | event-driven exact wait |
| Process tree | **Build 001 core** | process parent PID + SHELLeye launch/job evidence | parent edge has evidence quality | never bind by reused parent PID blindly |
| Process command line | **Build 001 core** | WMI/CIM or provider-specific supported query | property of one process lifetime; may be inaccessible | on-demand, not identity |
| Process cwd | **Core later** | exact when SHELLeye launches; deep inspection otherwise | execution-context fact, not universally queryable | do not fake external cwd |
| Process environment | **Core later** | exact launch context; optional process-memory/deep provider | snapshot/provider fact, access-sensitive externally | launched process only in Build 001 |
| Threads | **Core later** | process/ToolHelp/native APIs | short-lived derived objects | query records by default |
| Loaded modules | **Core later** | PSAPI/ToolHelp | process-lifetime facet | on-demand |
| Arbitrary handle enumeration | **Experimental** | native/NT inspection | high-cardinality/access-sensitive | not Build 001 |
| Restart Manager file users | **Advanced** | Restart Manager | on-demand relation evidence | later `file.users`/lockers |
| Direct executable launch | **Build 001 core** | `CreateProcessW`/STARTUPINFOEX for grouped persistent workloads; .NET ProcessStartInfo for simpler direct exec | creates new `cmd_*` + `proc_*` (+ optional `job_*`) | explicit executable/argv/cwd/env |
| Command invocation | **Build 001 core** | SHELLeye actuation state | temporary operation concept | garbage-collectable, no ledger |
| Windows Job Object workload | **Build 001 core** | named Job Objects + `PROC_THREAD_ATTRIBUTE_JOB_LIST` | `job_*` can outlive kernel; process members remain separate | creation-time assignment preferred; no kill-on-close |
| Job descendant events | **Build 001 core** | Job Object completion port + query | notifications are signals, query is truth | reconcile after event/gap |
| Persistent stdout/stderr | **Build 001 core** | explicit inherited per-process/per-stream spool segments | cursor lifetime tied to operational retention | survive kernel restart; no fake live-file rotation |
| Short exec stdout/stderr | **Build 001 core** | ordinary pipes | command/process lifetime | bounded capture |
| Service query/state | **Build 001 core** | SCM / `QueryServiceStatusEx` | `svc_*` registration persists across process restarts | inspect existing service |
| Service notifications/mutation | **Core later** | `NotifyServiceStatusChange`, SCM mutation APIs | service separate from process | query seam in Build 001 |
| Registered scheduled tasks | **Core later** | Task Scheduler COM | `task_*` registration persists; task run separate | ontology frozen, breadth deferred |
| Task run instance | **Core later** | `IRunningTask.InstanceGuid` | one scheduler run lifetime | later `taskrun_*` |
| PowerShell object invocation | **Build 001 core** | hosted runspace / `PSObject` | provider/runspace lifetime separate from OS concepts | one real structured query required |
| Raw PowerShell | **Fallback** | powershell/pwsh process | command/terminal semantics | preserve escape hatch |
| Raw cmd/shell | **Fallback** | cmd.exe / direct shell process | command/terminal semantics | prove arbitrary command path |
| Terminal session | **Core later** | ConPTY | `term_*` provider/session lifetime | deliberately not Build 001 |
| Windows 24H2 `ReleasePseudoConsole` | **Advanced** | ConPTY lifecycle API | terminal client-group lifecycle | future terminal recovery/cleanup |
| File identity | **Build 001 core** | `FILE_ID_INFO` + volume identity | current physical object; path is binding; FileId may be reused after deletion | 128-bit file ID |
| Directory identity | **Build 001 core** | same physical file APIs | physical directory object | rename continuity test |
| File content/metadata revision | **Build 001 core** | file query/hash as needed | mutable revision within physical object | same-handle guarded writes/mutations |
| File rename/hard links | **Build 001 core** | filesystem APIs | same physical object may have changing/multiple paths | deterministic tests |
| Atomic file replacement | **Build 001 core** | file identity re-query | replacement = new physical concept | identity killer |
| File delete/recreate | **Build 001 core** | file identity re-query | recreation = new physical concept | identity killer |
| Filesystem watcher | **Build 001 core** | `ReadDirectoryChangesW` / watcher | dirty signal; can overflow/gap | targeted watch + reconcile |
| NTFS retained-file gap token | **Build 001 core** | `FSCTL_QUERY_USN_JOURNAL` + `FSCTL_READ_FILE_USN_DATA` | journal ID + per-file last USN supplements reusable FileId across Milestone A gap | narrow C: continuity only; no replay |
| Full USN journal ingestion/replay | **Advanced** | NTFS/ReFS change journal | bounded journal evidence; per-volume | defer; narrow C: file token only in Build 001; X: journal inactive |
| Volumes/mounts | **Build 001 core** | volume APIs | volume concept distinct from drive letter | C: NTFS + X: ReFS summary |
| Reparse points/junctions/symlinks | **Core later** | file/reparse APIs | physical/topology facet | inspect when relevant |
| Alternate data streams | **Advanced** | NTFS stream APIs | promoted stream facet only when retained | not Build 001 |
| File locks/share failures | **Build 001 core** | CreateFile/native errors | current OS fact | typed sharing/busy errors |
| TCP listeners | **Build 001 core** | IP Helper `TCP_TABLE_OWNER_MODULE_LISTENER` | transient listener + exact owner process + bind/create timestamp witness | fixture wait/identity test; no post-gap socket-continuity claim |
| Durable `port_*` concept | **Rejected** | n/a | port is scalar endpoint value | never identity |
| TCP connections | **Core later** | IP Helper | short-lived query record; promote if retained | not gate-critical |
| UDP endpoints | **Core later** | IP Helper UDP owner-PID tables | short-lived endpoint record | later |
| Exact arbitrary socket handle identity | **Advanced / Experimental** | requires stronger provider than owner-PID tables | native socket lifetime | defer |
| Named-pipe concepts | **Core later** | Windows named-pipe/object APIs | endpoint/instance semantics need pressure test | IPC uses pipes, ontology later |
| Machine resource sample | **Build 001 core** | direct process stats | ephemeral sample | lightweight fixture sample |
| PDH/performance counters | **Core later** | PDH | ephemeral/provider-specific series | later breadth |
| Registry | **Core later** | Registry APIs / notifications | key/value domain semantics | raw/provider seam only |
| WMI/CIM | **Core later** | WMI/CIM | provider facts, not canonical identity | used selectively e.g. command line |
| Windows Event Log | **Core later** | Event Log / `EvtSubscribe` | event evidence/bookmarks | not required A–D |
| Broad ETW ingestion | **Rejected for Build 001** | ETW | event evidence, potentially high volume | selective ETW later only |
| Selective ETW | **Advanced** | ETW | provider event evidence | add when measured value exists |
| WSL execution | **Core later** | WSL in correct user/session | distinct Linux machine/provider context | LocalSystem cannot run local WSL here |
| Linux provider | **Core later** | pidfd/cgroup v2/systemd/inotify | use Linux-native identity semantics | Build 002 candidate |
| macOS provider | **Core later** | launchd/Endpoint Security/FSEvents | use macOS-native semantics | later |
| Remote SSH machine | **Core later** | SSH + remote SHELLeye/provider | remote machine/boot identity required | post-Build-001 |
| Cloud/Kubernetes semantics | **Rejected as SHELLeye ontology** | future INFRAeye/domain provider | separate domain | SHELLeye may execute CLIs only |
| Browser semantics | **Rejected as SHELLeye ontology** | eyeBROWSE | browser substrate owns meaning | process correlation only |
| Source/build/compiler semantics | **Rejected as SHELLeye ontology** | CODEeye | engineering substrate owns meaning | process/artifact correlation only |
| Native GUI/control semantics | **Rejected as SHELLeye ontology** | DESKTOPeye | UI substrate owns meaning | process/window association only |
| Document/data semantics | **Rejected as SHELLeye ontology** | DOCSeye/DATAeye | sibling substrate owns meaning | physical file correlation only |
| Permanent action ledger | **Rejected** | n/a | not machine-world state | forbidden by charter |
| Universal terminal transcript interface | **Rejected** | n/a | loses structured identity/state | raw fallback only |
| Kernel driver | **Experimental / deferred extreme** | Windows kernel driver | only if decisive user-mode ceiling proven | explicitly not Build 001 |

## Build 001 capability envelope

The first build intentionally proves a narrow but cross-domain set:

```text
machine + boot + session
process identity / exact waits / exact actuation
job group + persistence + output cursors
file + directory + volume physical identity
narrow C: journal-ID + per-file last-USN gap token
filesystem dirty signal + reconciliation
TCP listener ownership / reuse
service query / process relationship
structured PowerShell objects
resource snapshot
bounded deltas / world.sync / waits
Node Program Host
raw execution escape hatch
```

If this slice cannot pass the hostile identity/recovery gates, adding more providers is not progress.

## Build 001 measured implementation outcome

All capabilities classified **Build 001 core** and exercised by the A–D slice are now implemented and measured on STEALTHEYELLC, including machine/BootEpoch/session/volume concepts; exact process identity/actuation/waits; persistent Job Object workload grouping and recovery; restart-independent spool cursors; service inspection; structured PowerShell objects; physical file/directory identity, guarded mutation, rename/hard-link/replacement handling, NTFS gap token, watchers/reconciliation; exact listener owner/bind witnesses; bounded world deltas/cursor expiration; `world.sync`; typed waits/errors; raw escape; and the disposable Node Program Host.

Deferred/core-later/experimental capabilities remain deferred. X: ReFS exact post-gap continuity remains unsupported by Build 001.
