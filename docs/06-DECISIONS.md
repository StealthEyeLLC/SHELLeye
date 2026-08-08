# 06 — Decisions

Status: **Canonical**  
Baseline: **2026-08-08**

This file distinguishes decisions that are strong enough to build against from implementation details that should remain open until Build 001 evidence exists.

## Frozen decisions

### D-001 — ChatGPT is the only product operator

SHELLeye is optimized for machine identity, compact state, relationships, changes, operations, conditions, and local programmability. Human-readable terminal/UI surfaces are engineering conveniences, not the product architecture.

### D-002 — Windows owns machine truth; SHELLeye owns continuity

Native operating-system/provider state remains authoritative. SHELLeye adds logical IDs, exact witnesses, conservative resolution, deltas, waits, recovery, and compact projections.

### D-003 — Windows-first, not lowest-common-denominator POSIX

Build 001 uses strong Windows-native primitives such as process sequence numbers, Job Objects, `FILE_ID_INFO`, SCM, IP Helper, WTS, PowerShell runspaces, and named pipes. Portability is pressure-tested later with a deep second provider.

### D-004 — C# / .NET 10 is the Build 001 kernel

Node 24 is the Program Host. Native helpers are allowed later only when a concrete Windows capability earns them.

### D-005 — SQLite WAL is the initial operating-state store

Persist only current/recoverable operating state, promoted concept bindings, bounded deltas, runtime descriptors, and output-spool metadata. Do not create a permanent action/history ledger.

### D-006 — Logical concept IDs are never raw OS identifiers

PID, path, drive letter, port number, service PID, provider object instance, and command string are facts/bindings, not SHELLeye logical identity.

### D-007 — A `proc_*` is exactly one native process lifetime

Processes do not rebound across restart. Same executable, same command line, same service, or same PID later does not imply same `proc_*`.

On the target build, the preferred process witness is:

```text
BootEpoch + PID + SystemBasicProcessInformation.SequenceNumber + creation time
```

Every process mutation re-resolves and verifies the exact current incarnation before acting.

### D-008 — Long-lived restart continuity belongs above the process

Use `job_*`, `svc_*`, registered `task_*`, or later workload concepts for logical continuity across native process replacement.

### D-009 — Named Windows Job Objects are the preferred SHELLeye-created grouped-workload facet

When compatible:

- launch initial process suspended;
- assign to job before resume;
- do not set `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` for persistent jobs;
- persist native job name;
- reopen after kernel recovery while members remain live;
- use completion-port messages as signals, not sole truth.

Do not force incompatible/external workloads into jobs.

### D-010 — Persistent job output is restart-independent

Persistent jobs default to bounded operational stdout/stderr spools or an equivalent sink independent of kernel lifetime. Short direct executions may use ordinary pipes.

### D-011 — Generic SHELLeye `file_*` / `dir_*` identity is physical

The primary Windows witness is volume identity + `FILE_ID_INFO` 128-bit file ID.

- rename/hard-link path change can preserve the same physical concept;
- atomic replacement creates a new physical concept;
- delete/recreate creates a new physical concept;
- path is a binding, not identity.

### D-012 — File mutation is identity guarded

A retained file/directory operation opens/resolves the current object and verifies physical identity/revision before mutation. An old `file_*` never silently writes a replacement occupying the same path.

### D-013 — A durable `port_*` concept is rejected

Port numbers are values. `listener_*` is a transient concept tied to protocol/endpoint plus an exact owning process incarnation and an observation generation.

Port close/reuse creates a new listener concept.

### D-014 — Services and tasks are not processes

`svc_*` remains the logical SCM service while its `current_process` changes. Registered scheduled tasks are separate from task-run instances; Scheduler instance GUID is a useful run witness.

### D-015 — Process parentage carries evidence quality

A reported parent PID is not enough to create an exact process-tree edge after the original parent may have exited/reused. Parent relationships can be exact/resolved-current/reported/unknown.

### D-016 — Structured PowerShell is a provider, not a shell transcript

PowerShell invocation should use hosted runspaces and return `PSObject`/typed structure before formatting. Native Windows APIs remain canonical where they offer stronger identity/state.

Raw PowerShell remains an escape hatch.

### D-017 — Direct executable + argv is the preferred generic execution form

Do not require shell strings for normal process launch. Explicit executable, args, cwd, environment, and session context reduce quoting ambiguity and preserve execution intent.

### D-018 — Terminal/ConPTY is compatibility, not core ontology

Terminal sessions exist for REPLs/interactive/TUI/terminal-dependent programs. They do not define the normal ChatGPT machine interface.

### D-019 — Events are dirty signals; current queries are truth

Job notifications, file watchers, service notifications, ETW/Event Log, and later event providers accelerate synchronization. Gap/overflow/provider restart triggers reconciliation.

### D-020 — USN is deferred from Build 001

The architecture keeps a USN provider seam, but Build 001 correctness must not depend on it. Current target evidence shows an active C: NTFS journal and no active X: ReFS journal.

### D-021 — Broad ETW is deferred from Build 001

Selective ETW may later reduce polling or improve external process/network lifecycle visibility. Build 001 does not become an ETW telemetry collector.

### D-022 — Most Windows providers start in-process with the kernel

Process/file/network/service/session providers query state already owned by Windows. Separate provider processes are not added mechanically.

Stateful/special providers such as PowerShell may be separate when independent lifecycle or engine isolation earns the boundary.

### D-023 — Multiple machine lifetimes remain explicit

At minimum distinguish stable machine identity, BootEpoch, SHELLeye observation sequence, process/job/file/service/task/session/listener/provider lifetimes/revisions as needed. Do not force one global version to imply causal order it does not possess.

### D-024 — Delta-first / interest-driven observation

Normal operation tracks promoted/retained interests and bounded changes rather than repeatedly returning full machine enumerations. High-cardinality objects stay query records until retained.

### D-025 — `world.sync` is a coherence barrier, not verification architecture

It reconciles requested dirty/current views against authoritative providers. It does not promise machine quiescence or global causality.

### D-026 — Waits target real machine conditions

Exact process exit, job empty, service state, file condition, output cursor/match, and listener presence/absence are core semantics. Targeted polling remains valid where Windows offers no stronger event primitive.

### D-027 — Program Host is first-class and deterministic

One disposable Node 24 program can execute tens/hundreds of typed SHELLeye SDK calls locally. It owns no canonical state and contains no second model/agent.

### D-028 — Small ChatGPT-facing tool surface

The rich SDK stays local. The model should see a small set of generic top-level operations rather than hundreds of primitive schemas. Exact transport/MCP names remain replaceable.

### D-029 — Cross-substrate links are sparse correlations, not a universal ontology

SHELLeye owns generic process/file/machine meaning; CODEeye, eyeBROWSE, DESKTOPeye, DOCSeye, and DATAeye retain their own domain semantics.

### D-030 — Wrong-object operation count is the primary identity metric

For process/file/listener hostile fixtures:

```text
wrong-object operations = 0
false rebounds = 0
```

Loss of continuity is acceptable when exact identity cannot be proven.

## Deferred decisions

### DD-001 — Exact PowerShell hosting engine

Build 001 experiment chooses between available Windows PowerShell 5.1 hosting and a packaged modern `Microsoft.PowerShell.SDK` provider.

### DD-002 — Separate PowerShell provider process

Preferred if engine/version isolation and persistent runspace lifecycle materially simplify the system; otherwise in-kernel hosting is allowed.

### DD-003 — Exact SQLite schema/index layout

Ontology is frozen; physical schema waits for implementation measurements.

### DD-004 — Exact RPC framing/serialization

Start with simple framed JSON/NDJSON over Windows named pipes. Change only if profiling shows a real bottleneck.

### DD-005 — Long-term kernel service/session topology

Build 001 runs in the interactive `StealthEye` session. A later LocalSystem service plus explicit per-user/session launch brokers may be stronger for multi-session observation, but the live WSL LocalSystem limitation proves that user-context execution requires deliberate design. Do not freeze the distributed/session topology before a real need.

### DD-006 — Exact listener event provider

Build 001 uses targeted IP Helper queries/waits. Selective ETW or another event source may replace/reduce polling later if measurements justify it.

### DD-007 — Output spool rotation/retention thresholds

The architectural requirement is bounded operational retention and cursor continuity; exact sizes/durations wait for measurement.

### DD-008 — Generic connection/pipe promotion semantics

Network connections and named pipes are high-cardinality. Retention/identity rules should be pressure-tested in later builds.

### DD-009 — Full service delete/recreate continuity algorithm across long observation gaps

Observed delete/create is straightforward. Unobserved same-name resurrection across a gap should remain conservative until implementation evidence defines the strongest witness set.

### DD-010 — Second platform/provider

Likely WSL2/Linux because it offers a strong contrasting process model (`pidfd`, cgroup v2, systemd), but Build 002 chooses based on implementation value at that time.

## Rejected approaches

### R-001 — "Just wrap PowerShell"

Rejected. PowerShell offers valuable structured breadth but cannot be the identity/persistence architecture for native process/file/listener/job concepts.

### R-002 — "Just wrap cmd.exe"

Rejected. Text command execution remains an escape hatch, not a persistent machine world.

### R-003 — "Just SSH/terminal"

Rejected as primary architecture. A terminal byte stream does not provide durable local machine identity, physical file identity, service/task separation, or semantic deltas.

### R-004 — PID as process identity

Rejected. Windows reuses PIDs; the target now exposes a purpose-built process sequence number for reuse detection.

### R-005 — Process rebinding across restart

Rejected. It creates dangerous false-continuity pressure. Restarted worker continuity belongs to a higher-level job/service/workload.

### R-006 — Path as file identity

Rejected. Rename/hard links and replacement/delete-recreate semantics make path a location/binding, not physical identity.

### R-007 — Port number as listener identity

Rejected. Port reuse is expected machine behavior and must create a new listener concept.

### R-008 — Command string equals process/job

Rejected. One command may create a shell, launcher, children, descendants, or no process at all. `cmd_*`, `proc_*`, and `job_*` remain distinct.

### R-009 — Terminal as canonical machine ontology

Rejected. Structured/native execution has stronger information density and identity for ChatGPT.

### R-010 — Full process/file/network dump after each action

Rejected. Interest-driven compact state + bounded deltas + real waits are the normal interface.

### R-011 — Permanent command/output/action ledger

Rejected by the project constraints. Persist only current/recoverable operating state and bounded operational buffers/spools.

### R-012 — Universal object abstraction that erases domain semantics

Rejected. Process, service, task, job, file, listener, session, and terminal lifecycles materially differ. Share common addressing/query conventions without flattening the ontology.

### R-013 — Persistent provider process for every Windows subsystem

Rejected. Most state already lives outside SHELLeye. Separate processes must earn independent lifecycle/isolation value.

### R-014 — Always-on broad ETW ingestion

Rejected for Build 001. It adds event volume/complexity without being necessary to prove the machine-world spine.

### R-015 — USN as mandatory Build 001 recovery layer

Rejected. The first slice can reconcile exact current physical identity without it, and the live ReFS volume currently has no active journal.

### R-016 — Keep arbitrary native handles alive through a permanent broker

Rejected as a default architecture. Process handles can be reopened/revalidated, named Job Objects remain while live members exist, and long-lived file handles can perturb deletion/share semantics. Reconsider only for a concrete capability.

### R-017 — Kernel driver in Build 001

Rejected. No decisive Build 001 capability requires one through the current evidence.

### R-018 — Human-friendly CLI/dashboard as product priority

Rejected. Minimal engineering diagnostics are enough; ChatGPT is the operator.

## Change rule

A frozen decision may change only when implementation/platform evidence demonstrates a materially stronger architecture. When it changes, update the relevant canonical document and this file together.
