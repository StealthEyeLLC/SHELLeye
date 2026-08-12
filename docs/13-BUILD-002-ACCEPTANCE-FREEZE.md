# SHELLeye Build 002 — Acceptance Candidate Evidence Binding

Status: **PROSPECTIVE / INCOMPLETE — NO MEASURED ACCEPTANCE RUN HAS BEGUN**  
Prepared: **2026-08-12**  
Branch: `build/build002-provider-neutrality`

This document occupies the frozen Build 002 acceptance-evidence path required by `docs/11-BUILD-002-PROVIDER-NEUTRALITY-SPEC.md`, but it does **not** activate the measured acceptance campaign yet.

The implementation and every noninteractive acceptance-preparation artifact that can be bound truthfully are fixed below. The remaining blocking prerequisite is a fresh, owner-context probe from the real registered Ubuntu WSL2 provider. The currently available machine-control identity is `NT AUTHORITY\SYSTEM`; WSL rejects distribution access from that identity with `Wsl/WSL_E_LOCAL_SYSTEM_NOT_SUPPORTED`. No Linux L0-L5 result, no measured Program Host result, and no final Build 002 classification is claimed here.

## 1. Frozen implementation candidate

```text
implementation commit: ebe82459b04d22f5f933c0377abe865c934f6745
implementation tree:   bb681df3f16bdad78ca7df62b083f89c2900652a
parent commit:          85f1528986b96690948a0e6ebde01a7ed409a91d
```

GitHub publication verification established that the implementation commit is exactly one commit ahead of the frozen parent, zero commits behind, with exactly the intended 18 changed paths.

No implementation source may change after a measured acceptance campaign begins. A source repair after that boundary ends/preserves that run and requires a new prospective candidate freeze as specified in `docs/11-BUILD-002-PROVIDER-NEUTRALITY-SPEC.md`.

## 2. Frozen repository evidence

```text
Build 002 research path:   docs/10-BUILD-002-RESEARCH.md
research blob:             a7faba9d4356afcebc1a83679fbd1cb4d4c2e448
research containing commit: ba15e1b3a983c594d0d84e0a481142564e5b1a1b

Build 002 specification path: docs/11-BUILD-002-PROVIDER-NEUTRALITY-SPEC.md
specification blob:          bfc4c66d1df605b90a4c3fe15bf18c3482c12fcb
specification containing commit: ebe82459b04d22f5f933c0377abe865c934f6745

Provider matrix path: docs/12-BUILD-002-PROVIDER-MATRIX.md
provider matrix blob: f49b5e547ca139098a98ec903c05e7fab6b5c245
```

The prospective acceptance-evidence binding amendment is section 18 of the frozen specification blob above. No separate post-spec source-contract amendment is introduced by this document.

## 3. Program Host and acceptance-harness binding

The complete repository subtrees are frozen, not merely their filenames:

```text
program-host tree:       0387692ed4ff58627e09b01cb647fd0af8161589
acceptance-test tree:    0015208c988a7457c8aa667714b45da9c824f867
Linux helper source tree: 801c4e2d17ac550b00c6d53f5d1a7e41c31c021e
```

Key acceptance files inside those trees:

```text
program-host/sdk/index.js
  60cfc6c3189e5cb6cdb5201099e30accc0d740c1
program-host/src/build002-acceptance.js
  6651f9eb6a575e8659580339c7a35eb1b891d2eb
tests/acceptance/build002-linux-pid-reuse-stress.js
  10efcf1ffb09516d2bb6aaacb40f8dde7e58c12e
tests/acceptance/build002-recovery-prepare.js
  8311996d80b12bf903a5ea92a90a56e67cf810a4
tests/acceptance/build002-recovery-recover.js
  46ac684e649546039f32d0f83082f1797d25d3dc
tests/acceptance/build002-distro-restart.js
  2f72124ac759b25c8a08b9f55a857aee8618b2a6
```

The PID-reuse stress count remains exactly **256** real short-lived Linux process launch/wait iterations. The Program Host threshold remains at least **40** successful typed operations in one persistent invocation, including at least **12** successful real-Linux-provider operations.

## 4. Published-commit deterministic verification

All checks in this section were run against a clean detached checkout of the published implementation commit `ebe82459b04d22f5f933c0377abe865c934f6745` / tree `bb681df3f16bdad78ca7df62b083f89c2900652a`.

```text
Release build: PASS
warnings: 0
errors: 0

Build 002 provider-contract suite: 5 / 5 PASS

Build 001 frozen Windows hostile core: 25 / 25 PASS
false process rebounds: 0
false file rebounds: 0
false listener rebounds: 0
wrong process mutations: 0
wrong file mutations: 0
wrong-object mutations: 0

published implementation diff --check: PASS
published detached working tree after rebuild: CLEAN
```

The final frozen JavaScript set was additionally checked with `node --check` on a GitHub Actions verification branch rooted directly at the published implementation commit. GitHub Actions run `31583702395` completed successfully; the only added file on that verification branch was the verification workflow itself. The checked files were the Program Host SDK, Build 002 Program Host acceptance harness, PID-reuse stress harness, two recovery harnesses, and distro-restart harness.

This GitHub Actions verification branch is disposable verification infrastructure and is not part of the Build 002 implementation tree.

## 5. Published Linux helper binding

The Linux helper was rebuilt from the exact published implementation checkout with:

```text
configuration: Release
RID: linux-x64
self-contained: true
source tree: 801c4e2d17ac550b00c6d53f5d1a7e41c31c021e
```

The complete 192-file publish directory was copied into the frozen runtime preparation path and compared file-for-file by SHA-256 against the isolated published-commit output.

```text
runtime path:
C:\SHELLeye\runtime\linux\app\SHELLeye.Platform.Linux

helper executable size: 78256 bytes
helper executable SHA-256:
33533827987DAA5A7801CC2EC88407CA941379594D313557B6845DBCB5163F61

managed helper DLL SHA-256:
F4A631A9A3B69D2299BC9A0C2DFB188DC1D343E588A30B2C633175DBAB1614BD

runtime publish directory file count: 192
isolated publish directory file count: 192
publish-directory hash comparison: exact match
```

This binds preparation artifacts only. It is not evidence that the helper has executed inside the real Provider B distribution.

## 6. Fresh Windows and Windows-side WSL facts

Measured on 2026-08-12 from the connected Windows machine:

```text
Windows DisplayVersion: 25H2
Windows build:          26200.9168

WSL package version:    2.7.11.0
WSL-reported kernel package version: 6.18.33.2-2

registered distribution: Ubuntu-24.04
registration: {aa957c59-794f-4ad3-ae28-9188cae51ee3}
registration Version field: 2
registration BasePath:
C:\Users\StealthEye\AppData\Local\wsl\{aa957c59-794f-4ad3-ae28-9188cae51ee3}
registration DefaultUid: 0
registration Flags: 15
```

The WSL package-reported kernel version above is a fresh Windows-side package fact. It is **not** the required fresh in-distribution Linux kernel/provider probe and must not be substituted for L0 evidence.

The dormant owner registry hive was loaded only to read the registration facts and was unloaded successfully immediately afterward.

## 7. Current access boundary

Current connected execution identity:

```text
NT AUTHORITY\SYSTEM
```

At preparation time:

- no authenticated owner desktop `explorer.exe` process was present;
- no genuine owner-context execution primitive was available through the connected machine path;
- `wsl.exe --version` was readable from SYSTEM;
- distribution access/listing from SYSTEM was rejected with `Wsl/WSL_E_LOCAL_SYSTEM_NOT_SUPPORTED`.

Therefore the real Ubuntu-24.04 provider has **not** been measured by this campaign.

## 8. Runtime and state layout binding

Current preparation layout:

```text
Windows kernel runtime root: C:\SHELLeye\runtime\kernel
Windows kernel app root:     C:\SHELLeye\runtime\kernel\app
runtime descriptor:          C:\SHELLeye\runtime\kernel\runtime.json
state root:                  C:\SHELLeye\state
state database:              C:\SHELLeye\state\shelleye-dev.db
Linux helper runtime root:   C:\SHELLeye\runtime\linux\app
```

The existing `runtime.json` is historical preparation state: it references PID `9556`, and that process is not alive. No SHELLeye process was live when this evidence binding was prepared. That stale runtime descriptor is not acceptance evidence and must be replaced/reconciled by the fresh runtime used for the measured campaign.

The state root also contains the SQLite WAL/SHM companions for `shelleye-dev.db`; these are runtime state, not frozen source artifacts.

## 9. Blocking live Provider B bind

Before **any** measured L1-L5 case or measured Program Host acceptance invocation begins, this evidence record must be completed prospectively from a genuine owner-context invocation of the real registered `Ubuntu-24.04` distribution.

At minimum, L0 must freshly record from inside that distribution:

```text
distribution identity/version
Linux kernel release
machine-id/provider identity evidence
kernel boot_id
PID namespace identity
/proc/1 start-time ticks
provider-world epoch
UID/GID/groups
mount topology and root filesystem
systemd availability/version/state
cgroup version/mount
pidfd support
statx support including mount-ID form
inotify availability
exportable file-handle support on the acceptance filesystem
```

Only after those facts are captured and bound may this document's status be changed prospectively to an active measured-candidate freeze and the measured campaign begin.

## 10. Current adjudication boundary

As of this record:

```text
implementation publication: COMPLETE
published-commit deterministic verification: PASS
Windows regression: PASS
Linux helper preparation: COMPLETE
real Provider B L0 measurement: NOT RUN
L1-L5 measured acceptance: NOT RUN
measured Program Host gate: NOT RUN
docs/14-BUILD-002-RESULTS.md: MUST NOT EXIST YET
```

If no genuine owner-context Provider B measurement becomes available, the only valid final Build 002 classification remains:

```text
INCONCLUSIVE — real Provider B measurement could not be completed
```

This record does not weaken, reinterpret, or silently satisfy any frozen Build 002 gate.