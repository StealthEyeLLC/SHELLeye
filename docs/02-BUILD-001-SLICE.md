# 02 — Build 001: Machine World Kernel Slice

Status: **FINAL SPEC / FROZEN FOR BUILD 001 — PLANNED / NOT IMPLEMENTED**
Priority: **P0 — prove the machine-world spine before breadth**  
Target: **STEALTHEYELLC / Windows 11 build 26100.8973**

## 1. Purpose

Build 001 is SHELLeye's first product implementation. It is not disposable scaffolding.

Its job is to prove the permanent architectural spine with four hard milestones:

### Milestone A — Persistent Machine World

> A SHELLeye-created long-running grouped workload, exact retained process/job/file concepts, restart-safe output, and current machine state survive a hard SHELLeye kernel death and are conservatively recovered after restart without making the workload a child-lifetime artifact of the controller.

### Milestone B — Persistent Machine Objects / Delta First

> ChatGPT can operate on compact machine concepts, relationships, condition waits, and bounded semantic deltas instead of repeatedly dumping and parsing full process/file/network/service tables.

### Milestone C — Recovery Continuity / Identity Killer

> Hostile process/file/listener/recovery cases produce zero false rebounds and zero wrong-object mutations. An old process handle can never target a new process merely because Windows reused its PID; an old file handle can never mutate a replacement merely because it occupies the same path; an old listener handle never becomes a later listener merely because the port was reused.

### Milestone D — Programmable Machine Operation

> One local Node 24 Program Host invocation executes at least 30 meaningful typed machine operations, local waits/branches included, and returns one compact result with no model round trip between primitives.

These four gates define Build 001 complete. Do not create `09-BUILD-001-RESULTS.md` until all four gates have actually run and passed.

## 2. Build philosophy

### Permanent vertical slice

Every Build 001 component must contribute to A–D. Do not add broad ETW ingestion, full USN journal ingestion/replay, full Task Scheduler mutation, ConPTY, WSL execution, registry ontology, remote machines, Linux/macOS providers, cloud orchestration, or universal handle graphs merely for completeness.

### Identity before breadth

The decisive product property is not how many Windows commands SHELLeye can wrap. It is whether retained machine concepts remain exact enough for ChatGPT to operate without wrong-target actions.

### Current reality remains authoritative

Event streams and cached state accelerate operation. Native re-query/reconciliation determines current truth.

### No artificial process survival

A persistent workload must survive because Windows owns the workload independently of the kernel, not because the acceptance test secretly keeps the kernel alive.

## 3. Exact initial stack

### Kernel

- C# / .NET 10 (`net10.0`);
- one restartable headless `SHELLeye.Kernel` process;
- Windows provider modules in-process unless a separate lifecycle is technically justified;
- source-generated or ordinary P/Invoke for required Win32/NT APIs;
- Windows named-pipe local RPC;
- framed JSON/NDJSON first; do not optimize encoding before measurement.

### State

- SQLite WAL;
- outside the repository;
- schema focused on current/recoverable operating state;
- bounded delta storage, not permanent history.

### Program Host

- portable Node.js 24.18.1 already installed at `C:\AgentBrowser\tools\node-v24.18.1-win-x64`;
- disposable process by default;
- typed JavaScript SDK over the kernel RPC surface;
- owns no canonical machine state.

### Structured PowerShell

- hosted runspace provider returning `PSObject` structure rather than formatted text;
- Build 001 implementation must test whether to host the available Windows PowerShell 5.1 engine or a separately packaged compatible modern PowerShell SDK;
- a separate `SHELLeye.PowerShell` provider process is preferred if it materially simplifies engine/version isolation and persistent runspace state;
- the provider boundary is not required to survive as a separate process if implementation evidence shows an in-kernel runspace is simpler without compromising recovery.

### Fixture

- tiny deterministic Node HTTP workload using the already installed portable Node runtime;
- no framework dependency required;
- fixture can spawn one child process, bind loopback port 0, read a config file, emit deterministic stdout/stderr records, and shut down on a simple control condition/signal.

## 4. Initial repository/project shape at implementation time

The setup pass intentionally creates no product code. Build 001 should begin with the smallest useful shape:

```text
SHELLeye/
├─ src/
│  ├─ SHELLeye.Protocol/
│  ├─ SHELLeye.World/
│  ├─ SHELLeye.Kernel/
│  ├─ SHELLeye.Platform.Windows/
│  └─ SHELLeye.PowerShell/        # only if separate provider wins experiment
├─ program-host/
│  ├─ sdk/
│  ├─ src/
│  └─ examples/
├─ tests/
│  ├─ SHELLeye.IntegrationTests/
│  └─ fixtures/
│     ├─ ProcessIdentity/
│     ├─ FileIdentity/
│     ├─ PortReuse/
│     └─ PersistentWorkload/
└─ experiments/
```

The exact assembly count may be smaller. Split only when the boundary earns itself.

## 5. Build 001 live topology

Preferred development topology on STEALTHEYELLC:

```text
Windows OS objects
  processes / named Job Objects / files / SCM / TCP tables / sessions
                           │
                           ▼
                 SHELLeye.Kernel
          C# / .NET 10 / native providers
                           │
                 named pipe: shelleye-dev
                           │
          ┌────────────────┴────────────────┐
          ▼                                 ▼
Node Program Host              PowerShell runspace provider
(disposable)                    (separate only if justified)
```

Suggested persistent development locations:

```text
X:\SHELLeye\repo
C:\SHELLeye\runtime\kernel
C:\SHELLeye\runtime\powershell       # only if separate provider
C:\SHELLeye\state\shelleye-dev.db
C:\SHELLeye\spool
C:\SHELLeye\Temp
```

The canonical repository clone can use `X:`; the deterministic physical-file fixture should primarily use `C:` NTFS because Build 001 must establish exact physical-file identity there. ReFS identity gets a targeted smoke case rather than driving the entire slice.

For initial persistent development launch, use an interactive-user scheduled task such as `shelleye-kernel-dev` under `STEALTHEYELLC\StealthEye`, matching the session in which the developer workload runs. Configure that Build 001 task to run with the highest privileges available to the owner account because the final NTFS continuity witness queries the volume USN journal identifier, which Windows documents as an administrator operation. This is an intrinsic OS access requirement, not a SHELLeye permission tier. This is a deployment choice for the first target, not a future privilege architecture. Multi-session/system-service execution is deferred until it buys a concrete capability.

## 6. Local protocol

Minimum protocol mechanics:

- version handshake;
- request ID / response correlation;
- notifications;
- cancellation/timeouts;
- streaming/delta notifications;
- provider epoch metadata;
- reconnect lifecycle;
- explicit typed errors;
- compact object references rather than embedding giant payloads.

No general remote-auth/SSH transport work in Build 001.

## 7. Operating-state schema direction

Do not freeze SQL column names before implementation, but the ontology requires approximately:

```text
machines
boot_epochs
concepts
sessions_current
processes_retained
jobs
job_members_current
commands_current
files_retained
file_paths_current
volumes
services_retained
listeners_retained
execution_contexts
provider_epochs
runtime_descriptors
output_spools
promoted_interests
world_state
delta_ring
```

Key principles:

- logical concept ID separate from native witness;
- process witness includes BootEpoch, PID, sequence number, creation time; BootEpoch prefers current `ProcessTelemetryIdInformation.BootId` when available and is corroborated/fallbacked by persisted Windows last-boot evidence;
- file witness includes volume identity + 128-bit file ID; exact C: NTFS post-gap continuity additionally stores USN journal ID + per-file last USN;
- named job native name persisted;
- listener binds to exact owner process incarnation + TCP bind/create timestamp where IP Helper exposes it;
- deltas are bounded and pruneable;
- completed short-lived commands are garbage-collectable;
- no permanent command/action ledger.

## 8. Windows process provider

Build 001 must implement:

- process enumeration using `SystemBasicProcessInformation` on this target;
- PID, image, sequence number, reported parent PID;
- exact process open/verification with creation time;
- optional target-specific handle-bound corroboration through `ProcessTelemetryIdInformation` when available;
- process state/exit code where accessible;
- session ID;
- executable path where accessible;
- lightweight CPU/time, memory, and I/O inspection;
- exact process wait;
- direct process termination through a verified handle;
- parent/child relation quality rather than blind parent-PID attachment;
- direct launch with explicit executable/argument list/cwd/environment.

Optional rich fields such as all modules/threads/handles are not required for A–D.

### Process actuation invariant

Every operation through `proc_*` must resolve the current exact incarnation before acting. Stored PID alone is never accepted as actuation authority.

Required retained-process mutation path:

```text
retained BootEpoch/PID/SequenceNumber/creation witness
→ OpenProcess(retained PID, required rights)
→ GetProcessTimes on that handle must match retained creation time
→ while the same handle is held, verify retained SequenceNumber:
     ProcessTelemetryIdInformation if supported, otherwise/further corroborated by
     a fresh SystemBasicProcessInformation row for that PID
→ mismatch/absence/exit/access failure => typed conservative result
→ mutate/wait using that same opened process handle
```

The same-handle rule closes the enumeration/open/mutation race: even if the original exits and Windows later reuses its PID, an already-open process handle remains bound to the original process object and cannot redirect to the replacement.

Hard assertion: `terminate(old_proc)` can never terminate a process that later reused the old PID.
## 9. Execution / Job Object provider

Build 001 persistent workload path:

1. create uniquely named Windows Job Object;
2. persist native job name + `job_*` concept before/around launch transaction;
3. do **not** enable `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`;
4. associate the Job Object completion port while the job is still inactive;
5. create/open restart-independent stdout/stderr spool-segment handles with append semantics;
6. build `STARTUPINFOEX` with `PROC_THREAD_ATTRIBUTE_JOB_LIST` so the fixture is assigned to the intended job as part of `CreateProcessW` on this Windows 11 target;
7. use `PROC_THREAD_ATTRIBUTE_HANDLE_LIST` so the child inherits only the required stdout/stderr/stdin handles;
8. call `CreateProcessW` with `EXTENDED_STARTUPINFO_PRESENT` and retain the returned exact process/thread handles;
9. retain the exact `proc_*` witness;
10. reconcile completion-port signals with job/process queries because most job notifications are not guaranteed.

`CREATE_SUSPENDED → AssignProcessToJobObject → ResumeThread` remains a compatibility fallback when creation-time `PROC_THREAD_ATTRIBUTE_JOB_LIST` is unavailable/incompatible. It is not the preferred Build 001 path on STEALTHEYELLC.

Do not force unrelated or incompatible external processes into SHELLeye jobs. Native nested-job/breakaway/assignment constraints are returned as OS facts.

Required operations:

```text
job.create
job.inspect
job.members
job.wait_empty
job.terminate
process.start(... job)
process.wait
process.terminate
job.output(afterCursor)
```

Exact public names can change; semantics cannot.
## 10. Restart-safe output

Persistent workload stdout/stderr must remain operational across kernel death.

Build 001 default is one restart-independent spool **segment per process/stream**, for example:

```text
C:\SHELLeye\spool\<job-id>\<proc-id>.stdout
C:\SHELLeye\spool\<job-id>\<proc-id>.stderr
```

Launch requirements:

- open the stream handles before `CreateProcessW` with append/write semantics appropriate to the fixture;
- mark only required stream handles inheritable;
- pass them through `STARTF_USESTDHANDLES` plus `PROC_THREAD_ATTRIBUTE_HANDLE_LIST`;
- the child owns its inherited write handle independently of kernel lifetime.

Kernel reads by segment/byte cursor and exposes bounded chunks through the logical job output cursor.

Acceptance requirements:

- kill kernel while workload continues;
- workload must not die due to a broken kernel-owned pipe;
- after kernel restart, old logical output cursor remains usable if its segments are still retained;
- new output produced during the gap is readable;
- model-facing output chunks are bounded;
- deterministic fixture output is bounded;
- completed segments are garbage-collected after the operational retention window.

Transparent live rotation of a spool file that a running child already holds open is **not** required in Build 001. A later stream sink can earn that complexity if sustained high-volume workloads require active rotation. Do not falsely claim path rename/rotation redirects an existing child file handle.

This is kernel-restart continuity, not a permanent log or machine-power-loss durability system.
## 11. File provider

Build 001 required:

- normalized path binding;
- volume identity;
- `FILE_ID_INFO` / 128-bit file ID;
- explicit recognition that file IDs can be reused over time after deletion;
- file and directory concepts;
- current existence/type/size/time metadata;
- content hash only when required for a revision/precondition;
- create/read/write/rename/move/delete for disposable fixture files;
- mutation precondition on current physical identity;
- **same-handle verify-and-mutate:** query physical identity/revision on the handle that will perform `WriteFile`, `SetFileInformationByHandle`, or another handle-based mutation;
- `OpenFileById` where it materially reduces namespace races and is supported;
- `ReadDirectoryChangesW` or equivalent watcher dirty signals;
- overflow/gap reconciliation;
- same-volume rename continuity;
- atomic replacement detection;
- delete/recreate detection;
- directory rename continuity.

### Narrow NTFS post-gap continuity witness

`FILE_ID_INFO` alone cannot prove post-gap continuity because Windows explicitly permits file-ID reuse over time. Milestone A therefore adds one narrow NTFS continuity token for its retained `C:` fixture file:

```text
volume identity
128-bit FileId
USN journal ID
last file/directory USN from FSCTL_READ_FILE_USN_DATA
```

Before kernel death, persist this token. After restart, continue the same `file_*` only if volume/FileId, journal ID, and last USN all still match. A reset/deleted journal, changed file USN, inaccessible query, or any mismatch means exact continuity is not proved; return stale/ambiguous/destroyed as evidence permits rather than rebinding.

This does **not** implement full USN-journal ingestion or event replay. Build 001 does not scan the volume journal or reconstruct missed file history. The `X:` ReFS test remains a current-identity smoke case while its journal is inactive.

Required primary API families:

- `GetFileInformationByHandleEx(FileIdInfo)`;
- `OpenFileById` where appropriate;
- `SetFileInformationByHandle` for handle-based rename/delete/metadata/end-of-file operations;
- `FSCTL_READ_FILE_USN_DATA` for the retained file token;
- `FSCTL_QUERY_USN_JOURNAL` for the C: journal identifier.
## 12. Volume provider

Build 001 minimum:

- enumerate `C:` and `X:` as `vol_*` concepts;
- filesystem type;
- volume identity/serial/GUID where available;
- current capacity/free space;
- drive-letter/mount binding.

The capability is useful for file identity and platform truth but should remain compact.

## 13. Service provider

Build 001 implements **query/inspection**, not broad service administration.

Required:

- find one or more services;
- inspect registered service identity/name, state, start type as practical;
- query current service PID where valid;
- resolve PID to exact current `proc_*`;
- emit `service.process_changed` / state delta for retained services when observed;
- wait/query structure ready for later `NotifyServiceStatusChange` use.

Acceptance uses an existing harmless service such as the running Task Scheduler/Event Log service for inspection. Build 001 does not create a test Windows service merely for breadth.

## 14. Network provider

Build 001 required:

- IPv4 and IPv6 TCP listener query via IP Helper;
- prefer `TCP_TABLE_OWNER_MODULE_LISTENER` so rows include owning PID plus `liCreateTimestamp` where available;
- exact owner `proc_*` resolution;
- transient `listener_*` concept promotion for watched fixture server using endpoint + exact process + bind/create timestamp + observation generation;
- targeted wait for listener open/close;
- port-reuse identity test.

The bind/create timestamp is an additional listener-incarnation witness, not a documented globally unique socket ID. Across an unobserved kernel gap, current endpoint state may be reconstructed but uninterrupted socket continuity is not asserted solely from matching endpoint/PID/timestamp values.

Connection-table breadth is optional. UDP is core-later unless trivial after TCP implementation.

No durable `port_*` concept.
## 15. Session provider

Build 001 minimum:

- current Windows session IDs;
- current interactive user/session relation;
- process → session relation;
- enough WTS data to explain whether a process is in the intended session.

Do not build full RDP/logon administration.

## 16. Structured PowerShell provider

Build 001 must prove at least one real object pipeline without formatting text.

Acceptance example:

```text
Get-Process -Id <fixturePid>
→ projected PSObject result
  ProcessName
  Id
  SessionId (or provider-equivalent property)
  typed/provider metadata
```

or another object-rich Windows query that demonstrates the same property.

Required:

- create/open runspace;
- invoke cmdlet/script pipeline;
- return object collection before formatting;
- preserve error/warning streams separately;
- provider epoch;
- raw PowerShell fallback.

If a separate PowerShell provider process is implemented, Milestone C kills/restarts it and confirms OS concepts survive while runspace-local transient state is honestly reset.

## 17. Delta and coherence engine

Build 001 implements:

- monotonic SHELLeye observation sequence;
- bounded delta ring;
- interest filters/promoted objects;
- subscriptions;
- cursor-based reads;
- explicit `cursor_expired` behavior;
- condition waits;
- `world.sync` scoped reconciliation barrier;
- provider-gap/recovery delta.

Required event families:

```text
process.started / exited / changed
job.member_added / member_exited / empty
file.created / changed / renamed / replaced / deleted
service.state_changed / process_changed
listener.opened / closed
provider.restarted
world.reconciled
```

Do not full-dump process/file/network tables to ChatGPT after every primitive.

## 18. Typed error model

Build 001 should normalize routine machine errors into compact classes while preserving native details:

```text
not_found
stale
destroyed
ambiguous
inaccessible
access_denied
busy
sharing_violation
timeout
process_exited
provider_unavailable
cursor_expired
unsupported
native_error
```

No giant human prose for expected machine conditions.

## 19. Deterministic fixture: PersistentWorkload

One tiny Node fixture supports all core workflows.

Required behavior:

- accepts `--config <path>`;
- config can choose response text and child-process behavior;
- starts loopback HTTP server on port `0` and reports chosen port in one machine-readable ready record;
- reports its PID and child PID;
- spawns one deterministic child process;
- emits known stdout and one known stderr record;
- handles simple shutdown;
- can be restarted using the same config/workspace;
- does not itself implement SHELLeye semantics.

The fixture should be small enough to understand completely.

## 20. Deterministic fixture family: ProcessIdentity

Required cases:

1. launch fixture and retain `proc_A`;
2. record PID/sequence/creation time;
3. launch a second same-executable process simultaneously;
4. confirm separate `proc_*` concepts despite same name/executable;
5. exit `proc_A`;
6. relaunch same executable → new `proc_B`;
7. old `proc_A` remains terminal/destroyed and never rebounds;
8. run deterministic resolver test simulating a current process table row with old PID but a different sequence number/creation time;
9. run deterministic race test where the original exits around OpenProcess/verification; prove the held native handle cannot redirect to a reused PID;
10. `terminate(proc_A)` returns destroyed/stale and does not call termination on the simulated/new target;
11. attempt real PID reuse opportunistically if practical, but deterministic simulation is the acceptance source so the suite is not probabilistic.

Hard assertion:

```text
wrong process mutation count == 0
false process rebound count == 0
```

## 21. Deterministic fixture family: FileIdentity

Required cases on NTFS:

1. create `alpha.txt` → `file_A`;
2. write and change revision → same physical concept;
3. rename to `beta.txt` → same `file_A`, new path binding;
4. rename containing directory → same file/dir physical concepts;
5. create hard link if convenient → same file identity, additional path;
6. atomic replace `beta.txt` with a different file object → old `file_A` destroyed/replaced, new `file_B`;
7. attempt `write(file_A)` → must not write `file_B`;
8. delete file and recreate identical path/content → new `file_C`;
9. inject/simulate a reused file-ID candidate with a changed journal/file-USN continuity token and prove the old `file_*` does not rebound;
10. old handle remains destroyed;
11. targeted ReFS file identity smoke test on `X:` without claiming NTFS-equivalent gap continuity while X: has no active journal.

Hard assertion:

```text
wrong file mutation count == 0
false file rebound count == 0
```

## 22. Deterministic fixture family: PortReuse

Required cases:

1. server A binds loopback ephemeral port P;
2. retain `listener_A` bound to exact `proc_A` and its IP Helper bind/create timestamp;
3. stop A and wait for listener closed;
4. start server B so it binds P (explicitly or by controlled fixture retry);
5. resolve owner to exact `proc_B` and observe the new listener bind/create timestamp;
6. allocate `listener_B`;
7. old `listener_A` remains closed/destroyed;
8. no operation/query through old handle silently returns B as the same listener.

Hard assertion:

```text
false listener rebound count == 0
```

## 23. Milestone A acceptance gate — Persistent Machine World

### Setup

1. Start kernel in the intended interactive-user session, with the Build 001 scheduled task using the owner account's highest available privileges for the NTFS journal-ID continuity query.
2. Create disposable C: NTFS workspace and config `file_*`; retain volume/FileId plus C: journal ID + last file USN continuity token.
3. Create named `job_*`, associate completion port while inactive, and prepare explicit inherited spool handles.
4. Start fixture through creation-time Job Object assignment (`PROC_THREAD_ATTRIBUTE_JOB_LIST`) with explicit inherited spool handles; use suspended/assign/resume only if the documented fallback path is required.
5. Retain `job_*`, root/child `proc_*`, config `file_*`, and current listener observation.
6. Record current world cursor and output cursor.

### Kernel kill

7. Hard-kill only `SHELLeye.Kernel`.
8. Confirm fixture parent/child continue running.
9. Confirm HTTP endpoint still responds independently of the kernel.
10. Cause fixture to emit additional output during the kernel gap.

### Recovery

11. Restart kernel.
12. Detect the same BootEpoch using the current Windows BootId when available plus persisted last-boot evidence; any uncertainty advances the logical epoch rather than rebinding transient objects.
13. Reopen named Job Object.
14. Recover **same logical `job_*`**.
15. Enumerate process table and recover **same `proc_*`** for still-live parent/child using sequence number + creation-time witnesses.
16. Recover **same physical `file_*`** only if volume + 128-bit file ID + persisted C: journal ID + last-file-USN continuity token all still match; otherwise report conservative loss of exact continuity.
17. Resume job output from prior cursor and receive output produced during the gap.
18. Reconcile service/session/listener current state.
19. Emit recovery delta including any observation-gap uncertainty.

### Important listener rule

The listener may be rediscovered as current after the gap, but Build 001 must not fabricate proof that one native socket continuously survived the unobserved interval. The machine endpoint is usable; logical listener continuity is conservative.

### Provider recovery

If Build 001 uses a separate PowerShell provider:

20. kill provider while kernel/fixture remain alive;
21. restart provider;
22. confirm SHELLeye OS concepts are unaffected;
23. confirm provider-local runspace handles are re-created or reported lost honestly.

**Milestone A passes only if kernel lifetime is not workload lifetime and retained exact OS objects are recovered without false continuity.**

## 24. Milestone B acceptance gate — Objects / Delta First

On the live fixture plus existing Windows state:

1. `machine.inspect` returns compact machine + BootEpoch + session + volume summary;
2. inspect fixture `job_*` and root `proc_*`;
3. retrieve exact process tree/job members;
4. inspect config `file_*` and directory;
5. inspect current fixture `listener_*` and exact owner;
6. inspect one real existing `svc_*` and its current process relation where valid;
7. establish a world cursor;
8. change only the config file;
9. wait on actual file condition;
10. consume delta showing the targeted file revision/change rather than a directory dump;
11. start/exit a short client process;
12. wait on exact process exit;
13. consume compact process/job/network deltas;
14. query output only after the previous output cursor;
15. prove routine observations avoid repeated full `Get-Process`, directory, and TCP table payloads.

**Milestone B passes when normal perception is compact retained concepts + relationships + deltas + waits.**

## 25. Milestone C acceptance gate — Identity Killer

Run deterministic hostile cases across fresh and recovered kernels:

### Process

1. process exit;
2. same executable relaunch;
3. duplicate same-executable instances;
4. deterministic PID-reuse resolver simulation;
5. best-effort real PID reuse stress if practical;
6. old-process terminate attempt;
7. reported parent PID whose original parent no longer exists / could be reused;
8. process exits after OpenProcess/verification but before mutation; the held handle must still prevent redirection to a reused PID.

Expected:

```text
same executable relaunch → new proc_*
duplicate executable      → distinct proc_* concepts
PID reuse witness mismatch → old proc_* destroyed/stale
terminate(old proc_*)       → never touches replacement
uncertain parent PID        → reported/unknown relation, not false exact edge
```

### File/directory

9. file content edit;
10. file rename;
11. directory rename;
12. hard-link case where practical;
13. atomic replacement;
14. delete/recreate same path;
15. recreate same content;
16. old-file write attempt;
17. deterministic post-gap file-ID reuse simulation: same volume/FileId candidate but changed journal ID or file USN must not recover the old `file_*`.

Expected:

```text
content edit       → same physical file; revision changes
same-volume rename → same file_*; path changes
atomic replace     → old file_* destroyed/replaced; new file_*
delete/recreate    → new file_*
write(old file_*)  → never mutates replacement
same FileId + changed continuity token → old file_* not recovered
```

### Listener

18. server A listener opens;
19. server A exits;
20. same port reused by server B;
21. old listener resolution attempt.

Expected:

```text
listener_A closed
listener_B new concept
no rebound by port number
```

### Recovery

22. kernel death with live workload;
23. kernel recovery;
24. separate provider death/recovery if such provider exists;
25. boot-epoch transition simulation in resolver tests; old transient process/listener concepts must never resolve into the new epoch.

### Hard metrics

```text
false rebounds = 0
wrong-object mutations = 0
```

Ambiguous/stale/destroyed/unknown outcomes are acceptable when exact continuity cannot be proven.

**Milestone C is the decisive Build 001 gate.**

## 26. Milestone D acceptance gate — Programmable Machine Operation

One Node Program Host program, one model invocation, no model calls between primitives.

Canonical acceptance flow (minimum 30 meaningful SDK operations):

1. inspect machine + BootEpoch;
2. inspect interactive session;
3. inspect `C:` volume;
4. create disposable directory;
5. create config file;
6. retain/inspect config physical identity;
7. establish world cursor;
8. create named job;
9. start fixture through creation-time Job Object assignment (`PROC_THREAD_ATTRIBUTE_JOB_LIST`) with explicit inherited spool handles; use suspended/assign/resume only if the documented fallback path is required;
10. retain exact root process;
11. wait for ready output cursor;
12. inspect job members;
13. resolve child process;
14. wait for loopback listener;
15. retain/inspect listener owner;
16. launch direct client process;
17. wait for exact client exit;
18. collect client result without terminal formatting;
19. invoke structured PowerShell object query against fixture process;
20. inspect lightweight process CPU/memory/I/O state;
21. read job output after previous cursor;
22. modify config with expected file identity/revision;
23. wait for file change;
24. consume compact world delta;
25. rename config and prove same physical `file_*`;
26. terminate old root process through exact `proc_*`;
27. wait exact exit and listener close;
28. start replacement fixture process in the same logical `job_*`;
29. prove replacement has a new `proc_*`;
30. wait for replacement listener;
31. prove replacement listener is a new concept owned by new process;
32. inspect one existing Windows service and current service/process relation;
33. move/rename config again and inspect final directory state;
34. read final output delta;
35. terminate entire job;
36. wait job empty;
37. wait listener absent;
38. consume final world delta;
39. delete disposable workspace;
40. return one compact structured acceptance object.

The Program Host may perform additional local loops/retries required to wait for deterministic conditions. Those loops are not model turns.

**Milestone D passes only if the result demonstrates real typed SHELLeye operations across multiple machine domains, not a single giant shell script hidden behind one tool call.**

## 27. Build 001 post-gate benchmark

After A–D pass, compare a conventional agent-shell baseline against SHELLeye on the same acceptance workload.

### Baseline

Use ordinary PowerShell/cmd-style interaction:

- `Get-Process` / process text/object re-query;
- directory listings/path rediscovery;
- `Get-NetTCPConnection`/`netstat`-style listener lookup;
- polling;
- stdout/stderr parsing;
- repeated PID/path/port discovery.

### SHELLeye

Use:

- retained `proc_*`/`job_*`/`file_*`/`listener_*`/`svc_*`;
- typed state;
- exact waits;
- bounded deltas;
- Program Host.

### Metrics

Record:

```text
model turns
operations per model turn
model observation bytes/tokens
raw shell-output bytes
process rediscovery count
file rediscovery count
listener rediscovery count
condition polling count
end-to-end latency
kernel recovery latency
wrong-target hostile-case result
```

Do not invent a marketing speedup target before measurement.

## 28. Build 001 no-go list

Exclude unless a proven blocker requires one:

- full Linux support;
- full macOS support;
- SSH fleets;
- cloud providers;
- Kubernetes;
- Docker orchestration;
- GUI automation;
- browser semantics;
- source/compiler semantics;
- document/data semantics;
- remote desktop;
- full ETW ingestion;
- broad USN journal ingestion/replay (the narrow C: journal-ID + per-file last-USN continuity token is Build 001 core);
- generic handle graph;
- full Registry ontology;
- full Task Scheduler management;
- terminal UI;
- full ConPTY terminal provider;
- WSL machine worlds;
- persistent historical telemetry warehouse;
- SIEM/log analytics;
- multi-agent scheduler;
- general security product;
- kernel driver;
- Windows kernel modification.

## 29. Implementation order

Recommended order:

1. protocol + operating-state DB + concept IDs + BootEpoch, using ProcessTelemetry BootId when available with last-boot fallback/corroboration;
2. process enumeration/sequence witness + exact open/verify/wait/terminate;
3. direct process creation + named Job Object + restart-safe spool;
4. file/volume physical identity + guarded mutation;
5. kernel recovery for job/process/file/output;
6. bounded delta/world-sync/wait substrate;
7. listener query/owner resolution/waits;
8. service/session compact providers;
9. structured PowerShell provider experiment + frozen implementation;
10. deterministic hostile fixtures;
11. Node Program Host SDK;
12. A → B → C → D gates;
13. benchmark and measured results document.

Do not postpone Milestone C hostile identity tests until after feature breadth.
