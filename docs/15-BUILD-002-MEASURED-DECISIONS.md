# SHELLeye Build 002 — Measured Decisions Addendum

Status: **CANONICAL / MEASURED / FROZEN THROUGH BUILD 002**  
Date: **2026-08-12**  
Build 002 final classification: **PASS — provider-neutral spine survived with Windows depth preserved**  
Measured results: `docs/14-BUILD-002-RESULTS.md`  
Operative final measured freeze: `docs/13-BUILD-002-ACCEPTANCE-FREEZE.md`

This document is the measured Build 002 addendum to `docs/06-DECISIONS.md`.

`docs/06-DECISIONS.md` remains the historical Build 001 decision baseline. The decisions below record only the provider-neutrality conclusions actually earned by Build 002 measurement. They do not authorize Build 003, sibling-Eye redesign, World Kernel changes, cloud/remote-machine ontology, or a broader universal-computer schema.

## Build 002 measured decisions

### D-031 — The common SHELLeye spine is provider-qualified, not provider-flattened

The common core survives across Windows and Linux only where the semantics are genuinely shared: provider-qualified machine worlds, one-native-lifetime process concepts, physical-file concepts, conservative identity, bounded deltas, coherence barriers, exact/abstaining actuation, and local Program Host computation.

Provider-native witnesses remain authoritative. A common SDK operation is common only when its meaning is true on both providers; otherwise the capability remains a typed provider facet or explicitly unsupported.

Windows capabilities measured in Build 001 are not weakened to obtain portability.

### D-032 — Ubuntu-24.04 WSL2 is a distinct hosted Linux machine world

The selected second provider is the registered `Ubuntu-24.04` WSL2 distribution on STEALTHEYELLC.

The Linux world is hosted by the Windows machine but is not the same machine-world incarnation as Windows and is not a Windows interactive-session alias.

A Linux provider-world incarnation is conservatively qualified by provider/distribution identity plus Linux-native generation evidence, including:

```text
Linux boot_id
PID namespace identity
PID 1 start-time ticks
registered distribution/provider identity
```

Windows `BootEpoch` does not imply that the WSL distribution remained in the same Linux provider-world incarnation. Actual distro restart advances the Linux world epoch even when Windows `BootEpoch` is unchanged.

This resolves Build 001 deferred decision `DD-010 — Second platform/provider`: **Ubuntu-24.04 WSL2 was selected, implemented, and measured successfully for Build 002.**

### D-033 — Linux `proc_*` remains exactly one native Linux process lifetime

Linux PID is a locator, never permanent identity.

The measured Linux retained-process witness is provider-world epoch + PID + `/proc` start-time evidence. Exact native actuation/wait uses pidfd-backed operations where supported.

A Linux `exec` may change the process image without changing the underlying process lifetime when PID/start-time/pidfd continuity remains exact.

A process that exits and a later process that reuses the same PID are different concepts. Uncertain identity produces stale/destroyed/ambiguous outcome rather than rebound.

Measured Build 002 outcome:

```text
Linux PID stress: 256 / 256 PASS
false Linux process rebounds: 0
wrong Linux process mutations: 0
```

### D-034 — Linux generic file identity is physical and provider-native

Linux pathnames are bindings, not physical file identity.

The measured Linux current physical-file witness uses native statx-derived device/inode/mount identity qualified by Linux provider-world incarnation. Where the filesystem exports a stable native file handle, that handle is the strong recovery witness across an unobserved SHELLeye-kernel gap.

Consequences:

- rename may preserve one physical file concept;
- hard links may resolve to one physical file concept;
- unlink/delete plus recreate at the same pathname creates a new concept;
- retained exact writes must not target a pathname replacement;
- where strong post-gap evidence is unavailable, SHELLeye abstains rather than manufacturing continuity.

Measured L3 recovered the retained ext4 file exactly through its strong exported handle. Measured L4 distro restart correctly destroyed the old Linux file concept rather than rebinding it.

### D-035 — Provider-specific facets and asymmetry are permanent architecture, not failure

Build 002 rejects a lowest-common-denominator provider interface.

Windows remains free to use Windows-native capabilities such as deep process handles/sequence evidence, Job Objects, `FILE_ID_INFO`, SCM, WTS, IP Helper, NTFS continuity evidence, and structured PowerShell.

Linux remains free to use Linux-native capabilities such as pidfd, `/proc`, statx, exported file handles, UID/GID/groups, namespaces, cgroup v2, inotify, and systemd facets.

PowerShell remains a Windows facet. Build 002 does not invent a fake Bash-object symmetry. Windows does not acquire fake pidfd/inode/systemd semantics. Linux does not acquire fake Windows handle/Job Object/SCM semantics.

### D-036 — WSL provider lifetime is distinct from Linux provider-world identity

WSL may tear down a distribution when no suitable Windows-side provider lifetime remains, so a SHELLeye-kernel-only recovery test cannot assume Linux userspace persists automatically.

The measured Build 002 implementation uses a compact StealthEye-owner Windows WSL lifetime anchor whose persisted correspondence includes exact Windows PID/start-time/executable plus distro/provider qualification. The anchor is created outside the SHELLeye kernel task lifetime so provider userspace can remain live while the kernel is intentionally killed and restarted.

This anchor is a **WSL transport/lifetime facet**, not Linux process identity, not a universal machine-world field, and not a permanent Windows service. A future implementation may replace it only with an equal-or-stronger provider-supported lifetime mechanism that preserves the same conservative recovery contract.

### D-037 — Linux launched-workload lifetime may use a systemd provider facet without making systemd universal ontology

For Build 002 direct Linux launch, SHELLeye may use a systemd transient lifetime mechanism to keep the actual workload alive independently of the short-lived provider bridge/kernel while establishing exact PID/start-time/pidfd identity before the target image runs.

Systemd ownership does not become the logical process identity. Later inspection, recovery, wait, and actuation remain tied to native process witnesses.

This is a Linux/WSL provider lifecycle facet, not a claim that Windows Job Objects and systemd units are universally equivalent.

### D-038 — Cross-provider shared-path visibility never proves identity by translation alone

A Windows path and a WSL path such as `C:\...` and `/mnt/c/...` may expose related host-backed storage, but pathname translation alone does not authorize hard identity merge.

Cross-provider correspondence remains sparse and evidence-qualified. The measured hostile cross-provider case retained separate provider-qualified identities and produced:

```text
cross-provider false identity merges: 0
```

### D-039 — Provider reconciliation terminalizes dead native witnesses conservatively

`world.sync` remains a retained/promoted-interest coherence barrier, not a full telemetry dump and not a verifier-agent pipeline.

When a retained Linux native file witness is definitively gone (including native dead/stale-handle outcomes such as provider-classified `ENOENT`/`ESTALE`), reconciliation terminalizes that concept rather than aborting global synchronization or rebinding to a pathname replacement.

Provider changes remain bounded semantic deltas; no permanent broad procfs/journald/netlink/fanotify/eBPF ingestion is introduced.

### D-040 — Rich local computation remains provider-neutral at the Program Host layer

Provider neutrality does not reintroduce model round trips per shell primitive.

The final measured Build 002 Program Host invocation used one persistent connection and completed:

```text
typed SHELLeye operations: 55
real Linux-provider operations: 38
model calls between primitives: 0
```

A post-L4 provider-aware Program Host invocation also passed on the new Linux provider-world incarnation.

The Program Host remains disposable local computation with no canonical state and no second autonomous model/agent.

## Measured recovery boundaries

Build 002 establishes two distinct Linux/WSL recovery semantics:

1. **SHELLeye-kernel-only gap while Provider B remains live** — same Linux provider-world incarnation may be recovered only when native witnesses still prove exact continuity. Final measured L3 recovered both the Linux process and strong-handle file exactly.
2. **Actual WSL distribution restart** — the Linux provider-world incarnation changes. Old transient Linux process/file identities must not rebound. Final measured L4 advanced the Linux world epoch with Windows `BootEpoch` unchanged and produced zero false rebounds / zero wrong-object mutations.

These boundaries must not be collapsed into one generic "restart" semantic.

## Unchanged constraints

Build 002 does not change the following governing constraints:

- native providers own material machine truth;
- false correspondence is worse than temporary duplication/loss of continuity;
- sibling substrate boundaries remain hard;
- no browser DOM/network meaning enters SHELLeye;
- no desktop GUI semantics enter SHELLeye;
- no source/compiler/Git engineering meaning enters SHELLeye;
- no cloud/infrastructure ontology is added;
- no telemetry warehouse, permanent verifier agent, approval database, or generic action ledger is added;
- no extra permanent Windows SCM service is added;
- Program Host remains the rich local multi-operation surface;
- Build 003 remains **unauthorized** until separately researched/frozen.

## Canonical Build 002 status

Build 002 is **COMPLETE / MEASURED / PASSED**.

Canonical classification:

> **PASS — provider-neutral spine survived with Windows depth preserved**

The measured evidence supports the provider-qualified architecture recorded above. No additional Build 003 work is implied or authorized by this addendum.
