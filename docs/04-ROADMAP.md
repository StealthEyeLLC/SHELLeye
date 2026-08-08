# 04 — Roadmap

Status: **Canonical direction; post-Build-001 sequencing remains evidence-driven**  
Baseline: **2026-08-08**

## Governing rule

Do not expand breadth until Build 001 proves the machine-world spine.

Roadmap stages are capability directions, not promises to implement every listed item. Implementation evidence may reorder them. When that happens, update this document and `06-DECISIONS.md` rather than maintaining competing roadmaps.

## Build 001 — Machine World Kernel Slice

Goal: prove the permanent Windows-first spine.

Required:

- C#/.NET 10 restartable kernel;
- SQLite WAL operating state;
- exact process identity and verified actuation;
- named Job Object workload continuity;
- restart-safe output cursors;
- physical file/directory identity;
- compact service/session/volume/listener relationships;
- structured PowerShell object provider;
- waits/deltas/world sync;
- Node 24 Program Host;
- deterministic process/file/listener identity killers;
- kernel/provider recovery;
- 30+ operation local acceptance program;
- measured shell-vs-SHELLeye benchmark.

Do not expand until A–D pass.

## Build 002 — Provider Neutrality Pressure Test

Goal: separate genuinely portable machine concepts from accidental Windows-provider coupling without weakening Windows.

Candidate work:

- formalize provider capability descriptors/facets from Build 001 evidence;
- add one materially different second provider, likely Linux/WSL2;
- exploit Linux-native `pidfd` rather than emulating Windows witnesses;
- map cgroup v2/systemd semantics to job/service concepts where they genuinely correspond;
- test Linux file identity/inotify/fanotify behavior;
- validate execution-context and session model across Windows ↔ WSL boundary;
- pressure-test which API names belong to provider-neutral SDK versus Windows facet.

Success criterion: Windows remains deep while the core ontology survives contact with a different OS.

## Build 003 — Deeper Windows Event / Resource World

Goal: reduce polling and add high-value machine depth only where measured workloads justify it.

Candidates:

- selective ETW process/network providers;
- optional USN journal recovery/scanning on volumes where active and useful;
- richer Task Scheduler registered-task/run model;
- Registry concepts/change waits;
- named-pipe discovery/promoted concepts;
- richer TCP/UDP connection tracking;
- service notifications/subscriptions;
- PDH/performance-counter resource queries;
- on-demand module/thread/handle/file-user relationships;
- Restart Manager file-user provider;
- richer executable/package/application discovery.

Explicit non-goal: turning SHELLeye into a telemetry warehouse.

## Build 004 — Interactive / Remote Machine Worlds

Goal: extend machine operation beyond one local noninteractive Windows execution context.

Candidates:

- ConPTY terminal sessions as compatibility objects;
- persistent terminal/runspace lifecycle and output cursors;
- user/session process launch across local Windows sessions where technically required;
- first-class WSL machine worlds;
- SSH remote-machine provider;
- remote BootEpoch/machine identity/recovery;
- transport replacement/remote protocol work driven by real needs.

Do not conflate remote-machine support with cloud/infrastructure semantics.

## Build 005 — Cross-Substrate Composition

Goal: make SHELLeye the generic machine connective tissue for the StealthEye family without taking over sibling ontologies.

Candidate links:

```text
CODEeye build/artifact → SHELLeye file/process/job
eyeBROWSE browser      ↔ SHELLeye process/listener/file
DESKTOPeye application ↔ SHELLeye process/session
DOCSeye document        ↔ SHELLeye physical file
DATAeye dataset         ↔ SHELLeye physical file/process
```

Add only sparse cross-substrate identities/relations needed for real composition workflows.

## Build 006 — Scale / Hardening

Goal: prove the machine world remains compact, correct, recoverable, and low-overhead at sustained use.

Candidates:

- large-process-count and high-churn stress;
- bounded-delta/cursor pressure;
- thousands of watched/promoted file objects;
- many concurrent jobs and output spools;
- session/logon churn;
- provider outage/recovery storms;
- boot/restart campaigns;
- storage compaction/GC;
- protocol performance and binary encoding only if JSON framing becomes a measured bottleneck;
- multi-machine scale if remote providers now justify it.

## Later / separate substrate domains

Do not pull these into SHELLeye merely because SHELLeye can execute their CLIs:

- cloud resource ontology;
- Kubernetes/deployment topology;
- Terraform/infrastructure state semantics;
- browser DOM/AX semantics;
- compiler/source/build meaning;
- GUI control semantics;
- document/data semantic models.

A future INFRAeye or sibling substrate can own distributed/cloud semantics while SHELLeye remains the generic machine layer underneath.

## Deferred extreme options

Only reconsider after measured evidence:

- kernel driver;
- Windows kernel modifications;
- always-on universal handle capture;
- broad ETW ingestion;
- WFP/minifilter drivers for exact arbitrary network/file handle lifetimes;
- custom PowerShell fork;
- persistent native broker solely to keep arbitrary handles alive.

Each must earn itself through a capability impossible or materially weaker through supported user-mode interfaces.
