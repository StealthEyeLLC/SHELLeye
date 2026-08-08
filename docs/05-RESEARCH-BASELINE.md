# 05 — Research Baseline

Status: **Canonical evidence baseline — final synthesis verified 2026-08-08**

This document records the external/current evidence that materially shaped SHELLeye. It intentionally distinguishes fact, evidence, inference, open questions, and speculation.

Terminology:

- **FACT** — directly observed on STEALTHEYELLC or a stable native-system property used by the design.
- **CURRENT EXTERNAL EVIDENCE** — supported by current primary/authoritative documentation reviewed during this pass.
- **ARCHITECTURAL INFERENCE** — a SHELLeye design conclusion drawn from facts/evidence.
- **OPEN QUESTION** — implementation measurement/compatibility work deliberately left unresolved.
- **SPECULATION** — plausible but not used as a frozen design basis.

## 1. Process identity and PID reuse

### CURRENT EXTERNAL EVIDENCE

Microsoft now documents `SystemBasicProcessInformation` under `NtQuerySystemInformation`, available from Windows 11 build **26100.4770**. Its `SYSTEM_BASICPROCESS_INFORMATION` includes PID, reported parent PID, image name, and `SequenceNumber`. Microsoft explicitly describes `SequenceNumber` as a unique value assigned to each process and usable to detect PID reuse instead of process creation time; it also recommends this basic class over the older full process class when basic inventory is sufficient because it is faster/lower-memory.

Source: [NtQuerySystemInformation / SystemBasicProcessInformation](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation).

Microsoft process documentation establishes that a PID identifies a process only during its lifetime, while an open process handle remains bound to that process object until the handle is closed, even after termination. `OpenProcess` opens the process currently represented by the PID; `GetProcessTimes` supplies handle-bound creation time; `TerminateProcess` and wait APIs operate on a process handle.

Sources:

- [Process Handles and Identifiers](https://learn.microsoft.com/en-us/windows/win32/procthread/process-handles-and-identifiers)
- [OpenProcess](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess)
- [GetProcessTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes)
- [TerminateProcess](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-terminateprocess)

Microsoft also exposes `ProcessTelemetryIdInformation` through `NtQueryInformationProcess`; its structure contains `ProcessSequenceNumber`, `ProcessStartKey`, creation time, session ID, and boot ID. Microsoft labels the `NtQueryInformationProcess` family internal/subject to change, so this is useful target-specific corroboration rather than the sole identity contract.

Sources:

- [NtQueryInformationProcess](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntqueryinformationprocess)
- [PROCESS_TELEMETRY_ID_INFORMATION_TYPE](https://learn.microsoft.com/en-us/windows/win32/devnotes/process_telemetry_id_information_type)

### FACT

STEALTHEYELLC is Windows build **26100.8973**, newer than the documented `SystemBasicProcessInformation` availability floor. A final-synthesis live probe of `NtQueryInformationProcess(ProcessTelemetryIdInformation)` on this target succeeded and returned process sequence/start/boot metadata.

### ARCHITECTURAL INFERENCE

- `PID != proc_* identity`.
- Build 001 process witness is `BootEpoch + PID + SequenceNumber`, strengthened by creation time and exact native-handle verification.
- `proc_*` represents one native process lifetime and never rebounds across restart.
- retained mutation opens the PID, verifies creation time and retained sequence **while that same process handle is held**, then acts through the same handle.
- if the original exits after `OpenProcess`, later PID reuse cannot redirect the held handle; this closes the enumeration/open/mutation wrong-target race.
- `ProcessTelemetryIdInformation` is a preferred target corroborator when available, with a fresh `SystemBasicProcessInformation` sequence check as the required fallback on this build.
- process restart continuity belongs to `job_*`, `svc_*`, registered task/workload concepts, not `proc_*`.

### OPEN QUESTION

`NtQuerySystemInformation` and `NtQueryInformationProcess` are documented as internal/subject to change. Build 001 isolates them behind the Windows provider and retains public handle/creation-time behavior plus provider fallbacks rather than making NT query layout a cross-platform contract.
## 2. Process parentage

### CURRENT EXTERNAL EVIDENCE

`SystemBasicProcessInformation` reports `InheritedFromUniqueProcessId`. The older `Win32_Process` management class also exposes parent process ID and creation data.

Sources:

- [NtQuerySystemInformation](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation)
- [Win32_Process](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-process)

### ARCHITECTURAL INFERENCE

A reported parent PID is not automatically an exact durable process edge after the fact. The parent may have exited and Windows may reuse its PID.

SHELLeye therefore preserves relation quality: exact when it created/observed both incarnations, otherwise resolved-current/reported/unknown as evidence permits. This prevents a stale parent PID from being attached to an unrelated current `proc_*`.

## 3. Job Objects and persistent workload groups

### CURRENT EXTERNAL EVIDENCE

Microsoft Job Object documentation establishes:

- Job Objects manage groups of processes as a unit;
- child processes are associated with the job by default unless native breakaway behavior changes that;
- nested jobs are supported on modern Windows;
- named jobs can be reopened with `OpenJobObject`;
- a Job Object is destroyed when the **last handle is closed and all associated processes have terminated**;
- `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` changes that behavior by terminating associated processes when the last handle closes;
- completion-port messages are useful notifications, but most are not guaranteed and process IDs in messages may already be recycled.

Source: [Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects).

Final verification found a stronger creation primitive than the earlier preferred suspended/assign/resume sequence. `UpdateProcThreadAttribute` documents `PROC_THREAD_ATTRIBUTE_JOB_LIST` on Windows 10+ to assign a list of Job Objects to the child **as part of process creation**. The same mechanism exposes `PROC_THREAD_ATTRIBUTE_HANDLE_LIST` to explicitly constrain inherited handles.

Sources:

- [UpdateProcThreadAttribute](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute)
- [Create processes](https://learn.microsoft.com/en-us/windows/win32/procthread/creating-processes)

### ARCHITECTURAL INFERENCE

A SHELLeye persistent `job_*` should, when compatible:

- have a unique named Windows Job Object facet;
- associate its completion port while still inactive to reduce missed startup notifications;
- on this target, prefer `STARTUPINFOEX + PROC_THREAD_ATTRIBUTE_JOB_LIST` so intended job membership exists before child user code executes;
- use `PROC_THREAD_ATTRIBUTE_HANDLE_LIST` for only the restart-safe stdout/stderr/stdin handles the child requires;
- retain suspended → `AssignProcessToJobObject` → resume as a compatibility fallback, not the preferred Build 001 path;
- not enable kill-on-close for persistent jobs;
- treat completion-port messages as dirty/change signals and current job/process queries as truth;
- reopen the native job by name after kernel restart while members remain alive.

No separate native handle-broker process is required merely to keep a Job Object alive across kernel death.

### OPEN QUESTION

Some workloads have existing/nested job expectations or explicit breakaway behavior. Build 001 must prove the fixture path, while the general provider must allow `not grouped / grouping unsupported` instead of forcing every process into a Job Object.
## 4. Persistent stdout/stderr

### CURRENT EXTERNAL EVIDENCE

Windows process creation supports explicit standard handles and explicit inherited-handle lists. An inherited handle refers to the same underlying object in the child process. Normal file writes are buffered by the operating system; `FlushFileBuffers`/write-through address storage/power-loss durability rather than merely surviving the parent controller process.

Sources:

- [Create processes](https://learn.microsoft.com/en-us/windows/win32/procthread/creating-processes)
- [Inheritance](https://learn.microsoft.com/en-us/windows/win32/procthread/inheritance)
- [WriteFile](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-writefile)

### FACT

If a kernel owns anonymous pipe read ends and dies, the I/O topology itself changes even if the child process should remain alive.

### ARCHITECTURAL INFERENCE

For persistent jobs, kernel-owned anonymous pipes are the wrong default. Build 001 redirects stdout/stderr to restart-independent **per-process/per-stream spool segments**, explicitly inherited by the child through `PROC_THREAD_ATTRIBUTE_HANDLE_LIST`, and exposes logical cursor reads across those segments.

A subtle final-pass correction is important: a child that already owns an open spool file handle does not automatically switch to a new file merely because the pathname is renamed/rotated. Therefore Build 001 requires bounded model-facing reads, bounded fixture output, and garbage collection of completed segments; it does **not** claim transparent live rotation of an active segment. A later dedicated stream sink can earn that complexity if measured workloads need it.

This is operational state, not an action ledger, and kernel-crash continuity does not require per-write storage flush semantics.
## 5. File identity

### CURRENT EXTERNAL EVIDENCE

Windows `FILE_ID_INFO` contains `VolumeSerialNumber` and a 128-bit `FILE_ID_128 FileId`. The 128-bit form is important for a filesystem-general Windows model including ReFS. `OpenFileById` can open by identifier where supported.

Sources:

- [FILE_ID_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_id_info)
- [OpenFileById](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-openfilebyid)

The final pass independently verified a critical lifetime caveat: Microsoft explicitly states that file IDs are **not guaranteed unique over time** because filesystems are free to reuse them. On NTFS a file keeps the same file ID until deletion; `ReplaceFile` leaves the resulting file with the replacement file's ID.

Source: [BY_HANDLE_FILE_INFORMATION — file-ID remarks](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/ns-fileapi-by_handle_file_information).

Windows also exposes handle-based mutation through `SetFileInformationByHandle`, including rename/disposition/end-of-file classes. This allows SHELLeye to verify identity and mutate through the same native handle rather than verify one path lookup and then race through another.

Source: [SetFileInformationByHandle](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle).

### ARCHITECTURAL INFERENCE

Generic SHELLeye `file_*` / `dir_*` represents a physical filesystem object, not a path or semantic document.

- same-volume rename: same physical concept;
- hard-link path changes: same physical concept;
- atomic replacement: old physical concept replaced/destroyed, new concept at same path;
- delete/recreate: new concept even with identical path/content;
- cross-volume move: new destination physical identity.

For mutation, Build 001 opens the target, queries `FILE_ID_INFO` and any revision precondition, then performs `WriteFile` / `SetFileInformationByHandle` through **that same verified handle** wherever Windows offers handle-based semantics. This closes the inspect→path-replacement→write race without retaining long-lived file handles.

For post-kernel-gap recovery, `volume + FileId` alone is insufficient because IDs can be reused. Exact continuity therefore needs an additional provider witness or must be conservatively lost.

SHELLeye intentionally differs from CODEeye here: CODEeye may preserve semantic source-file continuity across an editor's physical replacement; SHELLeye describes the machine file that actually exists.
## 6. Filesystem events and USN

### CURRENT EXTERNAL EVIDENCE

`ReadDirectoryChangesW` provides asynchronous/synchronous directory change notifications. .NET `FileSystemWatcher` documentation warns its internal buffer can overflow; when it does, changes can be missed and an error is raised.

Sources:

- [ReadDirectoryChangesW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-readdirectorychangesw)
- [FileSystemWatcher.Error](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.error)

Microsoft documents that file IDs can be reused over time. Separately, `FSCTL_READ_FILE_USN_DATA` returns the last USN written for a specified file/directory, while `FSCTL_QUERY_USN_JOURNAL` returns the journal ID. Microsoft documents the journal ID as an integrity check: it changes when existing USNs may be unusable, and all future USNs remain greater than existing USNs in the valid chronology.

Sources:

- [FSCTL_READ_FILE_USN_DATA](https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_read_file_usn_data)
- [FSCTL_QUERY_USN_JOURNAL](https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_query_usn_journal)
- [Using the Change Journal Identifier](https://learn.microsoft.com/en-us/windows/win32/fileio/using-the-change-journal-identifier)

### FACT

Final live probes:

```text
C: NTFS — journal active; known journal ID; FSCTL_READ_FILE_USN_DATA returns v3 + nonzero last USN
X: ReFS — journal inactive; FSCTL_READ_FILE_USN_DATA returns v3 with last USN 0
```

Microsoft documents volume journal-ID operations as administrator operations. The `StealthEye` owner account is a member of the local Administrators group, so the Build 001 interactive scheduled task can run with highest available owner-account privileges without creating a SHELLeye privilege model.

### ARCHITECTURAL INFERENCE

Build 001 now distinguishes two USN uses:

```text
watcher/native event → low-latency dirty signal
physical identity/current query → current truth
C: journal ID + per-file last USN → narrow exact continuity token for the unchanged retained file across Milestone A kernel gap
full USN record scanning/replay → later provider
```

This is a material final-pass upgrade. It preserves the original economy—no broad volume journal ingestion, no event-history reconstruction, no requirement to create/resize a journal—while closing the only identified false-file-continuity hole in Milestone A. If the journal ID changes, the file USN changes, the query is inaccessible, or the provider lacks this witness, continuity is reduced to stale/ambiguous rather than manufactured.

X: remains a current ReFS 128-bit identity smoke case while its journal is inactive.
## 7. Services

### CURRENT EXTERNAL EVIDENCE

`QueryServiceStatusEx` exposes SCM service state and current process ID where meaningful; Microsoft documents that the PID is not valid in all pending/stopped states.

`NotifyServiceStatusChange` provides asynchronous service-state notifications and service create/delete notifications, with documented lag/re-registration behavior.

Sources:

- [QueryServiceStatusEx](https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-queryservicestatusex)
- [NotifyServiceStatusChange](https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-notifyservicestatuschangew)

### ARCHITECTURAL INFERENCE

`svc_*` is a service registration/logical service, not a process. Service restart changes `current_process`; the service concept can remain. The service PID must be resolved to a current exact `proc_*` rather than treated as durable identity.

## 8. Scheduled tasks

### CURRENT EXTERNAL EVIDENCE

Task Scheduler COM APIs expose registered tasks and running instances. `IRunningTask` exposes an `InstanceGuid`, engine PID/state, path/name data; the Scheduler generates a distinct run instance identity.

Sources:

- [IRegisteredTask::GetInstances](https://learn.microsoft.com/en-us/windows/win32/api/taskschd/nf-taskschd-iregisteredtask-getinstances)
- [IRunningTask](https://learn.microsoft.com/en-us/windows/win32/api/taskschd/nn-taskschd-irunningtask)

### ARCHITECTURAL INFERENCE

Registered task identity and task-run identity must be separate (`task_*` versus `taskrun_*`). Full task breadth is not needed in Build 001.

## 9. Network listeners and port ownership

### CURRENT EXTERNAL EVIDENCE

Windows IP Helper `GetExtendedTcpTable` supports owner-module listener tables for IPv4/IPv6. `MIB_TCPROW_OWNER_MODULE` includes the owning PID and `liCreateTimestamp`, a FILETIME indicating when the context bind operation that created the TCP link occurred.

Sources:

- [GetExtendedTcpTable](https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-getextendedtcptable)
- [MIB_TCPROW_OWNER_MODULE](https://learn.microsoft.com/en-us/windows/win32/api/tcpmib/ns-tcpmib-mib_tcprow_owner_module)

### ARCHITECTURAL INFERENCE

- a port number is not durable object identity;
- Build 001 `listener_*` includes endpoint/protocol + exact owner `proc_*` + bind/create timestamp + observation generation when available;
- closing/reopening the same port creates a new listener concept;
- `liCreateTimestamp` strengthens replacement detection but is not documented as a globally unique socket identifier;
- after an unobserved kernel gap, current endpoint state can be reconstructed without inventing proof that the same native socket persisted continuously.

Exact arbitrary external socket-handle identity remains deferred; IP Helper supplies strong current endpoint/owner/bind evidence without intrusive socket-handle instrumentation.
## 10. PowerShell as an object provider

### CURRENT EXTERNAL EVIDENCE

Microsoft PowerShell hosting documentation supports:

- creating `Runspace` instances;
- creating an `InitialSessionState`;
- building a pipeline through `System.Management.Automation.PowerShell`;
- synchronous/asynchronous invocation;
- returning typed/`PSObject` collections before human formatting.

Sources:

- [Windows PowerShell Host Quickstart](https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/windows-powershell-host-quickstart)
- [Creating Runspaces](https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/creating-runspaces)
- [Adding and invoking commands](https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/adding-and-invoking-commands)

### FACT

Live machine:

```text
Windows PowerShell 5.1.26100.8972 available
pwsh.exe not found on SYSTEM PATH
```

### ARCHITECTURAL INFERENCE

PowerShell should be a structured breadth provider, not `powershell.exe` plus formatted stdout and not the canonical source for process/file/service identity when stronger native APIs exist.

### OPEN QUESTION

Build 001 must experimentally choose between:

- hosting available Windows PowerShell 5.1 in an isolated provider process; or
- packaging a compatible modern `Microsoft.PowerShell.SDK` provider.

The architecture does not require a machine-wide PowerShell 7 install.

## 11. Direct executable argument handling

### CURRENT EXTERNAL EVIDENCE

.NET `ProcessStartInfo.ArgumentList` accepts separate argument strings and constructs/escapes the Windows process command line, avoiding the caller's need to manually quote one shell command string.

Source: [ProcessStartInfo.ArgumentList](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.argumentlist?view=net-10.0).

### ARCHITECTURAL INFERENCE

Direct executable + argv is the preferred generic execution representation. Native `CreateProcess` P/Invoke remains necessary where Build 001 needs suspended creation, exact Job Object association before resume, token/session control, or inherited-handle control.

## 12. ConPTY

### CURRENT EXTERNAL EVIDENCE

Windows provides pseudoconsole/ConPTY APIs for terminal semantics. On Windows 11 24H2 build 26100, `ReleasePseudoConsole` can release host ownership and let the pseudoconsole exit once attached clients disconnect; the lifecycle documentation explicitly reflects that the initial console process is not necessarily the whole interactive workload.

Sources:

- [CreatePseudoConsole](https://learn.microsoft.com/en-us/windows/console/createpseudoconsole)
- [ReleasePseudoConsole](https://learn.microsoft.com/en-us/windows/console/releasepseudoconsole)

### ARCHITECTURAL INFERENCE

ConPTY belongs in a terminal compatibility provider, not the canonical machine interface. Build 001 should not overbuild terminal emulation.

## 13. ETW and Event Log

### CURRENT EXTERNAL EVIDENCE

ETW supports high-rate real-time/logged event tracing through controller/provider/consumer sessions. Consumers can encounter lost events under pressure. Windows Event Log subscriptions support push/pull subscription patterns and bookmarks.

Sources:

- [Event Tracing for Windows](https://learn.microsoft.com/en-us/windows-hardware/test/weg/instrumenting-your-code-with-etw)
- [Consuming Events](https://learn.microsoft.com/en-us/windows/win32/etw/consuming-events)
- [EvtSubscribe](https://learn.microsoft.com/en-us/windows/win32/api/winevt/nf-winevt-evtsubscribe)

### ARCHITECTURAL INFERENCE

Broad ETW ingestion is not needed to prove Build 001. Add selective ETW later only where measured value exceeds native query/watcher/reconciliation complexity.

SHELLeye is not a telemetry collector.

## 14. WMI/CIM and process environment

### CURRENT EXTERNAL EVIDENCE

`Win32_Process` exposes management properties including command line, creation date, session ID, and process statistics.

Source: [Win32_Process](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-process).

Windows process environment functions such as `GetEnvironmentStrings` operate on the current process environment. Arbitrary external-process environment inspection is not exposed as a universal high-level supported property; debugger-style process-memory access is access-sensitive.

Sources:

- [GetEnvironmentStrings](https://learn.microsoft.com/en-us/windows/win32/api/processenv/nf-processenv-getenvironmentstrings)
- [ReadProcessMemory](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-readprocessmemory)

### ARCHITECTURAL INFERENCE

- WMI/CIM is a useful adapter, not process identity authority.
- SHELLeye knows exact cwd/environment for processes it launches.
- external process environment/cwd is optional deep inspection and may be inaccessible/unknown; the model must not pretend it is always available.

## 15. User/session and WSL context

### CURRENT EXTERNAL EVIDENCE

WTS APIs expose session information; `ProcessIdToSessionId` links a process ID to a Windows session.

Sources:

- [WTSQuerySessionInformation](https://learn.microsoft.com/en-us/windows/win32/api/wtsapi32/nf-wtsapi32-wtsquerysessioninformationw)
- [ProcessIdToSessionId](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-processidtosessionid)

### FACT

Live machine:

```text
machine-control process: NT AUTHORITY\SYSTEM
interactive desktop: STEALTHEYELLC\StealthEye, session 1
WSL: 2.7.11.0
Ubuntu-24.04: registered for StealthEye
wsl.exe from LocalSystem: WSL_E_LOCAL_SYSTEM_NOT_SUPPORTED
```

### ARCHITECTURAL INFERENCE

Execution context must include user/session reality. Future WSL execution must run in the correct user context; this is an intrinsic Windows/WSL fact, not SHELLeye policy.

## 16. Performance/resources

### CURRENT EXTERNAL EVIDENCE

Windows exposes direct process CPU/time, memory, and I/O APIs and the PDH/performance-counter subsystem for broader current performance data.

Source: [Performance Counters](https://learn.microsoft.com/en-us/windows/win32/perfctrs/performance-counters-portal).

### ARCHITECTURAL INFERENCE

Use direct cheap process statistics for tracked processes in Build 001. PDH/machine-wide resource depth is later. Resource samples are ephemeral by default.

## 17. Restart Manager and open-resource relationships

### CURRENT EXTERNAL EVIDENCE

Restart Manager can accept registered files/services/processes and return applications/services currently using those resources. `RM_UNIQUE_PROCESS` uses PID plus process start time as a unique-process witness within its model.

Source: [Restart Manager](https://learn.microsoft.com/en-us/windows/win32/rstmgr/about-restart-manager).

### ARCHITECTURAL INFERENCE

Restart Manager is a valuable on-demand relationship provider for questions such as "who is using/locking this file?" It is not a universal handle inventory and is not Build 001 core.

## 18. Linux portability comparison

### CURRENT EXTERNAL EVIDENCE

Linux has a notably strong native process-identity primitive: `pidfd_open()` returns a file descriptor referring to a task/process and is pollable for exit. Linux cgroup v2 provides process grouping/accounting/control. systemd tracks services/units using cgroups. inotify/fanotify provide filesystem event mechanisms.

Sources:

- [pidfd_open(2)](https://man7.org/linux/man-pages/man2/pidfd_open.2.html)
- [pidfd_send_signal(2)](https://man7.org/linux/man-pages/man2/pidfd_send_signal.2.html)
- [cgroup v2](https://www.kernel.org/doc/html/latest/admin-guide/cgroup-v2.html)
- [systemd](https://www.freedesktop.org/wiki/Software/systemd/)
- [Linux inotify](https://www.kernel.org/doc/html/latest/filesystems/inotify.html)

### ARCHITECTURAL INFERENCE

The SHELLeye ontology can plausibly survive a Linux provider, but the provider should exploit pidfds/cgroups/systemd rather than emulate Windows sequence numbers/Job Objects. Build 002 should be the real portability pressure test.

## 19. macOS portability comparison

### CURRENT EXTERNAL EVIDENCE

Apple's Endpoint Security API can report process execution/fork/mount/signal and other system events, subject to its platform/entitlement model. launchd/Service Management own service/background-item semantics; FSEvents exposes filesystem change streams.

Sources:

- [Endpoint Security](https://developer.apple.com/documentation/endpointsecurity)
- [Service Management](https://developer.apple.com/documentation/servicemanagement)
- [File System Events Programming Guide](https://developer.apple.com/library/archive/documentation/Darwin/Conceptual/FSEvents_ProgGuide/Introduction/Introduction.html)

### ARCHITECTURAL INFERENCE

macOS can support a later provider but has sufficiently different event/access semantics that designing a lowest-common-denominator Build 001 API now would be premature.

## 20. Current agent-shell approaches

The comparison is deliberately limited to **publicly documented interaction models**, not claims about undisclosed internal architecture.

### CURRENT EXTERNAL EVIDENCE

**Gemini CLI** documents `run_shell_command` as its primary system shell tool. On Windows it executes command strings through `powershell.exe -NoProfile -Command`, returning command/directory/stdout/stderr/exit code/background PIDs; it also exposes interactive/background shell management.

Sources:

- [Gemini CLI shell tool](https://geminicli.com/docs/tools/shell/)
- [Gemini CLI shell commands/background processes](https://geminicli.com/docs/cli/tutorials/shell-commands/)

**GitHub Copilot CLI** documents Bash/PowerShell tool sessions with list/read/write/stop operations and direct terminal/shell modes.

Sources:

- [GitHub Copilot CLI command reference](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-command-reference)
- [About GitHub Copilot CLI](https://docs.github.com/en/copilot/concepts/agents/copilot-cli/about-copilot-cli)

**Claude Code** documents terminal operation and a `Bash(...)` tool permission model, plus noninteractive/programmatic CLI modes.

Source: [Claude Code CLI reference](https://docs.anthropic.com/en/docs/claude-code/cli-usage).

**OpenAI Codex CLI** official documentation describes a local terminal coding agent that can read/modify code and run code/shell commands.

Source: [OpenAI Codex CLI getting started](https://help.openai.com/en/articles/11096431).

### ARCHITECTURAL INFERENCE

These products demonstrate that shell/background/terminal actuation is useful and mature. Their public interfaces do **not document** the combination SHELLeye is specifically targeting:

- durable logical process concepts distinct from PIDs;
- exact PID-reuse-safe actuation witnesses;
- physical file concepts distinct from paths;
- listener identity distinct from port numbers;
- service/task/job/process ontology separation;
- kernel-restart recovery of a current machine world;
- bounded semantic machine deltas/cursors;
- a deterministic local Program Host executing dozens of typed OS operations over retained objects.

That gap is SHELLeye's architectural opportunity. The conclusion is not that existing agents are weak; it is that they optimize primarily around command/terminal/file workflows rather than a persistent machine-object substrate.

## 21. Implementation language comparison

### FACT

The target already has .NET 10.0.302 and Node 24.18.1.

### ARCHITECTURAL INFERENCE

**C#/.NET 10 wins the first kernel** because it combines strong Windows native interop, async I/O/named pipes, COM, PowerShell hosting, mature SQLite support, and existing toolchain availability. Nothing in the Build 001 evidence requires a C++/Rust kernel.

Rust/C++ remain valid future native-helper choices if a specific low-level API proves materially awkward or impossible from managed code. Go/Node can perform many tasks but do not buy stronger Windows-native fidelity for this slice. Node remains excellent for the disposable Program Host.

This is an engineering fit conclusion, not a claim that C# is universally superior.

## 22. Frozen research conclusions

The final synthesis preserves the core architecture but materially strengthens several native mechanics:

1. **Process sequence number:** use Windows `SequenceNumber` on this build rather than relying only on PID + creation time.
2. **Same-handle process actuation:** after `OpenProcess`, verify creation/sequence while the same handle is held and mutate through that handle; PID reuse cannot redirect it.
3. **Process semantics:** `proc_*` is one native lifetime; restart continuity belongs in jobs/services/tasks/workloads.
4. **Creation-time job membership:** prefer `PROC_THREAD_ATTRIBUTE_JOB_LIST` on STEALTHEYELLC; suspended→assign→resume is fallback.
5. **Explicit stream-handle inheritance:** `PROC_THREAD_ATTRIBUTE_HANDLE_LIST` prevents accidental inheritance and gives persistent spools exact lifetime semantics.
6. **Job survival:** named Job Objects with live members survive kernel handle loss when kill-on-close is not set; no dedicated handle broker is required for Build 001.
7. **Persistent output:** use restart-independent spool segments/cursors; do not overclaim transparent live rotation of an already-open child handle.
8. **Parent edges:** stale parent PIDs are evidence, not exact process identity.
9. **Files:** generic SHELLeye file identity is physical; IDs can be reused over time, so actuation verifies/mutates through the same handle.
10. **NTFS gap continuity:** Build 001 uses only a narrow C: journal-ID + per-file last-USN token to prove the unchanged retained fixture file survived the kernel gap; broad USN ingestion remains deferred.
11. **Ports/listeners:** reject durable `port_*`; use transient listener identity tied to exact process plus IP Helper bind timestamp where available.
12. **Events:** notifications accelerate reconciliation; current native queries establish current truth.
13. **ETW:** broad/selective ETW remains later because no A–D identity invariant requires it.
14. **PowerShell:** host it as a structured `PSObject` provider; never make human formatting canonical truth.
15. **Terminal:** ConPTY is compatibility, not ontology.
16. **Provider topology:** most Windows providers do not need independent processes; special stateful providers must earn that boundary.
17. **No permanent handle broker:** exact process handles are opened per operation, Job Objects reopen by name, and long-lived arbitrary file handles would perturb machine semantics; no Build 001 capability justifies the extra broker.
18. **BootEpoch:** prefer the current process-telemetry `BootId` as a Windows boot witness when available, corroborated/fallbacked by persisted last-boot evidence; uncertain evidence advances the logical epoch.

No core concept/lifetime decision was invalidated by the final pass.
## 23. Remaining open questions before/inside implementation

These are deliberately **not** blockers to canonicalization:

- exact PowerShell engine/provider packaging on this machine;
- whether a separate PowerShell provider process is measurably better than in-kernel hosting;
- exact SQLite schema/indices;
- exact local RPC framing/serialization after first measurements;
- whether native process-start implementation uses only P/Invoke or mixes `System.Diagnostics.Process` for simple cases;
- listener polling interval/adaptation before later ETW work;
- completed-spool retention thresholds and whether a later dedicated stream sink is justified for active live rotation;
- exact runtime/install directories if the suggested paths conflict with implementation realities;
- later system-service/multi-session launch architecture;
- Build 002 choice of Linux/WSL provider.

## 24. Speculation not used as design basis

Possible future directions such as a kernel driver, WFP/minifilter integration, universal object-manager handle graph, always-on ETW ingestion, or persistent broker holding arbitrary native handles are **speculation** until a measured high-value capability cannot be obtained cleanly through supported user-mode APIs.
