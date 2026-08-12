# SHELLeye Build 002 - Final Measured Acceptance Freeze, Run 004

**Status:** ACTIVE / OPERATIVE MEASURED FREEZE - RUN 004; FINAL ZERO-STATE L0 COMPLETE; L1-L5 NOT STARTED  
**Date:** 2026-08-12  
**Branch:** `build/build002-provider-neutrality`

The commit containing this document is the **operative measured start line** for Run 004. No measured acceptance case has executed on the final-measured runtime before this commit.

Earlier Run 004 Freeze 1, Freeze 2, and Freeze 3 are preserved as superseded/inadmissible. Freeze 3 was explicitly invalidated at `f3afe77b3a96d2b84c34c7ae9878886e0e05a6c4` because the governing execution contract required the entire prospective regression/L3/L4/post-L4 sequence to be green before a measured freeze. That complete prospective sequence subsequently passed and is provider-authoritatively recorded at `a3e710ba73eb164c926458bef00402656e92492e`.

Run 001, Run 002, and Run 003 historical failures remain preserved and are not erased or reclassified.

## 1. Frozen authority

- product implementation commit: `6bb4806a64e27b82e7e664f6ad915364fe8d99b6`
- product implementation tree: `ac54028898a4257cf04ca23dac7a373aacb69203`
- versioned L3 observer commit: `cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d`
- corrected acceptance tree: `b4f5942fbd6aa2a5e2cc82baf89d85400c6bd153`
- Program Host tree: `5411a5cd66865b024ef88d2a6232259dbe956575`
- L3 observer blob: `651f4249c7e7f6e9add3a6cbeee39d49cf021004`
- L3 observer SHA-256: `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`
- full prospective prerequisite PASS: `evidence/build002/run-004/PROSPECTIVE-FULL-PASS.md` at commit `a3e710ba73eb164c926458bef00402656e92492e`
- final compact L0 bind: `evidence/build002/run-004/FINAL-L0-BIND.json`
- staged full final-freeze evidence: `evidence/build002/run-004/FINAL-ACCEPTANCE-FREEZE.md`

No product source changed after implementation commit 6bb. The sole acceptance-instrumentation repair was the prospectively versioned L3 native-gap observer, published and fully preflighted before this measured freeze.

## 2. Required prospective campaign - complete before freeze

On one clean serialized prospective runtime, before the final measured provider was created:

- Release build: PASS / 0 warnings / 0 errors
- provider contracts: 5/5 PASS
- retained Windows Build 001 hostile core: 25/25 PASS
- all false-rebound/wrong-mutation metrics: 0
- fresh owner-context Provider B L0: PASS
- real Linux PID stress: 256/256 PASS, false process rebounds 0
- repeated Program Host pressure: 3/3 PASS, each 55 typed / 38 Linux / 0 model calls
- L3 kernel-only gap: PASS using the exact versioned observer; same Linux process survived the real gap and strong-handle file recovered exactly after kernel restart
- L4 actual Ubuntu-24.04 distribution restart: PASS; Windows BootEpoch unchanged, Linux provider-world epoch advanced, old transient process/file became destroyed, false rebounds 0, wrong mutations 0
- post-L4 full Program Host: PASS, 55 typed / 38 Linux / 0 model calls

Therefore every prerequisite enumerated by the Freeze 3 invalidation was green **before** this operative measured freeze.

## 3. Final zero-state measured runtime

After prospective PASS publication:

1. prospective kernel and exact WMI provider-lifetime anchor were retired;
2. Ubuntu-24.04 was terminated once through the supported WSL lifecycle in authenticated owner Session 1;
3. final measured state/spool/temp directories were recreated empty;
4. kernel and Linux runtimes were republished directly from clean implementation commit 6bb;
5. exactly one byte-identical final-measured apphost alias was added;
6. the final measured kernel was started under `STEALTHEYELLC\StealthEye`, Session 1;
7. fresh L0 and a second stability/freeze check were captured;
8. no measured L1-L5 operation was run before this document was committed.

Measured layout:

- pipe: `shelleye-build002-run004-final-measured`
- state root: `C:\SHELLeye\state\build002-run004-final-measured`
- DB: `C:\SHELLeye\state\build002-run004-final-measured\shelleye.db`
- kernel runtime: `C:\SHELLeye\runtime\build002-run004-final-measured\kernel\app`
- Linux runtime: `C:\SHELLeye\runtime\build002-run004-final-measured\linux\app`
- spool: `C:\SHELLeye\spool\build002-run004-final-measured`
- temp: `C:\SHELLeye\Temp\build002-run004-final-measured-runtime`

Direct-publish runtime binding:

- Windows files: 271 = 270 clean published files + one byte-identical `SHELLeye.Run004.FinalMeasured.exe` alias
- Linux files: 192
- final measured apphost SHA-256: `CCBDAF2FBCA69E77F70977C2A4DDD524D3A054A809D5FEB32771629BE8C2F2E3`
- `SHELLeye.Kernel.dll` SHA-256: `822237700CC99133E5C2376959B104D2190F33F09FAA163A82315D5677826617`
- Linux helper executable SHA-256: `33533827987DAA5A7801CC2EC88407CA941379594D313557B6845DBCB5163F61`
- Linux helper DLL SHA-256: `726E27D9016A9BBDBB0174D95CEC1AA66A3A2D4EBD14A20DC4AF4FF932D7A22C`

## 4. Fresh final owner L0

Provider-authoritative compact artifact: `evidence/build002/run-004/FINAL-L0-BIND.json`.

- raw local L0 SHA-256: `6C5DDC28C15CF89FB120983D5004C429130561C50C536664923DCBDB8FA7F6D8`
- owner: `stealtheyellc\stealtheye`, Session 1
- kernel PID: 115388
- Windows BootEpoch: `boot_1`
- kernel epoch: `kernel_1`
- PowerShell provider epoch: `provider_1`
- provider world: `world_1`, state `current`
- provider key: `wsl:{aa957c59-794f-4ad3-ae28-9188cae51ee3}`
- distribution: `Ubuntu-24.04`
- OS: Ubuntu 24.04.4 LTS
- Linux kernel: 6.18.33.2-microsoft-standard-WSL2
- Linux provider-world epoch: `linux_epoch_e670dc03eafd2b924ea53c42`
- Linux provider bridge epoch: `linux_provider_8fbc9a3f41f14f04b4d42d705cef26ae`
- Linux boot ID: `84231dbf-82a4-46a6-9adc-90f72bc3d6f5`
- PID namespace: `pid:[4026532221]`
- PID 1 start ticks: 269
- pidfd/statx/inotify/cgroup v2/systemd/exportable file handle/unique mount ID: supported
- DB quick check: `ok`
- WMI provider-lifetime anchor PID 78868: exact persisted Windows PID/start-time/executable/distro/provider-key match

Second freeze check:

- captured UTC: `2026-08-12T17:00:23.8583430+00:00`
- SHA-256: `39A00887B4CD2FC34EF3ACCEB4D767FCBF6BA16660E90E99130AD4DA8A51F29F`
- same Linux world epoch: true
- same Linux boot ID: true
- DB quick check: ok
- competing Run 004 tasks: 0; only final measured kernel task running
- standard-name `SHELLeye.Kernel.exe` processes: 0
- final-measured kernel processes: exactly 1

## 5. Frozen harnesses

- Program Host: `program-host/src/build002-acceptance.js`, SHA-256 `4CFB2CABC3FBCDB458B8D1A0EA05C56355FC3878D11F109771988F8E94DBE742`
- PID stress: `tests/acceptance/build002-linux-pid-reuse-stress.js`, SHA-256 `C99152F29831C512351864F565711D562E60F7AB82E268A9CA8968009065F669`
- recovery prepare: SHA-256 `28FA085F7CE4770D22E75ACC6BD46FADF8BDB365465678E1C315C41C04B69369`
- recovery recover: SHA-256 `47C9DB87AA95D6CA39B662756EFBFC4BD4E51A94BD07CD1C6037796FD3B1EBA0`
- distro restart: SHA-256 `6C2C152F2CD8C35D8FB3C31509ABC63320110A254A625A9F4302FDCEDBB42E5A`
- versioned L3 native-gap observer: SHA-256 `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`
- Node: v24.18.1, SHA-256 `AC51903C4C111815D52280B1FDCC8DA067CBB37E2FE1A765097B85C3292C8582`

## 6. Frozen thresholds

Thresholds remain unchanged from the governing Build 002 specification:

- exactly 256 real Linux PID stress iterations
- exactly one measured Program Host invocation / one persistent connection
- >= 40 successful typed SHELLeye operations
- >= 12 successful real Linux-provider operations
- model calls between primitives: 0
- false Linux process rebounds: 0
- wrong Linux process mutations: 0
- false Linux file rebounds: 0
- wrong Linux file mutations: 0
- L3: retain real Linux process + strong-handle file; hard-stop/restart SHELLeye kernel only; Provider B native incarnation remains live; exact process and strong-handle file recovery required
- L4: actual Ubuntu-24.04 distro termination/restart; Linux world epoch must change; old transient process identities must not rebound
- L5: Windows/WSL provider views remain qualified and non-merged
- post-L4 provider-aware operation must pass

## 7. Frozen measured order

After this commit, execute exactly:

1. L1: one 256-cycle real Linux process stress campaign.
2. exactly one Program Host invocation / one persistent connection. Its process/file/cross-provider/delta assertions also provide L2/L5 pressure.
3. L3: frozen prepare -> hard-stop **only** final measured SHELLeye kernel -> >=15-second gap -> exact versioned native-gap observer once -> restart same DB/pipe/binaries -> frozen recover.
4. L4: frozen actual Ubuntu-24.04 distro-restart harness.
5. one post-L4 provider-aware Program Host invocation.
6. evidence-driven final adjudication.

No product source, frozen harness, threshold, runtime path, state root, provider binding, or acceptance-envelope patching is permitted after this commit. Any such change ends/preserves Run 004 and requires a new run.

`docs/14-BUILD-002-RESULTS.md` and main-branch promotion remain unauthorized unless and until this exact complete measured campaign genuinely passes.