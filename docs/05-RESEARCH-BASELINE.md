# 05 — Research Baseline

Status: **Canonical evidence baseline for the 2026-08-08 architecture pass**

This document records the external/current evidence that materially shaped SHELLeye. It intentionally distinguishes fact, evidence, inference, open questions, and speculation.

Terminology:

- **FACT** — directly observed on STEALTHEYELLC or a stable native-system property used by the design.
- **CURRENT EXTERNAL EVIDENCE** — supported by current primary/authoritative documentation reviewed during this pass.
- **ARCHITECTURAL INFERENCE** — a SHELLeye design conclusion drawn from facts/evidence.
- **OPEN QUESTION** — implementation measurement/compatibility work deliberately left unresolved.
- **SPECULATION** — plausible but not used as a frozen design basis.

## 1. Process identity and PID reuse

### CURRENT EXTERNAL EVIDENCE

Microsoft now documents `SystemBasicProcessInformation` under `NtQuerySystemInformation`, available from Windows 11 version/build **26100.4770**. Its `SYSTEM_BASICPROCESS_INFORMATION` contains:

```text
UniqueProcessId
InheritedFromUniqueProcessId
SequenceNumber
ImageName
```

Microsoft explicitly describes `SequenceNumber` as a unique value assigned to each process and usable to detect PID reuse instead of process creation time. Microsoft also notes this basic process class is faster/lower-memory than the older full process information class for basic enumeration.

Source: [NtQuerySystemInformation / SystemBasicProcessInformation](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation).

Microsoft process documentation also establishes that process IDs identify a process only during its lifetime and that open process handles remain references to the process object until the handle is closed. Process creation/exit times are available through `GetProcessTimes`; process termination is asynchronous and an exact process handle can be waited on.

Sources:

- [Process Handles and Identifiers](https://learn.microsoft.com/en-us/windows/win32/procthread/process-handles-and-identifiers)
- [OpenProcess](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess)
- [GetProcessTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes)
- [TerminateProcess](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-terminateprocess)

### FACT

STEALTHEYELLC is Windows build **26100.8973**, newer than the documented `SystemBasicProcessInformation` availability floor.

### ARCHITECTURAL INFERENCE

- `PID != proc_* identity`.
- Build 001 process witness should use `BootEpoch + PID + SequenceNumber`, strengthened by creation time and handle verification.
- `proc_*` should represent one native process lifetime and never rebound across restart.
- before mutation, reopen the current process and verify the incarnation; never call process mutation by stored PID alone.
- process restart continuity belongs to `job_*`, `svc_*`, registered task/workload concepts, not `proc_*`.

### OPEN QUESTION

The documented `NtQuerySystemInformation` page notes that the API may change and recommends alternatives for many information classes. Build 001 should isolate this call behind the Windows process provider and retain a creation-time fallback for older Windows versions rather than making the NT query shape a cross-platform contract.

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

- Job Objects manage groups of processes as a unit.
- child processes are associated with the job by default unless breakaway behavior changes that.
- nested jobs are supported on modern Windows.
- jobs can report events through I/O completion ports.
- most completion-port messages are not guaranteed, so lack of a message is not proof that an event did not occur.
- named jobs can be reopened with `OpenJobObject`.
- a Job Object is destroyed when the **last handle is closed and all associated processes have terminated**.
- `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` changes that behavior by terminating associated processes when the last handle is closed.

Source: [Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects).

### ARCHITECTURAL INFERENCE

This is a stronger Build 001 primitive than a custom process-supervisor database alone.

A SHELLeye persistent `job_*` should, when compatible:

- have a unique named Windows Job Object facet;
- launch the initial process suspended, assign, then resume when exact descendant grouping matters;
- not enable kill-on-close for persistent jobs;
- use completion-port messages as dirty/change signals and current job/process queries as truth;
- reopen the native job by name after kernel restart while members remain alive.

No separate native handle-broker process is required merely to keep a Job Object alive across kernel death.

### OPEN QUESTION

Some workloads have existing/nested job expectations or explicit breakaway behavior. Build 001 must prove the fixture path, while the general provider must allow "not grouped / grouping unsupported" instead of forcing every process into a Job Object.

## 4. Persistent stdout/stderr

### FACT

If a kernel owns anonymous pipe read ends and dies, the I/O topology itself changes even if the child process should remain alive.

### ARCHITECTURAL INFERENCE

For persistent jobs, kernel-owned anonymous pipes are the wrong default persistence primitive. Redirect output to bounded restart-independent spool files (or an equivalent sink) and expose cursor reads. The workload can continue writing during kernel gaps; the kernel can resume reading after restart.

This is operational state, not an action ledger. Spools are bounded/rotated/garbage-collected with job retention.

## 5. File identity

### CURRENT EXTERNAL EVIDENCE

Windows `FILE_ID_INFO` contains:

```text
VolumeSerialNumber
FILE_ID_128 FileId
```

Microsoft documents the combination as uniquely identifying a file on a single computer while comparing open handles. The 128-bit form is important for a filesystem-general Windows model including ReFS.

Source: [FILE_ID_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_id_info).

Windows also provides `OpenFileById` for opening a file by its identifier where supported.

Source: [OpenFileById](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-openfilebyid).

Windows hard links establish that one physical file can have multiple directory entries/paths.

Source: [Hard Links and Junctions](https://learn.microsoft.com/en-us/windows/win32/fileio/hard-links-and-junctions).

### ARCHITECTURAL INFERENCE

Generic SHELLeye `file_*` / `dir_*` must represent physical filesystem objects, not paths and not semantic documents.

- same-volume rename: same physical concept;
- hard-link path changes: same physical concept;
- atomic replacement: old physical concept replaced/destroyed, new concept at same path;
- delete/recreate: new concept even with identical path/content;
- cross-volume move: new destination physical identity.

A file mutation by retained concept must re-open/re-resolve and compare physical identity before acting.

SHELLeye intentionally differs from CODEeye here: CODEeye may preserve semantic source-file continuity across an editor's physical replacement; SHELLeye describes the machine file that actually exists.

## 6. Filesystem events and USN

### CURRENT EXTERNAL EVIDENCE

`ReadDirectoryChangesW` provides asynchronous/synchronous directory change notifications. .NET `FileSystemWatcher` documentation warns its internal buffer can overflow; when it does, changes can be missed and an error is raised.

Sources:

- [ReadDirectoryChangesW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-readdirectorychangesw)
- [FileSystemWatcher.Error](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.error)

The Windows change journal is a persistent per-volume change mechanism and uses journal IDs/USNs so consumers can detect invalidated history. Windows documentation covers NTFS/ReFS record formats and 128-bit file-ID support in later journal data structures.

Source: [Change Journals](https://learn.microsoft.com/en-us/windows/win32/fileio/change-journals).

### FACT

Live machine probe:

```text
C: NTFS — USN journal active
X: ReFS — "The volume change journal is not active"
```

### ARCHITECTURAL INFERENCE

Build 001 architecture:

```text
watcher/native event → low-latency dirty signal
physical identity/current query → truth
scoped reconciliation → gap/overflow recovery
USN → later optional volume-scale catch-up provider
```

Requiring USN for Build 001 would add complexity without being necessary to prove exact identity/recovery, and would make the first slice depend on a journal not active on `X:`.

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

Windows IP Helper `GetExtendedTcpTable` can return IPv4/IPv6 TCP tables including owner-PID listener/connection rows (`MIB_TCPTABLE_OWNER_PID` and IPv6 equivalents). Similar UDP owner-PID tables exist.

Source: [GetExtendedTcpTable](https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-getextendedtcptable).

### ARCHITECTURAL INFERENCE

- a port number is not a durable object identity;
- a `listener_*` is transient and must include the exact owning process incarnation plus endpoint/protocol observation;
- closing/reopening the same port creates a new listener concept;
- after an unobserved kernel gap, SHELLeye may report current endpoint state without inventing proof that the same socket persisted continuously.

Exact arbitrary external socket-handle identity is deferred; it would require a stronger provider than owner-PID tables.

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

The research pass materially upgraded the prompt's initial assumptions in these ways:

1. **Process sequence number:** use the new Windows `SequenceNumber` capability on this build rather than relying only on PID + creation time.
2. **Process semantics:** `proc_*` is one native lifetime; do not invent process rebinding. Put restart continuity in jobs/services/tasks.
3. **Job survival:** named Windows Job Objects can survive kernel handle loss while member processes live; no dedicated handle broker is required for Build 001.
4. **Persistent output:** use restart-independent bounded spools for persistent jobs instead of kernel-owned pipes.
5. **Parent edges:** treat stale parent PIDs as evidence, not exact process identity.
6. **Files:** generic SHELLeye file identity is physical, not path/logical-document identity.
7. **Ports:** reject durable `port_*`; use transient listeners tied to exact process incarnation.
8. **Events:** notifications accelerate reconciliation; they do not replace current native queries.
9. **USN/ETW:** both are powerful but deferred because they are not required to prove the spine.
10. **PowerShell:** host it as a structured `PSObject` provider; never make human formatting canonical truth.
11. **Terminal:** ConPTY is compatibility, not ontology.
12. **Provider topology:** most Windows providers do not need independent processes; special stateful providers must earn that boundary.

## 23. Remaining open questions before/inside implementation

These are deliberately **not** blockers to canonicalization:

- exact PowerShell engine/provider packaging on this machine;
- whether a separate PowerShell provider process is measurably better than in-kernel hosting;
- exact SQLite schema/indices;
- exact local RPC framing/serialization after first measurements;
- whether native process-start implementation uses only P/Invoke or mixes `System.Diagnostics.Process` for simple cases;
- listener polling interval/adaptation before later ETW work;
- output spool rotation thresholds;
- exact runtime/install directories if the suggested paths conflict with implementation realities;
- later system-service/multi-session launch architecture;
- Build 002 choice of Linux/WSL provider.

## 24. Speculation not used as design basis

Possible future directions such as a kernel driver, WFP/minifilter integration, universal object-manager handle graph, always-on ETW ingestion, or persistent broker holding arbitrary native handles are **speculation** until a measured high-value capability cannot be obtained cleanly through supported user-mode APIs.
