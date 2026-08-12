# SHELLeye Build 002 - Measured Acceptance Freeze, Run 002

Status: **ACTIVE MEASURED-CANDIDATE FREEZE - RUN 002; L0 COMPLETE; L1-L5 NOT YET STARTED**

Date: 2026-08-12
Branch: `build/build002-provider-neutrality`

This freeze begins a new measured acceptance campaign after preserving Run 001 as failed/ended. Source is frozen from the commit containing this document. Any source repair after the first Run 002 measured case ends/preserves Run 002 and requires another prospective freeze.

## 1. Preserved Run 001

Run 001 remains authoritative history and is not erased:

```text
Run 001 freeze commit: dfb463d5426335e876a8f8bbac45e88da3a769c9
Run 001 preserved failure commit: 03df938096f378f0673546505b57da39af1908c4
Run 001 evidence: evidence/build002/run-001/
```

Run 001's 256-iteration real Linux PID stress passed with zero wrong rebound. Its required Program Host invocation failed because the embedded PowerShell host could see the `Microsoft.PowerShell.Management` command but could not discover the bundled built-in module path. The failure is preserved as a genuine measured gate failure.

## 2. Prospective Run 002 repair

The bounded repair is:

```text
implementation commit: 2d81186040268c101497555f5a9425d0eba18ce2
implementation tree:   dab97caf712505457ee7d23481fd2ba06fe5dfa2
parent:                03df938096f378f0673546505b57da39af1908c4
changed source:         src/SHELLeye.Kernel/StructuredPowerShell.cs
```

The repair prepends SHELLeye's already-published bundled Windows PowerShell module directory:

```text
runtimes\win\lib\net10.0\Modules
```

to `PSModulePath` before `InitialSessionState.CreateDefault2()` opens the embedded runspace. No provider ontology, Linux identity contract, Windows identity contract, or Program Host threshold changed.

Pre-freeze owner-context smoke against this repair proved:

```text
structured Get-Process: PASS
PowerShell provider: Microsoft.PowerShell.SDK
structured: true
object count: 1
PowerShell errors: 0
real Linux provider state: current
pidfd: true
statx: true
```

## 3. Frozen authority and harnesses

```text
research: docs/10-BUILD-002-RESEARCH.md
specification: docs/11-BUILD-002-PROVIDER-NEUTRALITY-SPEC.md
provider matrix: docs/12-BUILD-002-PROVIDER-MATRIX.md
Program Host tree: 0387692ed4ff58627e09b01cb647fd0af8161589
acceptance-test tree: 0015208c988a7457c8aa667714b45da9c824f867
Linux helper source tree: 801c4e2d17ac550b00c6d53f5d1a7e41c31c021e
```

Frozen measured thresholds remain unchanged:

```text
PID stress iterations: 256
Program Host typed operations: >= 40
Program Host real Linux operations: >= 12
model calls between Program Host primitives: 0
false Linux process rebounds: 0
wrong Linux process mutations: 0
false Linux file rebounds: 0
wrong Linux file mutations: 0
```

## 4. Published-commit deterministic validation

All validation below was rerun from a clean detached checkout fetched from GitHub at implementation commit `2d81186040268c101497555f5a9425d0eba18ce2`.

```text
Release build: PASS
warnings: 0
errors: 0
provider-contract tests: 5 / 5 PASS
Build 001 Windows hostile core: 25 / 25 PASS
false process rebounds: 0
false file rebounds: 0
false listener rebounds: 0
wrong process mutations: 0
wrong file mutations: 0
wrong object mutations: 0
```

## 5. Runtime binding

The exact published-commit runtime was republished and copied file-for-file into the measured runtime paths.

```text
Windows kernel runtime:
C:\SHELLeye\runtime\kernel\app\SHELLeye.Kernel.exe
SHA-256: 5B3306D147F573BAC691AD17899CB5DFD495B78E531C09AE2CF329C9D232561A
publish files: 546

Linux helper runtime:
C:\SHELLeye\runtime\linux\app\SHELLeye.Platform.Linux
SHA-256: 33533827987DAA5A7801CC2EC88407CA941379594D313557B6845DBCB5163F61
managed helper DLL SHA-256: B8ABDFCE9D4FBBDAF1BAF4509F5D223DD02D73D9A46E6753099BDC6B7DDA4FB9
publish files: 192
```

Campaign runtime/state layout:

```text
pipe: shelleye-dev
state root: C:\SHELLeye\state\build002-run002
state DB: C:\SHELLeye\state\build002-run002\shelleye.db
kernel runtime root: C:\SHELLeye\runtime\kernel
spool root: C:\SHELLeye\spool\build002-run002
temp root: C:\SHELLeye\Temp\build002-run002-runtime
Linux helper runtime root: C:\SHELLeye\runtime\linux\app
```

The measured kernel is running under the authenticated owner token `STEALTHEYELLC\StealthEye`, Session 1. SYSTEM evidence is not substituted for Provider B evidence.

## 6. Run 002 fresh L0 bind

Raw evidence:

```text
evidence/build002/run-002/l0-owner.json
SHA-256: 43D57CF87F02E159D433DF859E107D903A2762BF57A1F386D55A3A5225304709
captured UTC: 2026-08-12T13:33:43.9168999+00:00
owner context: stealtheyellc\stealtheye
Windows session: 1
kernel PID at bind: 40372
```

Provider B:

```text
world ID: world_1
provider key: wsl:{aa957c59-794f-4ad3-ae28-9188cae51ee3}
distribution: Ubuntu-24.04
OS: Ubuntu 24.04.4 LTS
Linux kernel: 6.18.33.2-microsoft-standard-WSL2
machine-id: 9bd442ff38984ccaadd4f6e3f55669f5
kernel boot_id: b94477df-6dcf-45b1-b9e8-89dbe3e5b312
PID namespace: pid:[4026532221]
/proc/1 start ticks: 89
provider-world epoch: linux_epoch_02295e9237d7452f660ab13e
provider process epoch: linux_provider_a9af298edbe14f5581927f08b9a7856a
root filesystem: ext4 on /dev/sdd
systemd: available / running
cgroup: v2
pidfd: supported
statx: supported
unique mount ID: supported
inotify: supported
exportable file handle: supported on acceptance filesystem
```

Run 002 SHELLeye epochs at L0:

```text
Windows/SHELLeye BootEpoch: boot_1
kernel epoch: kernel_1
PowerShell provider epoch: provider_1
```

## 7. Measured campaign boundary

At this commit:

```text
Run 001: FAILED / ENDED / PRESERVED
Run 002 implementation repair: PUBLISHED
Run 002 deterministic preflight: PASS
Run 002 L0: PASS
Run 002 L1: NOT STARTED
Run 002 L2: NOT STARTED
Run 002 L3: NOT STARTED
Run 002 L4: NOT STARTED
Run 002 L5: NOT STARTED
Run 002 measured Program Host gate: NOT STARTED
final Build 002 results: NOT YET AUTHORIZED
```

The next measured operation after this freeze is the Run 002 L1 real Linux process identity campaign. No source patching is permitted inside Run 002.