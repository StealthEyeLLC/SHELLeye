# SHELLeye Build 002 — Provider Neutrality Pressure Test

Status: **PROSPECTIVE / FROZEN BUILD 002 EXPERIMENTAL ENVELOPE**  
Freeze date: **2026-08-12**  
Branch: `build/build002-provider-neutrality`  
Build 001 baseline: `54d2070365c04205fe4593e1d95ea76c302a709a`  
Research input: `docs/10-BUILD-002-RESEARCH.md`

This document freezes the Build 002 experimental envelope before provider-neutral source implementation. It is subordinate to the measured Build 001 authority and may change only through an explicit evidence-driven Build 002 amendment recorded prospectively before a new measured acceptance freeze.

## 1. Scientific / engineering question

> **Does the SHELLeye ontology survive contact with a materially different Linux provider while Windows remains as deep, exact, compact, and recovery-safe as measured Build 001?**

A successful result does not require every Build 001 concept to remain universal. Evidence-driven narrowing or provider facets are valid if:

- the remaining common semantics are real and useful;
- Windows capability is not weakened to manufacture symmetry;
- Linux uses Linux-native identity/actuation truth;
- wrong-target actuation remains zero;
- provider differences are surfaced explicitly rather than hidden.

## 2. Frozen providers

### Provider A — Windows

```text
host: STEALTHEYELLC
provider kind: windows
current DisplayVersion: 25H2
current build: 26200.9168
Build 001 provider: existing Windows-native implementation
```

The Build 001 Windows provider is a regression baseline, not a draft.

### Provider B — Linux / WSL2

```text
host relationship: hosted by STEALTHEYELLC
provider kind: linux-wsl2
distribution: Ubuntu-24.04
registration: {aa957c59-794f-4ad3-ae28-9188cae51ee3}
WSL version: 2
installed WSL package: 2.7.11.0
last canonical measured Linux kernel evidence: 6.18.33.2-2
```

The Linux kernel version, systemd state, cgroup state, namespace identities, pidfd/statx capabilities, filesystem topology, and other Linux facts **must be freshly measured inside the owner distribution before measured acceptance**. The 2026-08-08 kernel observation is not a substitute for that gate.

## 3. Current access constraint

The connected machine-control path currently executes as `NT AUTHORITY\SYSTEM`. On this host, WSL explicitly rejects that context with:

```text
Wsl/WSL_E_LOCAL_SYSTEM_NOT_SUPPORTED
```

The owner distribution is user-contextual.

Therefore:

- Build 002 research, source implementation, Windows regression, cross-compilation, deterministic provider-contract tests, persistence tests, Program Host work, and acceptance preparation proceed now.
- Development mocks may be used only for unit/provider-contract testing.
- No mock, Windows-side WSL registration, offline VHD inspection, or Linux VPS substitutes for real Provider B measured evidence.
- Until a genuine owner-context WSL invocation is available, final Provider B acceptance classification is **INCONCLUSIVE / PENDING REAL PROVIDER**, not pass.

## 4. Frozen architecture envelope

### 4.1 Provider-qualified machine worlds

Build 002 introduces an explicit provider-qualified world descriptor. Minimum semantics:

```text
worldId
providerKind
providerKey
hostMachineId
state
worldEpoch
capabilities
```

Windows remains the existing host machine world.

The Ubuntu WSL2 distribution is a **distinct hosted Linux machine world**. It is related to the Windows host through a sparse `HOSTS`/host relation. It is not merged with the Windows `machine_*` identity.

### 4.2 Provider-world epoch

The portable common concept is a **provider-world epoch**: a boundary beyond which transient provider-native identity must not silently continue.

Windows retains its existing deep `BootEpoch` facet unchanged.

The WSL/Linux epoch candidate is derived from native evidence including:

```text
Linux kernel boot_id
+ distro PID namespace identity
+ distro init (/proc/1) start-time ticks
+ stable distro/provider identity
```

Kernel `boot_id` alone is insufficient for WSL distro incarnation because a distribution can terminate/restart independently of the shared WSL2 kernel.

Any uncertain or changed epoch advances the logical provider-world epoch rather than rebinding transient process/listener/workload identity.

### 4.3 Process contract

Common semantics that survive:

- `proc_*` is exactly one native process lifetime;
- PID is a locator, not identity;
- process restart/relaunch creates a new `proc_*`;
- parent relationships carry evidence quality;
- exact actuation may never target a stale PID alone;
- loss of exact identity produces a structured abstention rather than a rebound.

Windows facet remains Build 001:

```text
BootEpoch + PID + Windows process sequence + creation time
-> verified native process handle
-> act/wait through that handle
```

Linux facet:

```text
provider-world epoch
+ PID
+ /proc/<pid>/stat start-time ticks
+ provider/namespace qualification
-> pidfd_open(PID)
-> verify retained epoch/start-time while pidfd is held
-> wait/pidfd_send_signal through the pidfd
```

A pidfd is a live process-lifetime witness and is never serialized as a cross-provider-restart identity token.

Linux `exec` preserves the same process incarnation when PID/start-time/pidfd lifetime remains the same; image replacement is a process property change, not a new `proc_*`.

### 4.4 File contract

Common semantics that survive:

- `file_*` / `dir_*` denote physical provider objects, not path strings;
- paths are bindings/locators;
- rename may preserve a physical file;
- delete/recreate and replacement must not rebind an old concept;
- exact mutation must resolve/verify the intended physical object and act through the strongest native anchor available;
- after an unobserved provider gap, continuity requires evidence strong enough for that provider or is conservatively lost.

Windows physical/file-gap semantics remain unchanged.

Linux current witness:

```text
provider-world epoch
+ filesystem/device identity
+ inode
+ statx mount identity (unique mount ID when available)
```

Linux optional persistent file-handle facet:

```text
name_to_handle_at
+ filesystem handle bytes/type
+ mount identity
-> open_by_handle_at
```

If an exportable handle is unavailable, Linux may retain exact current-runtime operations but must not claim exact file continuity across an unobserved provider/kernel gap from path + inode alone.

Linux exact retained write may operate through the exact opened/reopened file descriptor after identity verification. Linux rename/delete through a retained logical file is implemented only if the provider can preserve the same exact-target invariant; otherwise the operation returns `unsupported_by_provider` rather than introducing a path race.

### 4.5 File observation

Targeted `inotify` is the preferred Build 002 Linux dirty-signal candidate.

The common rule remains:

```text
provider event = evidence that reality may have changed
native current query = current truth
queue overflow/provider gap = reconciliation
```

No broad fanotify/eBPF/journald telemetry stream is authorized.

### 4.6 Workload / job pressure

The common core is deliberately narrow:

```text
provider-scoped workload/group
membership
lifecycle/current membership state
provider-qualified group termination capability where supported
```

Windows Job Objects remain the deep Windows facet.

Linux cgroup v2 hierarchy/controllers/delegation are Linux facets and are not asserted to equal Windows Job Objects.

**Build 002 implementation gate:** discover/report cgroup v2 capability. Full cgroup creation/mutation is research-classified and not required for the frozen measured slice.

### 4.7 Service pressure

The common service meaning is narrowed to a provider-scoped managed service registration/current state and provider-supported process relation.

Windows SCM remains the Windows facet.

systemd service units are a Linux facet. Targets, sockets, mounts, scopes, slices, timers, and other unit details do not become universal service fields.

**Build 002 implementation gate:** discover/report systemd availability. Full Linux service mutation is not required by the frozen measured slice.

### 4.8 Listener pressure

The common listener concept remains transient:

```text
protocol/local endpoint
+ provider-native incarnation evidence
+ exact owner-process relation when provable
+ observation generation
```

Windows IP Helper bind/create timestamp remains a Windows facet.

Linux socket diagnostics are a Linux facet.

**Build 002 implementation gate:** Linux listener implementation is deferred from this bounded slice. Research has established the provider seam; process/file/world semantics provide the decisive Build 002 identity pressure.

### 4.9 Execution context

Common execution context contains only semantics that actually survive:

```text
provider/world qualification
principal/credential facts
working directory when known
environment at launch when known
provider-native context facets
```

Windows session/token/WTS identity remains a Windows facet.

Linux UID/GID/groups and PID/user/mount/cgroup namespace identities are Linux facets.

No Linux UID or namespace is translated into a fake Windows interactive session.

### 4.10 Cross-provider correspondence

Windows and WSL path translation is never physical identity proof.

Do not automatically merge objects visible through:

```text
C:\...
/mnt/c/...
\\wsl$\Ubuntu-24.04\...
Linux root filesystem paths
```

Build 002 may expose sparse host/provider/path correspondence facts, but provider object identities remain separate unless exact evidence for a particular relation is established.

## 5. Frozen implementation slice

Build 002 source implementation is limited to the following provider-pressure work:

1. provider-qualified world persistence/descriptors;
2. Windows world represented through the new provider-qualified descriptor without changing Build 001 behavior;
3. Ubuntu-24.04 WSL2 provider bridge using explicit executable/argv, not shell-text scraping;
4. a small Linux-native helper cross-published from the existing .NET toolchain;
5. Linux capability probe for kernel/world epoch, UID/GID/namespaces, pidfd, statx, mount topology, inotify, cgroup v2, systemd, and exportable file-handle support;
6. Linux process retain/start/inspect/wait/terminate with `/proc` + pidfd exactness;
7. Linux physical file retain/inspect/read/exact-write with statx and optional exportable file handles;
8. conservative Linux file recovery when strong persistent-handle evidence is unavailable;
9. provider-qualified bounded deltas;
10. provider-aware `world.sync` for retained/provider-scoped interests, never an automatic full Linux dump;
11. provider-aware Program Host SDK while preserving old Windows call shapes;
12. deterministic common/provider-contract tests plus Linux-hostile acceptance harness;
13. measured real-provider acceptance when owner-context WSL becomes available.

Explicitly outside the frozen Build 002 implementation gate:

- Linux cgroup creation/controller management;
- systemd service mutation breadth;
- Linux listener implementation;
- fanotify product provider;
- broad procfs/journald/netlink/eBPF ingestion;
- remote Linux/SSH;
- cloud/Kubernetes/Terraform semantics;
- cross-provider identity merging;
- sibling-Eye architecture changes.

## 6. Structured error contract

Provider-neutral operations must distinguish at least:

```text
unsupported_by_provider
provider_unavailable
stale
ambiguous
permission_denied
destroyed
not_found
invalid_argument
timeout
native_error
```

A generic shell exit string is not the semantic error contract.

## 7. Windows regression gates

The pre-change Build 001 baseline was rerun on 2026-08-12 and passed:

```text
Release build: PASS / 0 warnings / 0 errors
Build 001 hostile core: 25 / 25 PASS
false process rebounds: 0
false file rebounds: 0
false listener rebounds: 0
wrong process mutations: 0
wrong file mutations: 0
wrong-object mutations: 0
```

After every meaningful provider-contract change, rerun at minimum:

1. solution Release build;
2. Build 001 25-case hostile core;
3. provider-contract deterministic tests.

The final Build 002 acceptance candidate must rerun the full frozen Build 001 hostile core. Any Windows failure blocks Build 002 success.

## 8. Provider B measured gates

These gates are prospective. They may not be weakened after failures are observed.

### L0 — Live provider capability bind

From the real Ubuntu-24.04 WSL2 distribution, record:

- WSL distribution identity/version;
- Linux kernel release;
- machine-id/provider identity evidence;
- kernel boot_id;
- PID namespace identity;
- `/proc/1` start-time ticks;
- provider-world epoch;
- UID/GID/groups;
- mount topology and root filesystem;
- systemd availability/version/state;
- cgroup version/mount;
- pidfd support;
- statx support including mount-ID form;
- inotify availability;
- exportable file-handle support on the acceptance filesystem.

### L1 — Linux process identity / exact actuation

Required real cases:

1. two simultaneous instances of the same executable are distinct `proc_*` concepts;
2. exit/recreate produces a new concept;
3. old logical process actuation is rejected after exit/recreate;
4. exact signal/termination uses pidfd after witness verification;
5. held pidfd never redirects to a replacement process;
6. bounded real PID-reuse stress is attempted and any observed reuse is correctly rejected;
7. an `exec` transition preserves the same process concept when native lifetime is unchanged;
8. parent relationship quality does not manufacture a false exact parent.

Hard metrics:

```text
false Linux process rebounds = 0
wrong Linux process mutations = 0
```

### L2 — Linux file identity / exact actuation

Run on the WSL Linux filesystem, not `/mnt/c`, unless a separately identified mount-pressure case is being executed.

Required real cases:

1. retain current physical file;
2. content change preserves physical concept and changes revision;
3. same-filesystem rename preserves concept when provider evidence can still resolve it;
4. hard link resolves to the same physical concept when supported;
5. delete/recreate at the same path creates a new concept;
6. same-content recreation still creates a new concept;
7. stale old logical file write cannot mutate the replacement;
8. if an exported file handle is available, delete/recreate or inode-reuse pressure must make the old handle stale rather than rebound;
9. exact retained rename/delete must either meet the exact-target contract or report `unsupported_by_provider`;
10. a mount/provider boundary case must not manufacture continuity.

Hard metrics:

```text
false Linux file rebounds = 0
wrong Linux file mutations = 0
```

### L3 — SHELLeye kernel recovery with Linux provider live

```text
retain live Linux process + file
-> hard-stop/restart SHELLeye kernel only
-> Provider B itself remains running
-> reconcile
```

Required:

- same provider-world epoch if native evidence proves it;
- same still-live process recovered only when epoch + PID + start-time evidence agrees;
- file recovered exactly only when frozen strong provider evidence supports gap continuity;
- otherwise explicit stale/ambiguous/destroyed, never rebound;
- bounded deltas describe meaningful changes/recovery without event-history invention.

### L4 — Actual WSL distribution restart boundary

```text
record Windows BootEpoch + Linux provider-world epoch
-> stop the selected WSL distribution through the supported WSL lifecycle
-> start it again
-> re-probe
```

Required:

- Windows BootEpoch remains independent of the WSL distro lifecycle;
- Linux provider-world epoch changes when the distro incarnation changes;
- all old Linux process concepts are terminal/stale and cannot act on new PIDs;
- Linux file continuity is retained only if the provider's frozen strong file witness proves it across the boundary;
- no false process/file rebound.

### L5 — Cross-provider hostile non-merge

For at least one Windows-hosted path reachable from WSL, observe both provider views.

Required:

- provider-qualified Windows and Linux observations remain distinct identities;
- path translation is reported only as a correspondence/location fact;
- no old logical object from one provider can mutate another provider's object merely because the paths map to the same host storage location.

## 9. Delta / world-sync acceptance

Build 002 must preserve delta-first behavior.

Required:

- Linux/provider deltas include provider/world qualification;
- bounded cursor semantics remain intact;
- `world.sync` reconciles retained/promoted interests and provider states;
- no default full procfs/mount/systemd/socket dump;
- provider unavailable is represented explicitly without invalidating unrelated Windows truth.

## 10. Program Host measured gate

Build 001 measured 52 typed operations in one Program Host invocation. Build 002 prospectively freezes a substantial but pressure-focused threshold:

```text
one Program Host invocation
>= 40 meaningful typed SHELLeye operations
>= 12 operations against the real Linux provider
at least one exact Linux process lifecycle
at least one exact Linux file lifecycle
provider/world inspection
world.sync + bounded delta consumption
Windows operations remain part of the same provider-aware SDK surface
0 model calls between primitives
```

The threshold may not be lowered after seeing measured failures.

The Program Host must be able to tell which provider/world every provider-sensitive object belongs to.

## 11. Benchmark rule

No Build 002 performance claim is required.

A conventional-shell comparison may be run only if its exact workload and measurement are recorded before the measured run. Otherwise report no Build 002 benchmark rather than selecting a favorable comparison after the fact.

## 12. Acceptance-candidate freeze

Before the first measured Build 002 acceptance case, record and bind:

- exact implementation commit;
- exact Build 002 spec blob/commit;
- exact research artifact blob/commit;
- decision amendments, if any;
- Windows build/version;
- WSL version/distribution registration;
- fresh Linux kernel/provider probe;
- Linux helper binary SHA-256;
- Program Host files/hash or bound tree;
- test harness files/hash or bound tree;
- runtime/state layout;
- clean working tree.

## 13. No source patching inside a measured run

Once a measured acceptance campaign begins:

- source is frozen;
- a defect requiring source repair ends/preserves that run as failed/incomplete;
- repair occurs prospectively;
- a new candidate freeze is created;
- the full affected acceptance campaign restarts.

Do not erase the first result.

## 14. Allowed evidence-driven amendments

Implementation-pressure amendments are allowed only for Build 002 semantics actually exercised by the real provider:

- machine/provider-world boundary;
- provider-world epoch witness;
- process identity/actuation witness;
- file identity/recovery witness;
- provider-specific exact file mutation capability;
- execution-context qualification;
- common-versus-facet SDK classification;
- structured provider error semantics.

Each amendment must record:

```text
observed evidence
failed assumption
old contract
new bounded contract
why it remains within Build 002
```

No unrelated Build 001 refactor or sibling architecture redesign is authorized.

## 15. Non-goals

Build 002 does not authorize:

- Build 003 Windows telemetry/resource expansion;
- Build 004 remote/SSH machine worlds;
- Build 005 cross-Eye composition;
- broad Linux feature coverage;
- universal command ontology;
- infrastructure/cloud/Kubernetes/Terraform ontology;
- GUI/browser/compiler/source semantics;
- permanent action ledger;
- verifier-agent pipeline;
- approval/risk/policy services;
- a second autonomous planner;
- another permanent Windows SCM service.

## 16. Frozen final classification

After measured acceptance, use exactly one of:

```text
PASS — provider-neutral spine survived with Windows depth preserved
PASS WITH NARROWING — evidence-driven de-universalization preserved a useful common core
FAIL — common ontology or exactness did not survive
INCONCLUSIVE — real Provider B measurement could not be completed
```

The present locked/LocalSystem condition requires `INCONCLUSIVE` unless and until real owner-context Provider B measurement actually runs. Implementation or mocks alone cannot change that classification.

## 18. Prospective acceptance-evidence binding amendment — 2026-08-12

Status: **PROSPECTIVE / PRE-MEASUREMENT**.

This amendment fixes implementation-era evidence names and one bounded stress count before any real Linux measured acceptance case has run. It does not change the Build 002 success criterion or weaken any gate above.

Frozen repository evidence paths:

- acceptance-candidate binding: `docs/13-BUILD-002-ACCEPTANCE-FREEZE.md`;
- measured Build 002 result, created only after a measured campaign actually runs: `docs/14-BUILD-002-RESULTS.md`.

Frozen acceptance harnesses:

- `program-host/src/build002-acceptance.js` — one persistent Program Host connection; >=40 successful typed operations; >=12 successful Linux-provider operations;
- `tests/acceptance/build002-linux-pid-reuse-stress.js` — exactly **256** real short-lived Linux process launch/wait iterations; reuse is best-effort, any observed reuse must not rebound;
- `tests/acceptance/build002-recovery-prepare.js` + `tests/acceptance/build002-recovery-recover.js` — two-phase SHELLeye-kernel recovery gate;
- `tests/acceptance/build002-distro-restart.js` — actual selected WSL distribution termination/restart boundary through the owner-context SHELLeye kernel.

Frozen Linux helper publication path for acceptance preparation:

`C:\SHELLeye\runtime\linux\app\SHELLeye.Platform.Linux`

The helper binary hash is **not** frozen here because implementation is still being finalized. Its exact SHA-256 must be bound in `docs/13-BUILD-002-ACCEPTANCE-FREEZE.md` after the implementation commit is published and re-built from that commit.
