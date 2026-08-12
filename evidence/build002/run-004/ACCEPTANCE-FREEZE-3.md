# SHELLeye Build 002 - Measured Acceptance Freeze, Run 004 / Freeze 3

**Status:** ACTIVE MEASURED-CANDIDATE FREEZE - RUN 004; FRESH ZERO-STATE L0 COMPLETE; L1-L5 NOT STARTED

Date: 2026-08-12
Branch: `build/build002-provider-neutrality`

This is the operative Run 004 freeze. Freeze 1 (`ea3be21f8bd14ee4780935856ce01003f862e7ea`) and Freeze 2 (`25eac2cd06608ef3956922e77b5c59662b72f1b4`) are prospectively superseded because Provider B/state were reset again before any admissible measured operation. The 16:40 PID-stress executions remain PRE-FREEZE / EXCLUDED / NOT COUNTED under `PRE-FREEZE-EXCLUSION.md`.

## Frozen authority

- implementation commit: `6bb4806a64e27b82e7e664f6ad915364fe8d99b6`
- implementation tree: `ac54028898a4257cf04ca23dac7a373aacb69203`
- execution lease: `20eee14807bbe2d11bd959682813a97c2f763571`
- corrected observer commit: `cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d`
- corrected observer blob: `651f4249c7e7f6e9add3a6cbeee39d49cf021004`
- corrected observer SHA-256: `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`
- corrected acceptance tree: `b4f5942fbd6aa2a5e2cc82baf89d85400c6bd153`
- Program Host tree: `5411a5cd66865b024ef88d2a6232259dbe956575`

No product source changed after implementation `6bb...`. The only acceptance-harness repair was the prospectively versioned L3 observer.

## Zero-state boundary

Immediately before this freeze:

- all Run 004 kernels/anchors were stopped;
- `Ubuntu-24.04` was terminated through the supported WSL lifecycle;
- `C:\SHELLeye\state\build002-run004` was recreated empty;
- the same published runtime was started under `STEALTHEYELLC\StealthEye` Session 1;
- no admissible Run 004 measured L1-L5 operation had executed in this new state/provider incarnation.

No further provider reset is permitted after this freeze except the frozen L4 distro-restart gate itself.

## Fresh L0 bound by this freeze

Raw: `evidence/build002/run-004/L0-BIND-3-RAW.json`
Freeze check: `evidence/build002/run-004/FREEZE-CHECK-3.json`
Runtime bind: `evidence/build002/run-004/RUNTIME-BIND-3.json`

- L0 raw SHA-256: `346172438F33028D98ACC3D99D1F0E68E4CD8CA4D3F6AD3CB359DF83CA23E45A`
- freeze-check SHA-256: `B3E0C318EE1C15F0EE4D4BCF9A2C2E3C423AE5D9A3D21AEFB8DD137E88A99CBA`
- owner: `stealtheyellc\stealtheye`, Session 1
- kernel PID: `21480`, kernel epoch `kernel_1`, provider epoch `provider_1`
- Windows BootEpoch: `boot_1`
- Linux world: `world_1`
- Linux world epoch: `linux_epoch_a4ea67a87225c14cba568059`
- Linux boot ID: `c435be7f-b55b-466e-b62c-71953535ee87`
- PID namespace: `pid:[4026532221]`
- PID 1 start ticks: `96`
- Ubuntu 24.04.4 LTS / Linux 6.18.33.2-microsoft-standard-WSL2
- pidfd/statx/inotify/cgroup-v2/systemd/exportable-handle/unique-mount-ID: supported
- WMI provider-lifetime anchor PID `113128`: owner exact / start-time exact / parent `WmiPrvSE`
- exactly two Ubuntu WSL clients: provider-lifetime anchor + kernel provider bridge
- freeze check: world epoch match true / boot ID match true / DB quick check ok

## Frozen runtime

- pipe: `shelleye-build002-run004-measured`
- DB: `C:\SHELLeye\state\build002-run004\shelleye.db`
- kernel: `C:\SHELLeye\runtime\build002-run004\kernel\app`
- Linux helper: `C:\SHELLeye\runtime\build002-run004\linux\app`
- `SHELLeye.Run004.Measured.exe`: `CCBDAF2FBCA69E77F70977C2A4DDD524D3A054A809D5FEB32771629BE8C2F2E3`
- `SHELLeye.Kernel.dll`: `822237700CC99133E5C2376959B104D2190F33F09FAA163A82315D5677826617`
- `SHELLeye.Platform.Linux`: `33533827987DAA5A7801CC2EC88407CA941379594D313557B6845DBCB5163F61`
- `SHELLeye.Platform.Linux.dll`: `726E27D9016A9BBDBB0174D95CEC1AA66A3A2D4EBD14A20DC4AF4FF932D7A22C`

## Frozen measured procedure

1. L1: exactly 256 real Linux launch/wait iterations, one measured invocation only.
2. Program Host: exactly one measured invocation / one connection, >=40 typed operations, >=12 real Linux operations, 0 model calls; this also pressures L2, L5, delta/world-sync.
3. L3: frozen recovery prepare -> hard-stop only SHELLeye kernel -> wait 15 seconds -> run `tests/acceptance/build002-l3-native-gap.ps1` exactly once -> if observer succeeds, immediately restart same DB/pipe -> frozen recovery recover. No ad hoc observer debugging is allowed inside the gate. Observer failure ends/preserves Run 004.
4. L4: actual `Ubuntu-24.04` distro termination/restart through the frozen harness.
5. Post-L4 provider-aware Program Host: one frozen invocation to prove normal operation after the new distro incarnation.

Thresholds remain exactly those in the Build 002 spec. False rebounds and wrong-object mutations must remain zero. No source, harness, threshold, runtime, state-root, or provider-binding patching is permitted after this freeze.

`docs/14-BUILD-002-RESULTS.md` remains unauthorized until the complete Run 004 campaign genuinely passes.
