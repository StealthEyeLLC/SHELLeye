# 00 — Project Charter

Status: **Canonical**  
Project: **SHELLeye**  
Owner: **StealthEyeLLC**  
Primary operator: **ChatGPT**  
Initial target platform: **STEALTHEYELLC / Windows 11 x64**  
Canonical baseline: **2026-08-08**

## Mission

SHELLeye exists to build the strongest technically defensible **AI-native persistent machine operating substrate** that the target machine can support.

The product is designed entirely for ChatGPT as the operator. It is not a human shell, terminal replacement, PowerShell frontend, SSH client, conventional orchestration dashboard, or DevOps product optimized around a human reading formatted command output.

SHELLeye should turn operating-system reality into durable agent-addressable concepts and relationships so ChatGPT can reason in terms such as:

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

rather than routinely executing a command, parsing text, rediscovering the same PID/path/port, polling, parsing more text, and repeating.

Success means ChatGPT can continue operating without repeatedly asking:

- what PID is that process now?
- is that service still running?
- which exact process owns that listener?
- did that file move or get replaced?
- is that long-running job done?
- did its descendants survive?
- what changed since the previous observation?

## Exhaustive project constraints

These five constraints are exhaustive at the project level. Contributors must not silently add new project-level constraints that contradict or expand them.

### 1. No extra safety

Do not add SHELLeye-specific permission systems, approval systems, authority tiers, confirmation gates, artificial capability restrictions, or protective abstractions.

Underlying operating-system access rules remain real machine facts. `access_denied`, protected-process behavior, sharing violations, session mismatch, elevation requirements, and unavailable resources should be represented accurately, not wrapped in a SHELLeye policy layer.

### 2. No extra guardrails

Do not add policy engines, allowlists, denylists, action classifications, privilege tiers, restricted modes, capability gates, or similar guardrail systems.

### 3. No theater

Do not add components mainly to look enterprise-grade, cautious, auditable, sophisticated, controlled, or complete. Every component must earn its existence through capability, correctness, continuity, reliability, performance, interoperability, or agent usability.

### 4. No separate verification architecture

Do not add verification agents, mandatory verifier stages, proof pipelines, mandatory double-check stages, or verification orchestration.

Ordinary systems correctness mechanisms remain required where useful: exact object-resolution checks, process handles, file-version checks, exit status, service state, socket state, native notifications, tests, assertions, stale-object detection, race handling, and transactional preconditions.

### 5. No receipts / permanent action ledger

Do not build permanent execution receipts, a general action ledger, a provenance archive, an audit-trail product, or a proof-of-execution history.

Operational state intrinsically required by SHELLeye is allowed and necessary, including:

- current machine / boot identity;
- current logical concepts and native witnesses;
- current process/job/service/task/file/listener bindings;
- bounded event/delta replay;
- current stdout/stderr buffers or bounded operational spools;
- current command/job state;
- current runtime/provider descriptors;
- active operation/transaction state required for correctness.

Do not turn those mechanisms into permanent historical bookkeeping.

## First-principles design rules

### ChatGPT is the product user

Every representation, persistence model, API, execution topology, and payload should be evaluated for ChatGPT's ability to operate the machine.

The default representation is not a terminal transcript. It is compact state, identity, relationships, changes, operations, and conditions. Human-readable CLI output may exist for engineering/debugging but is not the product ontology.

### Native machine truth versus SHELLeye continuity

Windows, filesystems, SCM, Task Scheduler, TCP/IP, user sessions, PowerShell providers, and other native subsystems remain authoritative for their own current state.

SHELLeye owns agent continuity:

- logical concept IDs distinct from PIDs, paths, port numbers, and provider object instances;
- exact native identity witnesses;
- current relationships between machine objects;
- conservative resolution before actuation;
- bounded deltas and cursors;
- event-driven waits where technically available;
- coherence/reconciliation;
- recovery after SHELLeye/provider restart;
- compact ChatGPT-facing projections;
- local deterministic program execution.

SHELLeye must not become a duplicated operating system database that claims stronger truth than the OS.

### Persistent machine principle

A model/tool call ending is not a reason for a real workload to end.

Long-running workloads, services, scheduled tasks, files, and other native machine objects should outlive transient ChatGPT calls. The SHELLeye kernel is independently restartable and is not the accidental process owner for everything it observes.

For SHELLeye-created persistent grouped workloads, Build 001 uses named Windows Job Objects where compatible and restart-safe output spools. The job deliberately does not depend on the kernel retaining the last object handle.

### Identity before convenience

Logical concept identity is never a raw OS identifier.

Hard rules:

- PID is not process identity.
- path is not file identity.
- port number is not listener identity.
- command string is not process identity.
- executable image/name is not process identity.
- service is not process.
- registered scheduled task is not a task run.

An old retained object must never mutate a different machine object merely because Windows reused an identifier or location.

### Conservative continuity

False continuation is worse than honest loss of continuity.

When exact identity cannot be established, SHELLeye returns a typed stale/destroyed/ambiguous/unresolved result rather than manufacturing a rebound.

A `proc_*` is one native process lifetime. Process restart creates a new process concept. Long-lived logical continuity belongs to higher-level concepts such as `job_*`, `svc_*`, and registered `task_*`.

### Event/delta first, reconciliation authoritative

Normal operation is:

```text
observe compact state
→ retain concepts
→ act on retained concepts
→ wait on real conditions
→ receive bounded semantic delta
→ continue
```

Event streams are change evidence, not eternal truth. Native notifications, Job Object completion messages, service notifications, filesystem watcher signals, ETW/Event Log evidence, or polling can be incomplete, delayed, unsupported, or lossy.

Canonical rule:

> **Events tell SHELLeye where reality may have changed; authoritative current queries tell SHELLeye what reality is now.**

### Multiple lifetimes, not one fake version counter

Machine state contains different temporal domains. The architecture may maintain independent notions such as:

- `BootEpoch`;
- SHELLeye observation/world sequence;
- process lifetime;
- job lifetime;
- service registration/config revision;
- registered task revision and task-run lifetime;
- physical file lifetime and file-content/metadata revision;
- volume lifetime;
- session/logon lifetime;
- listener observation lifetime;
- terminal/runspace lifetime.

A SHELLeye world sequence orders SHELLeye observations. It must not claim a perfect causal ordering across independent Windows subsystems.

### Interest-driven concept promotion

First-class addressability does not require permanent IDs for every thread, handle, TCP connection, event record, output line, environment variable, or resource sample.

High-cardinality objects remain query records unless ChatGPT retains, watches, relates, or acts on them. Promoted concepts receive durable IDs only when persistence materially helps operation.

### Structured actuation before shell strings

Execution should use the representation that best fits the operation:

1. direct executable launch with an explicit executable, argument list, cwd, environment, and session context;
2. structured PowerShell runspace invocation when PowerShell's object model provides useful breadth;
3. native Windows APIs for native domain operations;
4. ConPTY terminal execution for genuinely interactive/terminal-dependent programs;
5. raw PowerShell/cmd/WSL/shell strings as escape hatches.

This hierarchy is a correctness/information-density choice, not a capability restriction.

### Wait on machine conditions, not arbitrary sleeps

Core agent ergonomics should include waits for real conditions: exact process exit, job empty/completed, file state/change, service state, listener presence/absence, output cursor/match, and other provider-specific predicates.

Polling remains valid where Windows offers no stronger event primitive, but polling should be targeted to the retained condition rather than repeatedly dumping whole machine tables.

### Raw/native escape hatches remain possible

High-level semantics must not become veto layers. SHELLeye preserves access to direct process execution, raw PowerShell, raw cmd, WSL execution when the correct user/session context is available, filesystem paths, Registry, WMI/CIM, ETW/Event Log queries, and provider-native requests where useful.

### Headless operation

SHELLeye must operate with no human terminal, IDE, desktop application, or dashboard open. A terminal is a provider when the target program needs terminal semantics, not a requirement for the machine substrate itself.

### Programmability before model round trips

When deterministic local logic can execute many machine operations, run it in the local Program Host rather than requiring a model turn between every primitive.

The Program Host is a computational plane, not another agent. It owns no canonical machine state.

## Cross-substrate ownership

SHELLeye owns generic local-machine semantics: processes, execution groups/jobs, command invocation, files/directories/volumes, services, registered tasks/runs, generic sessions, generic listeners/connections, environment/execution context, resources, native event state, and generic execution.

It does not take over sibling ontologies:

- **CODEeye** owns source/compiler/build/test/debugger engineering meaning.
- **eyeBROWSE** owns browser targets/documents/DOM/AX/browser-network semantics.
- **DESKTOPeye** owns native UI/window/control/focus semantics.
- **DOCSeye / DATAeye** own deep document/data semantics.

SHELLeye may know that a process hosts a browser, that a file contains source, or that a process owns a native window. Cross-substrate links are correlations between concepts, not ownership transfer.

## Build 001 policy

Build 001 must establish the permanent spine rather than broad feature coverage.

It proves four properties before expansion:

1. a persistent machine world independent of kernel lifetime;
2. durable machine objects, compact observations, deltas, and real waits;
3. hostile identity/recovery behavior with zero false rebounds and zero wrong-object mutations;
4. one substantial local Program Host workflow executing 30+ meaningful machine operations without model round trips.

Build 001 is defined by `02-BUILD-001-SLICE.md`.

## Canonical-document discipline

The numbered documents in `docs/` form one specification. `AUTHORITY.md` records repository/project authority.

When implementation evidence invalidates a decision:

1. update the relevant canonical document;
2. update `06-DECISIONS.md`;
3. do not leave a contradictory second architecture beside it.

Experiments may live under `experiments/`, but an experiment does not become canonical merely because it succeeds. Promotion into the canonical specification must be explicit.
