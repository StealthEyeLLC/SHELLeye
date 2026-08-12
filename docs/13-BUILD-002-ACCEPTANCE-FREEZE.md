# SHELLeye Build 002 — Measured Acceptance Freeze, Run 003

Status: **ACTIVE MEASURED-CANDIDATE FREEZE — RUN 003; L0 COMPLETE; L1–L5 NOT STARTED**

Date: 2026-08-12
Branch: `build/build002-provider-neutrality`

This document prospectively binds Build 002 measured acceptance Run 003. It replaces the obsolete Run 002 status text previously occupying this path; it does not erase Run 001 or Run 002 history. No measured Run 003 L1–L5 case has begun at the time this freeze is authored.

## 1. Preserved prior runs

Run 001 and Run 002 remain failed/ended historical evidence in branch history and under `evidence/build002/run-001/` and `evidence/build002/run-002/`.

Run 003 prospective work performed before this freeze — including earlier L3/L4 pressure tests — is development evidence only and is **not** counted as measured Run 003 acceptance.

## 2. Execution serialization authority

Run 003 is serialized by provider commit:

```text
execution lease commit: 8d0112a397873b54bf6f8168ade87de985b73bf1
lease artifact: evidence/build002/run-003/EXECUTION-LEASE.md
legacy-task snapshot SHA-256:
AAB84C97B450B2E4F0B9D24CBC50FB8007D4C845BCEC8F0EEAB3E521D2A89855
```

The lease is evidence/control-plane only. It does not alter product source. During measured Run 003, no parallel SHELLeye Build 002 path may start/stop/replace the kernel, terminate Ubuntu-24.04, create a competing runtime/pipe, reuse the measured state database, or mutate Build 002 product source.

Sibling Eyes remain outside this lease and are not mutated.

## 3. Frozen implementation authority

```text
implementation commit: 6bb4806a64e27b82e7e664f6ad915364fe8d99b6
implementation tree:   ac54028898a4257cf04ca23dac7a373aacb69203
implementation parent: 79d0efa8853ad3c39d4781cba3aaf6706a836d2f
```

The implementation commit is the product-source authority for Run 003. The execution-lease and this freeze are downstream evidence/control commits only.

Repository bindings:

```text
Build 002 research blob:
a7faba9d4356afcebc1a83679fbd1cb4d4c2e448

Build 002 specification blob:
bfc4c66d1df605b90a4c3fe15bf18c3482c12fcb

provider matrix blob:
f49b5e547ca139098a98ec903c05e7fab6b5c245

Program Host tree:
5411a5cd66865b024ef88d2a6232259dbe956575

acceptance-test tree:
0015208c988a7457c8aa667714b45da9c824f867

Linux helper source tree:
9cd8730f4f8e8b9b6d03d320e0e1c27a166d5d50
```

## 4. Explicit bounded provider-pressure amendments

These amendments are prospective Build 002 implementation-pressure decisions. They are provider facets/mechanisms, not new universal ontology.

### 4.1 Linux process lifetime across SHELLeye kernel death

Observed evidence: a Linux workload launched as a child of the short-lived WSL provider bridge can disappear when that bridge/kernel disappears, even when PID/start-time identity was initially exact.

Old implementation contract: direct provider-child `Process.Start` was sufficient to launch a retained Linux process.

Run 003 bounded contract: on the selected systemd-capable Provider B, `process.start` uses a transient systemd unit plus an exact Unix-domain pre-exec handshake. SHELLeye verifies systemd MainPID, PID, start ticks, and pidfd while the launch proxy is still the same process lifetime; only then does the proxy `execv` the requested executable. Systemd owns only the Linux workload lifetime facet. SHELLeye identity and actuation remain PID + start-time + pidfd based.

This does **not** universalize systemd or equate a systemd unit with a Windows Job Object.

### 4.2 WSL provider-incarnation lifetime across SHELLeye kernel death

Observed evidence: WSL may shut down the distro when Windows-side WSL clients disappear; Linux systemd services do not by themselves keep the WSL instance alive.

Old implementation contract: a WSL lifetime client created inside the kernel/task process tree was sufficient.

Run 003 bounded contract: the owner-context kernel uses WMI process creation to create one exact StealthEye-owned `wsl.exe ... /usr/bin/sleep infinity` provider-lifetime anchor outside the kernel task tree. Its Windows PID, start time, executable path, distro, and provider key are persisted and must all match before adoption after kernel restart.

This anchor is transport/provider lifetime only. It is not a second autonomous brain, daemon ontology, service ontology, or substitute for Linux-native object identity.

### 4.3 Strong Linux file recovery

Observed evidence: exported ext4 file handles were available, but `open_by_handle_at` recovery requires a mount FD suitable for the syscall.

Old implementation: mount FD used `O_PATH`.

Run 003 bounded contract: the mount FD used for strong exported-handle recovery is opened read-only (`O_RDONLY | O_CLOEXEC | O_DIRECTORY`) before `open_by_handle_at`. Exact recovery still requires the frozen exported handle and device/provider evidence; failure remains stale/ambiguous/destroyed rather than pathname rebound.

This does not weaken file identity or make paths authoritative.

## 5. Published-commit deterministic validation

All retained deterministic gates were rerun against a clean detached checkout of implementation commit `6bb4806a...`:

```text
Release build: PASS
warnings: 0
errors: 0
provider-contract suite: 5 / 5 PASS
Build 001 Windows hostile core: 25 / 25 PASS
false process rebounds: 0
false file rebounds: 0
false listener rebounds: 0
wrong process mutations: 0
wrong file mutations: 0
wrong-object mutations: 0
implementation diff --check: PASS
frozen JavaScript syntax checks: PASS
```

Windows Build 001 depth therefore remains a hard retained baseline for Run 003.

## 6. Frozen measured runtime

Host/runtime facts:

```text
Windows: 25H2 / 26200.9168
WSL package: 2.7.11.0
selected distribution: Ubuntu-24.04
registration: {aa957c59-794f-4ad3-ae28-9188cae51ee3}
Node Program Host: v24.18.1
Node SHA-256:
AC51903C4C111815D52280B1FDCC8DA067CBB37E2FE1A765097B85C3292C8582
```

Measured layout:

```text
pipe: shelleye-build002-run003-measured
state root: C:\SHELLeye\state\build002-run003-measured
state DB: C:\SHELLeye\state\build002-run003-measured\shelleye.db
kernel runtime root: C:\SHELLeye\runtime\build002-run003-measured\kernel
Linux runtime root: C:\SHELLeye\runtime\build002-run003-measured\linux\app
spool root: C:\SHELLeye\spool\build002-run003-measured
temp root: C:\SHELLeye\Temp\build002-run003-measured-runtime
```

Published implementation runtime equality:

```text
kernel publish files: 546 / 546 exact SHA-256 manifest match
kernel managed DLL SHA-256:
822237700CC99133E5C2376959B104D2190F33F09FAA163A82315D5677826617
kernel apphost SHA-256:
CCBDAF2FBCA69E77F70977C2A4DDD524D3A054A809D5FEB32771629BE8C2F2E3

Linux helper publish files: 192 / 192 exact SHA-256 manifest match
Linux helper executable SHA-256:
33533827987DAA5A7801CC2EC88407CA941379594D313557B6845DBCB5163F61
Linux helper managed DLL SHA-256:
726E27D9016A9BBDBB0174D95CEC1AA66A3A2D4EBD14A20DC4AF4FF932D7A22C
```

The measured kernel is invoked through `SHELLeye.Run003.Measured.exe`, an acceptance-runtime alias that is byte-identical to the published `SHELLeye.Kernel.exe` apphost (same SHA-256 above). It is not a source patch.

Raw binding: `evidence/build002/run-003/runtime-bind.json`.

## 7. Fresh owner-context L0 bind

Raw L0 evidence:

```text
evidence/build002/run-003/l0-owner.json
SHA-256: D4D16001968B809764D1C2DA53B6944ADB0856D116CD706025DC1562ABACE6A2
```

Owner/provider facts:

```text
Windows owner context: stealtheyellc\stealtheye
Windows session: 1
kernel PID at bind: 31612
Windows BootEpoch: boot_1
kernel epoch: kernel_1
PowerShell provider epoch: provider_1

Provider world ID: world_1
provider kind: linux-wsl2
provider key: wsl:{aa957c59-794f-4ad3-ae28-9188cae51ee3}
distribution: Ubuntu-24.04
OS: Ubuntu 24.04.4 LTS
Linux kernel: 6.18.33.2-microsoft-standard-WSL2
machine-id: 9bd442ff38984ccaadd4f6e3f55669f5
kernel boot_id: b4c9749a-e805-4b43-b022-57448bd8f6a6
PID namespace: pid:[4026532221]
/proc/1 start ticks: 89
provider-world epoch: linux_epoch_0c6fc9e8023c13b8359a0725
root filesystem: ext4 on /dev/sdd
systemd: available / running
cgroup: v2
pidfd: supported
statx: supported
unique mount ID: supported
inotify: supported
exportable file handle: supported on acceptance filesystem
```

The measured WSL lifetime anchor is owner `STEALTHEYELLC\StealthEye`, Session 1, parented by `WmiPrvSE`, and its persisted PID/start-time/executable/distro/provider-key witness matches the live process exactly.

At freeze binding, exactly two Ubuntu WSL clients are allowed and present: the measured provider-lifetime anchor and the measured kernel’s provider bridge. No unrelated Ubuntu keepalive is part of the evidence.

SYSTEM evidence is not substituted for Provider B evidence.

## 8. Frozen measured thresholds and gates

Thresholds remain unchanged:

```text
PID stress iterations: exactly 256
Program Host typed operations: >= 40
Program Host real Linux operations: >= 12
model calls between Program Host primitives: 0
false Linux process rebounds: 0
wrong Linux process mutations: 0
false Linux file rebounds: 0
wrong Linux file mutations: 0
```

Measured Run 003 must execute the frozen real-provider gates:

1. L1 Linux process identity/exact actuation, including the 256-iteration stress.
2. L2 Linux file identity/exact actuation on the Linux filesystem.
3. L3 SHELLeye kernel recovery while Provider B remains in the same native incarnation.
4. L4 actual `Ubuntu-24.04` WSL distribution termination/restart boundary.
5. L5 cross-provider hostile non-merge.
6. delta/world-sync acceptance.
7. one measured Program Host invocation meeting the frozen 40/12/0 thresholds.
8. post-L4 provider-aware operation proving normal operation after a real distro-incarnation change.

The frozen Program Host workflow exercises L1/L2/L5/delta/world-sync pressure in addition to the dedicated PID and recovery harnesses.

## 9. Measured-campaign source boundary

At this freeze:

```text
Run 001: FAILED / ENDED / PRESERVED
Run 002: FAILED / ENDED / PRESERVED
Run 003 implementation source: PUBLISHED at 6bb4806a...
Run 003 deterministic validation: PASS
Run 003 L0: PASS / FROZEN
Run 003 L1: NOT STARTED
Run 003 L2: NOT STARTED
Run 003 L3: NOT STARTED
Run 003 L4: NOT STARTED
Run 003 L5: NOT STARTED
Run 003 measured Program Host: NOT STARTED
Build 002 final results: NOT YET AUTHORIZED
```

The first measured Run 003 acceptance operation after provider verification of this freeze is the frozen L1 PID-stress campaign.

Once measured L1 begins, product source is immutable for Run 003. Any source defect ends/preserves the run and requires a new prospective repair/freeze. No result in `docs/14-BUILD-002-RESULTS.md` is authorized until the newly frozen complete measured campaign has actually finished.
