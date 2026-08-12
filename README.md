# SHELLeye

Status: **BUILD 002 IMPLEMENTED / MEASURED / COMPLETE — Provider Neutrality Pressure Test**
Owner: **StealthEyeLLC**  
Primary operator: **ChatGPT**  
Initial target: **STEALTHEYELLC / Windows 11 x64**  
Canonical baseline: **2026-08-12**

SHELLeye is a **persistent, programmable machine world designed for ChatGPT as the operator**. It is not a human shell, terminal replacement, PowerShell GUI, SSH client, conventional orchestration dashboard, or DevOps product whose main interface is intended for manual reading.

The goal is to let ChatGPT retain and operate on machine concepts such as:

```text
machine_1
session_1
proc_42
job_18
svc_7
file_91
dir_12
vol_2
listener_9
cmd_73
```

instead of repeatedly rediscovering PIDs, paths, service state, ports, and command completion through formatted shell text.

The normal operating loop is:

```text
observe compact machine state
→ retain machine concepts
→ reason
→ act on retained concepts
→ wait on actual machine conditions
→ receive semantic deltas
→ continue
```

## Architectural headline

SHELLeye keeps Windows itself authoritative for machine reality while adding durable agent-facing identity, recovery, structured actuation, compact deltas, waits, and local programmability.

Build 001 is Windows-first:

- **Kernel:** C# / .NET 10, restartable, headless.
- **Operating state:** SQLite WAL, bounded to current/recoverable machine state rather than an action ledger.
- **Process identity:** a `proc_*` denotes exactly one native process lifetime. On the target Windows build the primary witness is `BootEpoch + PID + SystemBasicProcessInformation.SequenceNumber`, strengthened by creation time and same-handle verification before actuation; handle-bound `ProcessTelemetryIdInformation` is an optional Windows corroborator. A restarted process is a new `proc_*`.
- **Workload continuity:** `job_*` carries restartable multi-process workload identity. SHELLeye-created persistent jobs use named Windows Job Objects when technically compatible, prefer creation-time membership through `PROC_THREAD_ATTRIBUTE_JOB_LIST`, explicitly whitelist inherited stream handles, and deliberately do not use kill-on-last-handle semantics.
- **File identity:** `file_*` / `dir_*` denote physical filesystem objects, not paths. Windows `FILE_ID_INFO` plus volume identity is the primary current witness; because Windows may reuse file IDs after deletion, Build 001 adds a narrow C: NTFS journal-ID + per-file last-USN token only for exact post-kernel-gap recovery. Mutations verify and act through the same opened file handle wherever Windows exposes handle-based semantics.
- **Network:** `listener_*` is transient and bound to protocol/local endpoint plus an exact owning process incarnation, with the IP Helper TCP bind/create timestamp as an additional witness where available. A port number is a value, not a durable object identity.
- **PowerShell:** a structured provider based on hosted runspaces / `PSObject` results, not formatted terminal output and not canonical Windows truth.
- **Terminal:** ConPTY remains a compatibility provider, not the core ontology.
- **Events:** targeted native notifications and reconciliation. Events say what may have changed; authoritative current queries say what is true now.
- **Program Host:** disposable Node 24 process running a rich typed SDK; one model call can execute tens of deterministic machine operations locally.
- **ChatGPT surface:** intentionally small; the rich operation set belongs in the local SDK rather than hundreds of top-level tool schemas.

## Build 001 — Machine World Kernel Slice

Build 001 is the first completed product implementation. Its four canonical gates passed on STEALTHEYELLC; measured evidence is recorded in [`docs/09-BUILD-001-RESULTS.md`](docs/09-BUILD-001-RESULTS.md).

It has four hard gates:

1. **Milestone A — Persistent Machine World**  
   A long-running grouped workload, output, retained process/job/file concepts, and current machine state survive a hard SHELLeye kernel restart and are conservatively recovered.

2. **Milestone B — Persistent Machine Objects / Delta First**  
   ChatGPT operates on compact process/job/file/service/listener concepts, relationships, condition waits, and bounded semantic deltas rather than repeated full process/file/network dumps.

3. **Milestone C — Recovery Continuity / Identity Killer**  
   Deterministic hostile tests cover process exit/relaunch/PID reuse, duplicate executables, file rename/replacement/delete-recreate, directory rename, port reuse, kernel death, and relevant provider death. Hard metrics: **false rebounds = 0; wrong-object mutations = 0**.

4. **Milestone D — Programmable Machine Operation**  
   One Node Program Host invocation performs at least 30 meaningful machine operations across process, job, file, listener, service, resource, output, PowerShell, wait, and delta surfaces with no model round trip between primitives.

The exact executable slice and acceptance workflow are canonicalized in [`docs/02-BUILD-001-SLICE.md`](docs/02-BUILD-001-SLICE.md).
## Build 002 — Provider Neutrality Pressure Test

Build 002 is **complete / measured / passed**. It proved that the SHELLeye spine can remain exact across materially different Windows and Linux/WSL2 providers without flattening provider-native identity semantics or weakening the measured Build 001 Windows provider.

The final classification is **PASS — provider-neutral spine survived with Windows depth preserved**. Canonical measured evidence is recorded in [`docs/14-BUILD-002-RESULTS.md`](docs/14-BUILD-002-RESULTS.md); the operative final measured freeze is [`docs/13-BUILD-002-ACCEPTANCE-FREEZE.md`](docs/13-BUILD-002-ACCEPTANCE-FREEZE.md).

Build 003 is **not authorized by Build 002 completion**. Subsequent sequencing remains separately evidence-driven.

## Canonical documents

Read in order:

1. [`docs/00-CHARTER.md`](docs/00-CHARTER.md)
2. [`docs/01-ARCHITECTURE.md`](docs/01-ARCHITECTURE.md)
3. [`docs/02-BUILD-001-SLICE.md`](docs/02-BUILD-001-SLICE.md)
4. [`docs/03-PLATFORM-STEALTHEYELLC.md`](docs/03-PLATFORM-STEALTHEYELLC.md)
5. [`docs/04-ROADMAP.md`](docs/04-ROADMAP.md)
6. [`docs/05-RESEARCH-BASELINE.md`](docs/05-RESEARCH-BASELINE.md)
7. [`docs/06-DECISIONS.md`](docs/06-DECISIONS.md)
8. [`docs/07-CAPABILITY-MATRIX.md`](docs/07-CAPABILITY-MATRIX.md)
9. [`docs/08-WORKFLOW-PRESSURE-TESTS.md`](docs/08-WORKFLOW-PRESSURE-TESTS.md)
10. [`docs/AUTHORITY.md`](docs/AUTHORITY.md)

Measured results are canonicalized in `docs/09-BUILD-001-RESULTS.md` for Build 001 and `docs/14-BUILD-002-RESULTS.md` for Build 002. Build 003 remains unimplemented and unauthorized.

## Planned implementation shape

No implementation projects are created by this setup pass. The expected first shape is intentionally compact:

```text
SHELLeye/
├─ README.md
├─ docs/
├─ src/
│  ├─ SHELLeye.Protocol/
│  ├─ SHELLeye.World/
│  ├─ SHELLeye.Kernel/
│  ├─ SHELLeye.Platform.Windows/
│  └─ SHELLeye.PowerShell/
├─ program-host/
├─ tests/
│  └─ fixtures/
└─ experiments/
```

Assembly/process boundaries are not a goal. Split components only when lifecycle, native integration, state ownership, or measured engineering pressure justifies the boundary.

## Immediate vertical slice

The smallest decisive Build 001 workload is a tiny deterministic local HTTP fixture started under a named Windows Job Object using creation-time job assignment where supported, with explicit restart-safe stdout/stderr spool segments, exact process identity, a physically identified C: NTFS config file with a narrow post-gap continuity token, a child process, and a TCP listener. The acceptance suite restarts the worker, renames/replaces files, reuses an endpoint, kills/restarts the SHELLeye kernel, invokes structured PowerShell, and executes a 30+ operation Program Host flow.

The decisive invariant is simple:

> **When identity is uncertain, SHELLeye refuses to manufacture continuity. An old process/file/listener handle must never act on a different machine object merely because a PID, path, or port was reused.**
