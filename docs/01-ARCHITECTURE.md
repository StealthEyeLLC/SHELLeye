# 01 — Canonical Architecture

Status: **FINAL / SYNTHESIZED / VERIFIED / FROZEN FOR BUILD 001**
Project: **SHELLeye**  
Baseline: **2026-08-08**

## 1. Architectural thesis

SHELLeye is ChatGPT's **persistent temporal machine world**: a restartable continuity, identity, query, wait, and actuation layer over real operating-system state.

It is not the authority for Windows internals. It does not replace the Service Control Manager, Task Scheduler, NTFS/ReFS, TCP/IP stack, PowerShell, WMI/CIM, Event Log, ETW, or the native process model. Those systems remain the source of current machine truth.

SHELLeye makes their changing realities agent-addressable:

```text
Windows / native machine reality
  process manager / Job Objects / SCM / Task Scheduler
  NTFS-ReFS / IP Helper / WTS / Registry / Event Log / ETW
  PowerShell / WMI-CIM / ConPTY / WSL
                         │
                         ▼
            authoritative provider queries
             + targeted change signals
                         │
                         ▼
                  SHELLeye Kernel
        identity / continuity / reconciliation
          waits / deltas / compact projections
                         │
                         ▼
                 operating-state DB
                    SQLite WAL
                         │
                         ▼
               local Program Host SDK
                         │
                         ▼
                     ChatGPT
```

Canonical loop:

```text
observe compact current machine state
→ retain exact machine concepts
→ reason
→ act against current incarnations
→ wait on actual machine conditions
→ consume semantic deltas
→ continue
```

## 2. Core ownership rule

### Native systems own native truth

- Windows process APIs own whether a native process exists.
- Windows Job Objects own membership/accounting for native job groups created with them.
- SCM owns registered service configuration and service state.
- Task Scheduler owns registered tasks and run instances.
- Filesystems own physical files, directories, paths, links, reparse points, streams, and volume reality.
- The TCP/IP stack owns listener/connection reality.
- WTS/session APIs own Windows session reality.
- PowerShell owns PowerShell runspace/session state and the objects produced by pipelines.
- WMI/CIM, Event Log, ETW, performance APIs, Registry, and other providers own their native outputs.

### SHELLeye owns agent continuity

SHELLeye owns:

- logical agent-facing IDs distinct from raw PIDs/paths/ports/provider object instances;
- identity witnesses and exact-resolution rules;
- machine/boot/session epochs;
- current concept bindings and relationships;
- conservative stale/destroyed/ambiguous handling;
- provider/source attribution where multiple Windows views exist;
- bounded semantic deltas/cursors;
- targeted subscriptions/waits;
- synchronization/reconciliation barriers;
- restart recovery;
- structured execution context;
- local Program Host execution;
- compact ChatGPT-facing projections.

## 3. Machine identity and temporal clocks

### 3.1 Machine

`machine_*` is the stable SHELLeye installation-level concept for one operating-system machine world.

Initial identity witness:

- installation-generated random machine UUID persisted outside repositories;
- machine name and Windows installation/platform metadata as descriptive witnesses, not sole identity.

A rename of the computer should not silently create a new SHELLeye machine.

### 3.2 BootEpoch

A process cannot survive a Windows reboot. Services, registered scheduled tasks, and files often can.

Build 001 persists a SHELLeye `BootEpoch` and binds it to the strongest current Windows boot evidence available:

- **preferred target witness:** the `BootId` returned by handle-bound `ProcessTelemetryIdInformation` from the current SHELLeye process; final live probes returned the same BootId across Session 0 and the interactive session;
- **corroboration/fallback:** Windows last-boot/uptime evidence such as the operating-system `LastBootUpTime` observation persisted from the prior kernel run.

`ProcessTelemetryIdInformation` is an internal/subject-to-change Windows provider detail, so SHELLeye does not make its numeric BootId a portable protocol contract. If the preferred witness is unavailable or conflicts with persisted boot evidence, SHELLeye advances the logical `BootEpoch` conservatively rather than allowing transient objects to cross an uncertain boot boundary.

All transient native-process, task-run, terminal-process, and listener identity is scoped to the BootEpoch. Named Job Objects are also boot-local native objects even though the higher-level `job_*` descriptor may remain persisted for honest terminal-state recovery.
### 3.3 World sequence

SHELLeye maintains a monotonic observation sequence used for bounded deltas and cursors.

This sequence orders SHELLeye observations. It does **not** claim a perfect causal clock across filesystem, SCM, TCP/IP, process, PowerShell, ETW, or other independently produced state.

### 3.4 Domain lifetimes

Other lifetimes remain explicit where useful:

- user/logon/session lifetime;
- process lifetime;
- job lifetime;
- service registration/config revision;
- task registration revision / task-run lifetime;
- physical file lifetime / content-metadata revision;
- listener observation lifetime;
- terminal/runspace lifetime;
- output stream cursor.

One integer version is intentionally not forced onto all machine state.

## 4. Process model — one `proc_*`, one native lifetime

### 4.1 Canonical decision

A `proc_*` represents **exactly one native Windows process lifetime**.

It does not rebound across restart, relaunch, wrapper replacement, worker replacement, or service restart.

```text
proc_42 exits
same executable starts again
→ proc_42 remains exited/destroyed
→ proc_73 is the new process
```

This is the strongest defense against PID reuse and false process continuity.

Logical continuity across process replacement belongs to higher-level concepts:

```text
svc_7  current_process → proc_42
restart
svc_7  current_process → proc_73

job_9  current/root processes → proc_42...
worker restart
job_9  current/root processes → proc_73...
```

### 4.2 Native process witness

On the initial Windows target, the preferred observation witness is:

```text
BootEpoch
PID
SystemBasicProcessInformation.SequenceNumber
creation time
```

Microsoft documents `SystemBasicProcessInformation` as available from Windows 11 build 26100.4770 and describes its `SequenceNumber` as unique per process and specifically usable to detect PID reuse instead of `CreateTime`.

Primary source: [NtQuerySystemInformation / SystemBasicProcessInformation](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation).

The target is build 26100.8973, so Build 001 can use the sequence-number path directly while retaining a fallback design for older Windows providers.

### 4.3 Exact actuation path

No mutation operation may act on a stored PID by itself.

For a retained `proc_*`, Build 001 resolves and acts through one exact native handle:

```text
stored BootEpoch/PID/SequenceNumber/creation witness
→ OpenProcess(retained PID, required rights)
→ verify creation time on that handle with GetProcessTimes
→ while that same handle is held, verify the retained process sequence:
     preferred target corroboration: ProcessTelemetryIdInformation when available
     required fallback: fresh SystemBasicProcessInformation row for PID matches retained SequenceNumber
→ if the process exited, the witness mismatches, or access prevents exact resolution:
     return destroyed/stale/access_denied/inaccessible
→ perform wait/termination/other handle-based actuation through that same opened handle
→ close handle
```

The **opened process handle is the actuation anchor**. If the original process exits after `OpenProcess`, later PID reuse cannot redirect that handle to the replacement process; Windows keeps the handle bound to the original process object until the handle is closed. A post-open sequence mismatch therefore rejects the operation, and an exit after successful verification can only make the operation affect/fail against the original process object.

On this target, `NtQueryInformationProcess(ProcessTelemetryIdInformation)` is a useful handle-bound corroborating query because it exposes process sequence/start/boot metadata. It remains provider-internal and optional because Microsoft documents `NtQueryInformationProcess` as internal/subject to change; exact correctness does not depend on that one information class.

Primary sources:

- [Process Handles and Identifiers](https://learn.microsoft.com/en-us/windows/win32/procthread/process-handles-and-identifiers)
- [OpenProcess](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess)
- [GetProcessTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes)
- [TerminateProcess](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-terminateprocess)
- [NtQueryInformationProcess / ProcessTelemetryIdInformation](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntqueryinformationprocess)

Hard invariant:

> `terminate(old_proc)` must never terminate a different process that later reused the old PID.
### 4.4 Parentage is evidence, not magic identity

Windows can report `InheritedFromUniqueProcessId`, but a parent may already have exited and its PID may have been reused.

Therefore process lineage can have evidence quality:

- **exact** — SHELLeye observed/created the relationship with exact process incarnations;
- **resolved-current** — parent is still live and timing/sequence evidence supports the relation;
- **reported** — OS reports a parent PID but exact parent incarnation cannot be proven after the fact;
- **unknown** — insufficient evidence.

SHELLeye must not falsely attach a child to an unrelated current process merely because it inherited the same numeric PID value.

### 4.5 Process properties

Compact default process projection:

```text
proc_42
  state
  pid
  sequence
  image/name
  executable file concept when resolvable
  session
  parent relation + evidence quality
  job membership if known
  current listeners summary
  lightweight CPU/memory/I/O deltas when requested
```

Rich inspection is on demand:

- full command line;
- executable path;
- token/user/session information;
- modules;
- threads;
- handles where practical;
- environment where available;
- resource counters;
- child/descendant tree;
- stdout/stderr if SHELLeye launched/captured the workload;
- network relationships.

Command line can use supported WMI/CIM (`Win32_Process`) or other provider data when the native process API does not expose a convenient documented high-level field. Provider/source attribution is retained.

Arbitrary-process current working directory and complete environment are **not treated as universally available canonical properties**. SHELLeye knows them exactly for processes it starts. External-process environment/cwd inspection may require access-sensitive process-memory/PEB techniques and remains an optional deep provider rather than fake guaranteed truth.

## 5. Services are persistent registrations, not processes

`svc_*` represents a Service Control Manager service registration/concept.

A service may be stopped while still existing, may change PID across restart, may share a host process with other services, and may spawn workers.

Canonical relationship:

```text
svc_7
  state: running
  current_process → proc_42

restart

svc_7
  state: running
  current_process → proc_91
proc_42 → exited/destroyed
```

SCM current state comes from service APIs such as `QueryServiceStatusEx`; the returned PID is treated as a binding candidate and must be resolved to the exact current `proc_*`. Microsoft notes that the PID is not valid in every pending/stopped service state.

Service change signals should use `NotifyServiceStatusChange` where useful, with re-query/reconciliation because notifications can lag or require re-registration.

Primary sources:

- [QueryServiceStatusEx](https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-queryservicestatusex)
- [NotifyServiceStatusChange](https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-notifyservicestatuschangew)

If SHELLeye observes a service deletion and later a same-name service creation, the old service concept is destroyed and a new concept is allocated. After an observation gap, same-name resurrection is resolved conservatively rather than blindly treated as continuous.

## 6. Scheduled tasks and task runs

A registered scheduled task is not one process and not one run.

```text
task_7        registered task
  └─ taskrun_23  one execution instance
       └─ process relationships as discovered
```

Task Scheduler exposes running-task instances and an instance GUID. Use the registered task path/registration identity for `task_*` and Scheduler `InstanceGuid` for a task-run witness when available.

Primary sources:

- [IRegisteredTask::GetInstances](https://learn.microsoft.com/en-us/windows/win32/api/taskschd/nf-taskschd-iregisteredtask-getinstances)
- [IRunningTask](https://learn.microsoft.com/en-us/windows/win32/api/taskschd/nn-taskschd-irunningtask)

Build 001 does not need full Task Scheduler breadth; the ontology is frozen now so later task support does not collapse task registration into process identity.

## 7. Commands, invocations, jobs, and processes

These concepts are intentionally separate.

### CommandInvocation

`cmd_*` is the transient description/state of one requested actuation:

- provider/mode;
- executable/arguments or PowerShell operation;
- execution context;
- start/completion state;
- result/error;
- produced process/job references;
- stream cursors when applicable.

It is not a permanent action ledger and is garbage-collected after operational retention ends.

### Job

`job_*` is a SHELLeye workload/execution-group concept whose lifetime may span multiple native processes.

For SHELLeye-created grouped Windows workloads, the preferred native facet is a **named Windows Job Object** when technically compatible.

Windows Job Objects provide:

- grouping of processes as a unit;
- default descendant membership;
- accounting;
- group termination;
- completion-port notifications;
- named reopening;
- persistence after the last handle closes while associated processes remain alive.

Microsoft explicitly documents that a job object is destroyed only when its last handle has closed **and all associated processes have terminated**, unless `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` is set.

Therefore Build 001 persistent jobs:

- have unique native names persisted in operating state;
- **do not** set `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`;
- can survive a SHELLeye kernel crash while member processes remain alive;
- can be reopened by name after restart;
- use current job/process queries to reconcile membership.

Primary source: [Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects).

### Creation-time job assignment

For SHELLeye-created grouped workloads on the Build 001 target, the preferred launch path is `CreateProcessW` with `STARTUPINFOEX` and `PROC_THREAD_ATTRIBUTE_JOB_LIST`. Windows 10+ can assign the child to one or more Job Objects as part of process creation, so the intended job association exists before child user code executes.

Build 001 therefore prefers:

```text
create named Job Object
→ associate job completion port while job is still inactive
→ create restart-independent stdout/stderr handles
→ build STARTUPINFOEX attribute list:
     PROC_THREAD_ATTRIBUTE_JOB_LIST
     PROC_THREAD_ATTRIBUTE_HANDLE_LIST for only required inherited stream handles
→ CreateProcessW(... EXTENDED_STARTUPINFO_PRESENT ...)
→ retain returned exact process/thread handles and witnesses
```

The older `CREATE_SUSPENDED → AssignProcessToJobObject → ResumeThread` sequence remains a compatibility fallback if the creation-time job-list path is unavailable or incompatible with a specific workload. It is no longer the preferred Build 001 path on STEALTHEYELLC.

Explicit handle-list inheritance is a correctness/resource-lifetime primitive: persistent children inherit only the spool/stdin handles they actually require rather than every inheritable handle in the kernel process.

Do not force every external or incompatible workload into a Job Object. Existing/nested job hierarchies, explicit breakaway behavior, application expectations, and native assignment failures remain OS facts that SHELLeye reports honestly.

Primary sources:

- [UpdateProcThreadAttribute](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute)
- [Create processes](https://learn.microsoft.com/en-us/windows/win32/procthread/creating-processes)
### Job notifications are signals, not truth

Job completion-port notifications are useful for new/exited-process and active-process-zero signals. Microsoft documents that most job completion-port messages are not guaranteed to be delivered. SHELLeye therefore reconciles actual job/process state after notifications and after recovery gaps.

## 8. Persistent job output

Kernel-owned anonymous stdout/stderr pipes are a poor default for persistent jobs: if the kernel dies, its read ends disappear and the workload may observe broken pipes or lose recoverable output state.

Build 001 therefore distinguishes output modes:

- **short direct exec:** bounded in-memory/anonymous-pipe capture is acceptable;
- **persistent job:** stdout/stderr are redirected to restart-independent **per-process/per-stream spool segments** opened for append and explicitly inherited by the child.

The child holds the write handle independently of the kernel. Kernel death therefore does not close the child's spool handle. `STARTUPINFOEX` + `PROC_THREAD_ATTRIBUTE_HANDLE_LIST` limits inheritance to the required stream handles. SHELLeye tracks segment identity plus logical byte cursors and exposes:

```text
job.output(job_18, afterCursor)
```

Model-facing reads are always bounded/chunked. Completed spool segments are garbage-collectable after the operational retention window. Build 001's deterministic fixture emits bounded output; **transparent rotation of an actively inherited spool file is not a Build 001 requirement**, because renaming/rotating the pathname does not redirect a child that already holds the old file object. A later dedicated stream sink may add live rotation only if measured workloads require it.

This is restart-independent operating state, not a permanent logging/receipt architecture. Machine/power-loss durability is a different problem from SHELLeye-kernel-loss durability and does not justify per-write `FlushFileBuffers`/write-through in Build 001.

Interactive stdin/screen semantics belong to a terminal/ConPTY provider rather than being forced into every job.
## 9. ExecutionContext

Machine execution is context-dependent.

An execution context may include:

```text
user/logon/session
cwd: dir_* or explicit path
base environment + explicit overlay
executable resolution/PATH view
provider: direct | powershell | terminal | cmd | wsl | raw
encoding
shell/runspace-specific state when applicable
WSL distribution/context when applicable
```

Direct execution should not depend on a hidden global kernel working directory. Every operation carries or resolves its context explicitly.

Persistent PowerShell runspaces and terminal sessions may maintain provider-local cwd/functions/modules/variables. SHELLeye reports that state as a facet of the session/runspace instead of pretending it is global machine state.

## 10. Execution hierarchy

Preferred hierarchy:

### 10.1 Direct process execution

```text
process.start({
  executable: file_* | absolutePath,
  args: [...],
  cwd: dir_* | path,
  env: {...},
  session/context: ...,
  job: ...
})
```

No shell language is required. On .NET, `ProcessStartInfo.ArgumentList` is a useful high-level correctness primitive because arguments are supplied separately and escaped into the Windows process command line rather than requiring ChatGPT to manually construct quoting.

Primary source: [ProcessStartInfo.ArgumentList](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.argumentlist?view=net-10.0).

Where Build 001 needs suspended creation, exact handle inheritance, explicit token/session behavior, or Job Object assignment before resume, the Windows provider uses direct Win32 process creation P/Invoke rather than forcing everything through `System.Diagnostics.Process`.

### 10.2 Structured PowerShell

Use `powershell.invoke` for Windows administration breadth and object-rich cmdlets.

### 10.3 Native domain APIs

Service/file/network/session/job operations use their authoritative Windows APIs when those APIs expose stronger identity or state than a PowerShell wrapper.

### 10.4 Terminal provider

Use ConPTY when the program genuinely requires terminal/console semantics, a REPL, or interactive UI.

### 10.5 Raw shell escape hatch

`raw.exec` / raw PowerShell / cmd / WSL remains available when the task is inherently shell-oriented or no richer provider is worth building.

SHELLeye is not a universal shell-language project; the Program Host supplies programmability.

## 11. PowerShell object provider

PowerShell is a high-value structured provider, not canonical Windows truth.

Microsoft's hosting APIs support:

- creating/opening runspaces;
- persistent runspace state;
- constructing a `PowerShell` pipeline programmatically;
- synchronous or asynchronous invocation;
- receiving real `PSObject`/typed objects before human display formatting.

Primary sources:

- [Windows PowerShell Host Quickstart](https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/windows-powershell-host-quickstart)
- [Creating Runspaces](https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/creating-runspaces)
- [Adding and invoking commands](https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/adding-and-invoking-commands)

Canonical result projection should preserve useful object structure:

```text
objects[]
  typeNames
  selected/serialized properties
streams
  error
  warning
  verbose
  debug
  information
provider metadata
```

Do not call `Format-Table` and parse text to recover objects that already existed.

### Provider lifetime

The permanent architecture permits a separate restartable PowerShell provider process because persistent runspace state and engine/version isolation may genuinely justify a lifecycle boundary.

Build 001 must prove structured object return, but whether the first provider hosts Windows PowerShell 5.1 assemblies or a separately packaged modern PowerShell SDK remains an implementation experiment. The machine currently exposes Windows PowerShell 5.1 and no system `pwsh.exe` on the inspected SYSTEM PATH.

A provider restart may destroy runspace-local variables/functions/session state. It must **not** destroy SHELLeye OS concepts. The kernel reconstructs/restarts the provider and reports loss of provider-local transient state honestly.

## 12. File and directory identity

### 12.1 Physical object model

Generic SHELLeye `file_*` and `dir_*` concepts represent **physical filesystem objects**.

Windows `FILE_ID_INFO` provides:

```text
VolumeSerialNumber
128-bit FileId
```

This is the primary current physical-identity witness on NTFS/ReFS. On ReFS, 128-bit IDs matter; older 64-bit file-index fields are not sufficient for the general model.

Windows explicitly documents an important limit: file IDs are not guaranteed unique **over time** because filesystems may reuse them after deletion. Therefore `volume + FileId` is authoritative for comparing current opened objects, but it is not by itself sufficient proof that an object survived an unobserved delete/recreate gap.

Primary sources:

- [FILE_ID_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_id_info)
- [BY_HANDLE_FILE_INFORMATION — file-ID lifetime remarks](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/ns-fileapi-by_handle_file_information)
### 12.2 Path is a binding

A path is a location/name relationship, not identity.

Expected continuity:

```text
rename on same volume
→ same file_*; path binding changes

hard link added/removed
→ same file_*; path set changes

atomic replace at same path
→ old file_* destroyed/replaced; new file_*

delete + recreate same path/content
→ old file_* destroyed; new file_*

cross-volume move
→ new physical object identity on destination
```

This is intentionally different from CODEeye, which may preserve a logical source-file concept across physical editor replacement. Cross-substrate correlation handles that distinction instead of making SHELLeye file identity semantic.

### 12.3 Actuation rule

A mutation through `file_*` / `dir_*` must not verify one object and then reopen a pathname that can race to a replacement.

Canonical Build 001 pattern:

```text
retained physical witness
→ open current target (OpenFileById where appropriate, otherwise current path binding)
→ query FILE_ID_INFO on that opened handle
→ compare retained volume + 128-bit FileId
→ check any requested revision/content precondition on the same handle
→ perform WriteFile / SetFileInformationByHandle / other handle-based mutation through that same verified handle
→ close handle
```

For rename/delete/end-of-file/metadata operations, prefer `SetFileInformationByHandle` classes where Windows exposes them. If an operation cannot be completed through the verified handle without another namespace lookup, the provider must carry an equivalent native precondition through that operation or conservatively reject a race it cannot make exact.

`OpenFileById` is a useful provider primitive when supported and appropriate, but SHELLeye does not keep arbitrary long-lived file handles merely to manufacture persistence: such handles change deletion/share semantics and can perturb the machine being observed.

Primary sources:

- [OpenFileById](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-openfilebyid)
- [SetFileInformationByHandle](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle)
### 12.4 Content/metadata revision

Physical identity does not imply unchanged content. Each retained file can have current revision evidence such as:

- size;
- last-write metadata;
- content hash when required;
- reparse/link metadata;
- stream information on demand.

Stateful writes/moves/replaces can accept expected identity/revision preconditions to avoid races with concurrent machine writers.
### 12.5 Post-gap file continuity

Because file IDs can be reused after deletion, exact file recovery after an unobserved kernel gap needs more than `FILE_ID_INFO` alone.

For the Build 001 NTFS recovery fixture on `C:`, persist a **narrow continuity token** in addition to volume/FileId:

```text
USN journal ID
last file/directory USN from FSCTL_READ_FILE_USN_DATA
```

On recovery, the unchanged retained fixture file may continue as the same `file_*` only when:

- volume identity and 128-bit FileId still match;
- the NTFS journal ID is unchanged;
- the file's last USN is unchanged;
- the OS query itself is accessible/supported.

NTFS documents that a journal ID changes when prior USNs may be unusable, and all future USNs are greater than existing USNs within the valid journal chronology. Thus a delete/recreate that reuses a FileId cannot silently pass an unchanged `(journal ID, file USN)` continuity token. If the journal was reset/deleted, the file USN changed during the gap, the provider lacks the required access, or any witness disagrees, SHELLeye does **not** manufacture continuity: the retained concept becomes stale/ambiguous/destroyed as evidence permits and current reality may be promoted separately.

This is deliberately **not full USN-journal ingestion**. Build 001 does not scan/replay the journal to reconstruct event history. A later provider may use journal records to prove continuity through files that legitimately changed during an observation gap.

The `X:` ReFS smoke case does not claim equivalent post-gap continuity while its change journal is inactive; it proves current 128-bit physical identity only.

Primary sources:

- [FSCTL_READ_FILE_USN_DATA](https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_read_file_usn_data)
- [Using the Change Journal Identifier](https://learn.microsoft.com/en-us/windows/win32/fileio/using-the-change-journal-identifier)

## 13. Filesystem change ingestion

### Build 001

Use:

- native file identity queries for truth;
- `ReadDirectoryChangesW` / .NET watcher mechanisms for low-latency dirty signals;
- scoped snapshot/reconciliation after watcher overflow, provider restart, or kernel gap.

Primary sources:

- [ReadDirectoryChangesW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-readdirectorychangesw)
- [FileSystemWatcher.Error](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.error)

Watcher events do not become a permanent historical ledger.

### USN journal

Broad USN journal ingestion/replay remains post-Build-001, but final verification found one narrow Build 001 use that materially improves correctness: **NTFS retained-file continuity across kernel gaps**.

Build 001 therefore uses `FSCTL_QUERY_USN_JOURNAL` + `FSCTL_READ_FILE_USN_DATA` only to persist/compare the journal ID and last-USN continuity token for the retained `C:` fixture file. It does not consume a volume event stream, reconstruct missed history, or require SHELLeye to create/resize a journal.

The live target currently has an active journal on `C:` and no active journal on `X:`. Exact gap continuity is consequently provider/capability-specific; absence of a valid token reduces continuity rather than creating a false rebound.

Full journal scanning/checkpoint recovery remains later work because it adds history-window, truncation, journal-reset, privilege, and provider-specific complexity that A-D do not otherwise require.

Primary sources:

- [FSCTL_READ_FILE_USN_DATA](https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_read_file_usn_data)
- [FSCTL_QUERY_USN_JOURNAL](https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_query_usn_journal)
- [Using the Change Journal Identifier](https://learn.microsoft.com/en-us/windows/win32/fileio/using-the-change-journal-identifier)
## 14. Volumes, reparse points, links, and streams

`vol_*` is a stable volume concept based on Windows volume identity (volume GUID path/serial/filesystem data), not only `C:`/`X:` drive letters.

Drive letters and mount points are bindings that may change.

File inspection should preserve reparse-point/symlink/junction facts instead of silently canonicalizing away machine topology. Alternate data streams are provider-visible on demand, but individual streams need not receive durable IDs unless retained.

## 15. Network model — endpoint values, transient listeners

### 15.1 Port is not identity

Reject a durable `port_*` concept.

A port number is a scalar component of an endpoint. Windows may expose the same local port at different times to unrelated processes.

### 15.2 Listener

`listener_*` is a transient observed machine concept, approximately:

```text
protocol
address family
local address
local port
owning proc_* exact incarnation
TCP bind/create timestamp when available
observation generation
```

For Build 001, query `GetExtendedTcpTable` with the `TCP_TABLE_OWNER_MODULE_LISTENER` class where available. `MIB_TCPROW_OWNER_MODULE` adds `liCreateTimestamp`, the time the context bind that created the TCP link occurred, alongside the owning PID. Resolve that PID immediately to the exact current `proc_*`.

The bind timestamp is a useful additional listener-incarnation witness, **not a documented globally unique socket ID**. After an unobserved kernel gap, SHELLeye can reconstruct current endpoint reality and compare witnesses, but it still must not claim uninterrupted native-socket continuity solely because endpoint/PID/timestamp values match.

Primary sources:

- [GetExtendedTcpTable](https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-getextendedtcptable)
- [MIB_TCPROW_OWNER_MODULE](https://learn.microsoft.com/en-us/windows/win32/api/tcpmib/ns-tcpmib-mib_tcprow_owner_module)

Hard rule:

```text
proc A owns :8080 / listener_A
A exits
proc B later owns :8080
→ listener_A remains closed/destroyed
→ B is a new listener concept
```

Even if the endpoint tuple is identical, old `listener_*` must not silently rebound. After a kernel observation gap, continuity is conservative; current listener rediscovery does not invent proof that the same native socket remained continuously open.
### 15.3 Connections

TCP/UDP connection/endpoint rows are high-cardinality and short-lived. Return them as query records by default; promote to `conn_*` only when ChatGPT retains/watches one.

Exact socket-handle identity for arbitrary external processes is not Build 001 core. IP Helper tables provide current endpoint/owner truth, not a permanent socket object key.

## 16. Pipes

Named pipes are genuine machine objects but can be numerous and ephemeral. Build 001 uses Windows named pipes for SHELLeye IPC but does not need a complete generic `pipe_*` ontology.

Generic named-pipe discovery/retention is core-later. Promote a pipe concept when ChatGPT cares about a particular endpoint rather than enumerating every pipe eagerly.

## 17. User/logon/session model

Windows Session 0, the interactive console session, RDP sessions, service sessions, and user logons are materially different execution realities.

`session_*` should preserve enough information to avoid machine confusion:

- Windows session ID;
- user SID/account when available;
- protocol/console/RDP evidence;
- logon/session lifetime evidence;
- active/connected/disconnected state;
- relationship to processes and execution contexts.

Primary sources:

- [WTSQuerySessionInformation](https://learn.microsoft.com/en-us/windows/win32/api/wtsapi32/nf-wtsapi32-wtsquerysessioninformationw)
- [ProcessIdToSessionId](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-processidtosessionid)

This is state representation, not a privilege policy.

The live machine provides an immediate example: the machine-control service runs as `NT AUTHORITY\SYSTEM`, while the interactive `STEALTHEYELLC\StealthEye` desktop is session 1. WSL 2 is installed for that user, but invoking WSL from LocalSystem returns `WSL_E_LOCAL_SYSTEM_NOT_SUPPORTED`. Therefore future `wsl.exec` must deliberately target the relevant user/session context rather than assuming SYSTEM context is interchangeable.

## 18. Resources and performance

For retained processes, prefer direct per-process APIs for cheap current CPU/time, memory, handle count, and I/O data.

PDH/performance counters remain valuable for broader machine/resource queries and later provider breadth.

Resource samples are ephemeral observations by default. Do not allocate durable concepts or permanently persist every sample.

Primary source: [Performance Counters / PDH](https://learn.microsoft.com/en-us/windows/win32/perfctrs/performance-counters-portal).

## 19. Handles and file/process relationships

Do not build an eager universal handle graph in Build 001.

Reasons:

- arbitrary-handle enumeration is expensive/high-cardinality and often access-sensitive;
- many useful questions can be answered with a targeted domain API;
- retaining handles can alter object lifetime/semantics.

Useful on-demand providers include:

- modules/threads via supported process APIs;
- Restart Manager for "which applications/services are using these registered file resources?" relationships;
- provider-specific native inspection when a concrete task requires it.

Primary source: [Restart Manager](https://learn.microsoft.com/en-us/windows/win32/rstmgr/about-restart-manager).

Undocumented/native object-manager handle enumeration remains experimental until a concrete capability justifies its complexity.

## 20. Registry

Registry is a native machine domain with current-state APIs and change notifications (`RegNotifyChangeKeyValue`). It is core-later, not Build 001 breadth.

Registry keys/values should use domain-specific paths/handles and revisions rather than being forced into generic file concepts.

Primary source: [Registry Functions](https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry-functions).

## 21. WMI/CIM

WMI/CIM is a valuable breadth and compatibility provider, especially for management properties such as process command line, but it is not globally canonical truth.

The representation broker chooses it when the requested property/domain is well represented there. Native process/file/job/service/network APIs remain preferred when stronger identity/freshness is available.

Primary source: [Win32_Process](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-process).

## 22. ETW and Windows Event Log

ETW can expose rich process/network/system events and Event Log exposes durable OS/application event channels and subscriptions.

They are evidence/event providers, not the default world database.

Build 001 deliberately does **not** ingest broad ETW. Native process handles, Job Object events, SCM notifications, file watchers, IP Helper queries, and reconciliation are sufficient to prove the identity/recovery spine without turning SHELLeye into telemetry infrastructure.

Selective ETW can be added later where it uniquely improves external-process lifecycle/network visibility or reduces polling.

Primary sources:

- [Event Tracing for Windows](https://learn.microsoft.com/en-us/windows-hardware/test/weg/instrumenting-your-code-with-etw)
- [EvtSubscribe](https://learn.microsoft.com/en-us/windows/win32/api/winevt/nf-winevt-evtsubscribe)

## 23. Terminal / ConPTY

A terminal is a compatibility provider, not canonical machine ontology.

Use ConPTY for:

- REPLs;
- console programs that require a terminal;
- interactive programs;
- compatibility with tools whose behavior changes under redirected pipes.

Microsoft's current pseudoconsole API includes `ReleasePseudoConsole` on Windows 11 24H2 build 26100, allowing the host to release ownership so the pseudoconsole exits after connected clients disconnect. This reinforces the distinction between "initial shell process exited" and "terminal workload is actually finished."

Primary sources:

- [CreatePseudoConsole](https://learn.microsoft.com/en-us/windows/console/createpseudoconsole)
- [ReleasePseudoConsole](https://learn.microsoft.com/en-us/windows/console/releasepseudoconsole)

ConPTY is post-Build-001 unless an acceptance fixture unexpectedly requires terminal semantics.

## 24. Wait architecture

Core wait primitives target actual conditions.

### Process exit

Exact process handle wait; after kernel recovery reopen by PID and verify the stored incarnation first.

### Job membership/empty/completion

Job Object completion port as low-latency signal plus `QueryInformationJobObject` / exact process reconciliation.

### Service state

`NotifyServiceStatusChange` signal plus `QueryServiceStatusEx` current truth.

### File state/change

Directory/file watcher dirty signal plus file-identity/revision re-query; reconcile on overflow/gap.

### Listener state

Targeted IP Helper query loop in Build 001 because no listener-table event primitive provides an equivalent exact wait. Poll only the relevant endpoint/process interest rather than the whole machine at model cadence.

### Output

Wait for spool/stream cursor advancement or pattern predicate.

### General API shape

```text
wait.process_exit(proc_*)
wait.job_empty(job_*)
wait.service_state(svc_*, state)
wait.file(predicate)
wait.listener(predicate)
wait.output(job_*, afterCursor | match)
```

Timeout is a property of the wait request, not an excuse to replace condition waits with arbitrary `sleep` calls.

## 25. Delta engine

Normal observations return compact semantic change records:

```json
{
  "cursor": 419,
  "changed": [
    {"id":"proc_42","change":"exited","exitCode":0},
    {"id":"proc_73","change":"started","job":"job_18"},
    {"id":"listener_9","change":"opened","owner":"proc_73","local":"127.0.0.1:8080"}
  ]
}
```

Candidate event families:

```text
process.started / exited / changed
job.member_added / member_exited / empty / changed
service.state_changed / process_changed
file.created / changed / renamed / replaced / deleted
directory.changed
listener.opened / closed
connection.opened / closed (only watched/promoted)
task.started / completed
terminal.output
resource.changed
provider.restarted
world.reconciled
```

The bounded delta window is operating state. On cursor expiry:

```text
cursor_expired
```

with enough scope information for a targeted current-state resync. SHELLeye never pretends missing historical events are reconstructable if they are not.

## 26. `world.sync` coherence primitive

SHELLeye provides a synchronization barrier conceptually like:

```text
world.sync({
  processes: retained interests,
  files: retained interests,
  services: retained interests,
  network: retained interests
})
```

Meaning:

> incorporate currently known dirty signals and reconcile the requested views against authoritative current providers before returning.

It is not a verification pipeline and does not imply global quiescence of the machine. Other software can change the machine immediately after the barrier.

## 27. Representation / actuation broker

There is no simplistic global provider ranking. Selection is operation-specific.

Examples:

```text
start exact process          → native process creation
process identity             → SystemBasicProcessInformation + handle verification
wait exact process exit      → process handle
manage SHELLeye workload     → Windows Job Object
query/mutate service         → SCM
registered task/run          → Task Scheduler COM
physical file identity       → CreateFile/GetFileInformationByHandleEx
filesystem dirty signal      → ReadDirectoryChangesW
find TCP listener owner      → IP Helper
rich Windows admin query     → structured PowerShell
management compatibility     → WMI/CIM
interactive console program  → ConPTY
OS/application event query   → Windows Event Log
selective high-rate events   → ETW where justified
generic legacy command       → raw process/shell
WSL workload                 → WSL provider in correct user/session context
```

Selection criteria:

- object identity quality;
- source authority;
- freshness;
- event availability;
- latency/cost;
- information density;
- recovery semantics;
- operation correctness;
- required intrinsic OS access.

Provider choice is not a permission class.

## 28. Concept registry and lazy promotion

Build 001 first-class concepts:

```text
machine
boot
session
process
job
command invocation
file
directory
volume
service
listener
execution context
```

Registered task/task-run concepts are architecturally defined but may remain core-later if not required by the first slice.

Query-derived/high-cardinality records by default:

```text
thread
module
handle
connection
resource sample
environment variable
filesystem event
output line
```

Promote them when ChatGPT retains/watches/acts on one.

## 29. Lifecycle and resolution vocabulary

SHELLeye does not force every domain through CODEeye's lifecycle vocabulary.

Common resolution outcomes:

```text
current        exact current object resolved
exited         known process/job member has ended
stale          retained state/revision no longer current
destroyed      identity-bearing object no longer exists
ambiguous      multiple plausible current bindings and no exact winner
inaccessible   object may exist but OS denied required inspection/access
unknown        provider gap prevents a stronger classification
```

A provider-specific `rebound` may exist for a genuinely persistent logical object whose native realization changed, but **processes and listeners do not rebound across replacement**.

File rename is not rebound: physical identity is unchanged. File replacement is a new object.

## 30. Persistence architecture

Initial store: **SQLite WAL** outside target repositories.

Persist the minimum operating state required for continuity/correctness/performance, such as:

```text
machine + boot epochs
concept registry
current native identity witnesses
process current/exited bindings needed for retained handles
named job descriptors + current membership witnesses
service/task descriptors when promoted
file/directory identities + current path bindings + current revision evidence
volume identities
listener current/promoted state
execution-context descriptors
runtime/provider discovery descriptors
output-spool descriptors/cursors
promoted interests/watches
bounded delta ring + cursor metadata
```

Do not persist every process ever seen, every command forever, all stdout forever, all filesystem events, all ETW, all connections, or every resource sample.

## 31. Recovery architecture

### 31.1 Kernel recovery

1. kernel dies;
2. external OS objects continue according to native semantics;
3. named Job Objects with live members remain reopenable when not configured kill-on-close;
4. persistent workloads continue writing restart-independent output spools;
5. kernel restarts;
6. detect/validate BootEpoch;
7. reopen/reconcile retained jobs;
8. enumerate current process identity with sequence numbers and validate retained process witnesses;
9. reconcile retained file identities/paths, including the C: NTFS journal-ID + last-USN continuity token where exact file continuity is claimed; reconcile services, listeners, sessions, and other interests;
10. reestablish watchers/providers;
11. emit compact recovery delta including any gaps/uncertainty.

No claim is made that every event during the gap can be reconstructed.

### 31.2 Provider recovery

If PowerShell or another special provider process dies:

- restart/rebuild provider state;
- increment provider epoch;
- preserve OS-level SHELLeye concepts;
- invalidate only provider-local transient handles/runspace state that genuinely died;
- reconcile current provider output on demand.

### 31.3 Boot transition

On new BootEpoch:

- all old native `proc_*`, transient listeners/connections, terminal processes, and process-run state are terminal/destroyed;
- services, registered tasks, files/directories/volumes are reconciled as persistent domains;
- named job objects from the old boot cannot survive and are closed as old-lifetime state;
- no transient process handle is rebound across boot.

## 32. Cross-substrate composition

SHELLeye deliberately does not create a giant universal ontology.

A minimal cross-substrate link records:

```text
source substrate + concept id
target substrate + concept id
relation kind
current machine witness/correlation evidence
```

Examples:

```text
SHELLeye proc_42  ↔ eyeBROWSE browser_3
SHELLeye file_91  ↔ CODEeye file_27
SHELLeye proc_51  ↔ CODEeye build/process facet
SHELLeye file_20  ↔ DOCSeye document_8
```

SHELLeye owns the process/file side; sibling substrates own their semantic side.

Cross-substrate links are sparse and created only when composition needs them.

## 33. ChatGPT-facing API

The local SDK may be rich:

```text
machine.*
process.*
job.*
service.*
task.*
file.*
directory.*
volume.*
network.*
powershell.*
terminal.*
resource.*
events.*
wait.*
raw.*
```

The top-level model surface should remain small. The initial design target is approximately:

```text
machine.query    structured inspect/find/project/query
machine.program  run one local JS machine program
machine.wait     simple direct condition wait
machine.content  explicit file/stream/range content retrieval
machine.exec     execution front door / simple actuation
```

Exact MCP/tool names are transport details and remain replaceable.

The critical design rule is that ChatGPT should not load 150 primitive tool schemas when one Program Host call can execute a rich local SDK.

## 34. Program Host

Initial language: **Node.js 24**.

```text
ChatGPT
   ↓
one disposable Program Host invocation
   ↓
one persistent kernel connection
   ↓
25–200 typed SHELLeye operations
   ↓
local waits / loops / branches
   ↓
one compact machine result
```

The Program Host owns no canonical state. Kernel/providers/OS own the world. Program programs can end without invalidating machine concepts.

Node is chosen for the Program Host because it is already available and proven in StealthEye sibling substrates; the Windows kernel remains C# where native-system fidelity matters most.

## 35. Implementation language and topology

### Kernel: C# / .NET 10

Frozen first choice.

Why it wins on this target:

- direct managed access to async I/O, named pipes, SQLite ecosystem, and Windows integration;
- straightforward P/Invoke for process/job/file/network/session APIs;
- strong COM interop for Task Scheduler and other Windows components;
- natural PowerShell hosting seam;
- installed .NET 10 SDK/runtime;
- no demonstrated native capability that requires a C++/Rust kernel for Build 001.

### Native helpers

Allowed only if a measured API/interop problem earns one. Do not add Rust/C++ for prestige.

### Provider processes

Most Windows state is already external to SHELLeye. Therefore native process/file/network/service providers begin as conceptual/in-process kernel modules.

Separate processes are reserved for stateful/special providers whose independent lifecycle actually matters, initially PowerShell if implementation experiments confirm the value.

This intentionally differs from CODEeye's persistent Roslyn-host topology.

## 36. Portability comparison

Windows-first does not mean lowest-common-denominator abstractions.

Later providers can map the same high-level truths using strong native primitives:

- Linux `pidfd` gives a stable process reference independent of numeric PID reuse and pollable exit semantics; cgroup v2 provides process grouping/accounting; systemd units separate service identity from processes; inotify/fanotify provide filesystem event sources.
- macOS has launchd/service-management semantics, FSEvents, and Endpoint Security event streams for process execution/fork/mount/signal events, subject to platform entitlements.

Primary comparison sources:

- [Linux pidfd_open(2)](https://man7.org/linux/man-pages/man2/pidfd_open.2.html)
- [Linux cgroup v2](https://www.kernel.org/doc/html/latest/admin-guide/cgroup-v2.html)
- [systemd](https://www.freedesktop.org/wiki/Software/systemd/)
- [Apple Endpoint Security](https://developer.apple.com/documentation/endpointsecurity)
- [Apple Service Management](https://developer.apple.com/documentation/servicemanagement)

Build 001 does not weaken the Windows provider to fit those systems prematurely. A second platform later pressure-tests which abstractions are truly portable.

## 37. What is deliberately not canonical truth

The following may be provider evidence or escape hatches but are not universal identity sources:

- formatted PowerShell text;
- `tasklist` output;
- process name alone;
- PID alone;
- path alone;
- port alone;
- `netstat` text;
- watcher event history alone;
- WMI object instance identity alone;
- ETW event history alone;
- a terminal screen/transcript;
- a SHELLeye command history.

## 38. Architecture success criterion

SHELLeye succeeds when ChatGPT can retain a compact machine world, safely continue through normal OS churn and SHELLeye restarts, and perform local multi-step programs without repeatedly reconstructing machine identity from shell text.

The strongest invariant is:

> **Continuity is earned by native identity evidence. When evidence is insufficient, SHELLeye loses continuity rather than acting on the wrong machine object.**

### Build 001 measured Job Object lifetime correction

Live Build 001 execution on STEALTHEYELLC disproved one native lifetime assumption: after the kernel's last Job Object handle closed, `OpenJobObject` could not reopen the named job even while assigned member processes remained alive. An isolated cross-process probe reproduced the behavior.

Build 001 therefore adds one narrow persistence primitive without changing `job_*` semantics: immediately after creation-time `PROC_THREAD_ATTRIBUTE_JOB_LIST` assignment, SHELLeye duplicates the Job Object handle into the created root process with `DuplicateHandle(..., bInheritHandle = FALSE, DUPLICATE_SAME_ACCESS)`. The workload does not use this handle; its handle table simply keeps the native named object reopenable while the workload root remains alive. The kernel may die and later reopen the same named Job Object. The duplicate is non-inheritable and closes naturally when the root exits. There is no separate handle broker and no controller-owned workload lifetime.

This is now part of the canonical persistent-job launch path on STEALTHEYELLC.
