# SHELLeye Build 002 — Active Measured Acceptance Freeze

Status: **ACTIVE MEASURED-CANDIDATE FREEZE — L0 COMPLETE; L1-L5 NOT YET STARTED**`r`nActivated: **2026-08-12**`r`nBranch: `build/build002-provider-neutrality`

This document prospectively freezes the Build 002 measured candidate after a genuine owner-context live Provider B bind. From the commit containing this document forward, the source is frozen for this measured campaign. Any source defect requiring repair ends/preserves the campaign and requires a new prospective candidate freeze.

## 1. Pre-campaign repair history

The earlier implementation candidate `ebe82459b04d22f5f933c0377abe865c934f6745` and evidence-only preparation commit `69f5ad6420a7fa0cbd34739ada39b0da07ec53d7` never entered a measured campaign. Once the authenticated owner session returned, the first owner-context L0 attempt reached the real WSL bridge and exposed a bootstrap defect: the bridge copied only the Linux apphost into WSL while the frozen helper was a 192-file self-contained publish directory. The helper therefore exited because `SHELLeye.Platform.Linux.dll` was absent.

That failure occurred before an L0 bind and before any L1-L5 case. It is preserved in repository history. The prospective architecture-preserving repair copies the complete frozen helper publish directory to the dedicated WSL helper directory before launch.

## 2. Frozen implementation candidate

```text
source implementation commit: 5dff520bac3fe32a1e9ff9cee4e6b34e2a85ed8a
source implementation tree:   b7e2cf155d6dbca4aaf3c179631653d563cc516e
source repair parent:          69f5ad6420a7fa0cbd34739ada39b0da07ec53d7
original Build 002 implementation: ebe82459b04d22f5f933c0377abe865c934f6745
```

The repair commit changes exactly one source file:

```text
src/SHELLeye.Kernel/LinuxWslProvider.cs
```

It changes only helper-directory bootstrap behavior; provider identity contracts and provider semantics are unchanged.

## 3. Frozen authority/evidence inputs

```text
Build 002 research blob:
a7faba9d4356afcebc1a83679fbd1cb4d4c2e448

Build 002 specification blob:
bfc4c66d1df605b90a4c3fe15bf18c3482c12fcb

Provider matrix blob:
f49b5e547ca139098a98ec903c05e7fab6b5c245

Program Host tree:
0387692ed4ff58627e09b01cb647fd0af8161589

acceptance-test tree:
0015208c988a7457c8aa667714b45da9c824f867

Linux helper source tree:
801c4e2d17ac550b00c6d53f5d1a7e41c31c021e
```

The PID-reuse stress remains exactly 256 real short-lived Linux launch/wait iterations. The Program Host gate remains one persistent invocation with at least 40 meaningful typed operations, at least 12 real Linux-provider operations, and zero model calls between primitives.

## 4. Published-candidate deterministic gates

Freshly rerun from a clean detached checkout of published commit `5dff520bac3fe32a1e9ff9cee4e6b34e2a85ed8a`:

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
```

## 5. Runtime binary/layout bind

The complete Windows kernel and Linux-helper publish directories were rebuilt from the published source commit and copied file-for-file into the frozen runtime layout.

```text
Windows kernel runtime root:
C:\SHELLeye\runtime\kernel

Windows kernel app root:
C:\SHELLeye\runtime\kernel\app

state root:
C:\SHELLeye\state

state database:
C:\SHELLeye\state\shelleye-dev.db

Linux helper runtime root:
C:\SHELLeye\runtime\linux\app

Windows kernel publish files: 546
Windows kernel executable SHA-256:
8DD0CED35AAC16E08E8C99FBC579B83D5ED9181C57E3AF803CE6A1276FCF2A33

Linux helper publish files: 192
Linux helper executable SHA-256:
33533827987DAA5A7801CC2EC88407CA941379594D313557B6845DBCB5163F61

Linux helper managed DLL SHA-256:
29BC1C10AD5BDF2587DA756EFA315AF37FA9D70878893F558713806150D3B55A
```

The live acceptance kernel was started through the existing `shelleye-kernel-dev` interactive task and verified as:

```text
owner: STEALTHEYELLC\StealthEye
Windows session: 1
kernel PID at L0 bind: 115060
pipe: shelleye-dev
Windows BootEpoch: boot_4
KernelEpoch: kernel_21
PowerShell ProviderEpoch: provider_21
```

No SYSTEM-context WSL evidence is accepted by this freeze.

## 6. Windows / WSL registration bind

```text
Windows DisplayVersion: 25H2
Windows build: 26200.9168
WSL package version: 2.7.11.0
registered distribution: Ubuntu-24.04
registration: {aa957c59-794f-4ad3-ae28-9188cae51ee3}
registration Version field: 2
provider key: wsl:{aa957c59-794f-4ad3-ae28-9188cae51ee3}
```

## 7. Authoritative owner-context L0 bind

Raw evidence is committed at:

```text
evidence/build002/l0-owner.json
```

The source evidence file was captured at `2026-08-12T13:04:54.2495369+00:00` by an interactive-token task running as:

```text
WHOAMI=stealtheyellc\stealtheye
SESSION_ID=1
```

Source evidence SHA-256 before repository copy:

```text
2F9D59C3143E0CFBB5BE6B6FDF5ED2F3F8341BE9A64CBC7AD7B75DE5D35F08DE
```

Fresh Provider B facts:

```text
world ID: world_1
provider key: wsl:{aa957c59-794f-4ad3-ae28-9188cae51ee3}
distribution: Ubuntu-24.04
OS: Ubuntu 24.04.4 LTS
Linux kernel: 6.18.33.2-microsoft-standard-WSL2
machine-id: 9bd442ff38984ccaadd4f6e3f55669f5
kernel boot_id: 15d48896-29b4-4212-97dd-d7681f661275
PID namespace: pid:[4026532223]
/proc/1 start ticks: 30039
provider-world epoch: linux_epoch_3dbd1a1a43040fe87fbd3bd2
Linux ProviderEpoch: linux_provider_ba10820c03104ef39b2e6782d59a55b2
UID/EUID/GID/EGID: 0/0/0/0
groups: 0
```

Mount/provider facts:

```text
root filesystem: /dev/sdd ext4
root mount ID: 82
root major:minor: 8:48
mount count observed by provider: 34
acceptance filesystem: /tmp on /dev/sdd ext4
```

Provider-native capability bind:

```text
pidfd: available
statx: available
statx unique mount-ID form: available
inotify: available
exportable file handle on acceptance filesystem: available
systemd: available
systemd version: systemd 255 (255.4-1ubuntu8.12)
systemd state: running
cgroup: v2
cgroup mount: /sys/fs/cgroup cgroup2
```

The kernel probe and independent raw in-distribution commands agree on the distribution, kernel, machine-id, boot ID, root filesystem, systemd/cgroup state, and Linux identity.

**L0 result: PASS.**

## 8. Measured-campaign boundary

At the instant this freeze is committed:

```text
L0: PASS / BOUND
L1: NOT RUN
L2: NOT RUN
L3: NOT RUN
L4: NOT RUN
L5: NOT RUN
measured Program Host gate: NOT RUN
docs/14-BUILD-002-RESULTS.md: MUST NOT EXIST YET
```

The first measured case after this freeze is L1. Source is frozen from this point forward.
