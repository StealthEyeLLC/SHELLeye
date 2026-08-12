# SHELLeye Build 002 - Run 004 Final Measured Summary

**Final frozen classification:** **PASS - provider-neutral spine survived with Windows depth preserved**  
**Date:** 2026-08-12  
**Operative measured freeze:** `05545238e7a23091e86ae72174a4c0d329a1cf6d`  
**Product implementation:** `6bb4806a64e27b82e7e664f6ad915364fe8d99b6`  
**Versioned L3 observer:** `cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d`

Run 004 is the first admissible complete measured Build 002 campaign. Earlier Run 001/002/003 failures and earlier Run 004 freeze attempts remain preserved as historical evidence and are not rewritten as passes.

## 1. Frozen candidate

The final measured campaign used the unchanged product implementation at 6bb and the prospectively versioned L3 observation harness at cd7dd98.

Frozen product/runtime identity:

- implementation tree: `ac54028898a4257cf04ca23dac7a373aacb69203`
- acceptance tree: `b4f5942fbd6aa2a5e2cc82baf89d85400c6bd153`
- Program Host tree: `5411a5cd66865b024ef88d2a6232259dbe956575`
- final-measured apphost SHA-256: `CCBDAF2FBCA69E77F70977C2A4DDD524D3A054A809D5FEB32771629BE8C2F2E3`
- `SHELLeye.Kernel.dll`: `822237700CC99133E5C2376959B104D2190F33F09FAA163A82315D5677826617`
- Linux helper: `33533827987DAA5A7801CC2EC88407CA941379594D313557B6845DBCB5163F61`
- Linux helper DLL: `726E27D9016A9BBDBB0174D95CEC1AA66A3A2D4EBD14A20DC4AF4FF932D7A22C`
- L3 native-gap observer SHA-256: `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`

The entire prerequisite regression/L3/L4/post-L4 sequence passed prospectively before the operative freeze. Provider-authoritative proof: `PROSPECTIVE-FULL-PASS.md` at commit `a3e710ba73eb164c926458bef00402656e92492e`.

## 2. Fresh final L0 - PASS

Final owner-context Provider B bind:

- owner: `stealtheyellc\stealtheye`, Session 1
- final initial kernel PID: 115388 / kernel epoch `kernel_1`
- Windows BootEpoch: `boot_1`
- Linux world: `world_1`
- Linux world epoch: `linux_epoch_e670dc03eafd2b924ea53c42`
- Linux provider epoch: `linux_provider_8fbc9a3f41f14f04b4d42d705cef26ae`
- Ubuntu 24.04.4 LTS
- Linux kernel 6.18.33.2-microsoft-standard-WSL2
- Linux boot ID `84231dbf-82a4-46a6-9adc-90f72bc3d6f5`
- PID namespace `pid:[4026532221]`
- PID 1 start ticks 269
- pidfd/statx/inotify/cgroup-v2/systemd/exportable-file-handle/unique-mount-ID: supported
- DB quick-check: `ok`
- exact WMI provider-lifetime anchor at freeze: PID 78868
- raw L0 SHA-256: `6C5DDC28C15CF89FB120983D5004C429130561C50C536664923DCBDB8FA7F6D8`
- second freeze check SHA-256: `39A00887B4CD2FC34EF3ACCEB4D767FCBF6BA16660E90E99130AD4DA8A51F29F`

GitHub compact bind: `FINAL-L0-BIND.json`.

## 3. L1 - Linux process identity / exactness - PASS

Provider-authoritative evidence: `MEASURED-L1.json`, commit `8264683e42f2041eff1fd3c2fdc22cce1de5c34b`.

- exactly 256 real Linux launch/wait iterations
- passed: 256/256
- false Linux process rebounds: 0
- wrong rebound: 0
- owner context: Session 1
- kernel remained PID 115388 / epoch 1

Observed native PID reuse was 0 in this finite campaign; deterministic start-time/epoch rejection remained covered by the passing provider-contract suite.

## 4. One-shot measured Program Host / L2 / L5 / delta pressure - PASS

Provider-authoritative evidence: `MEASURED-PROGRAM-HOST.json`, commit `eb326087a3cc34cd02d5c6963747078e37ee6846`.

Exactly one measured pre-L4 Program Host invocation used one persistent connection and produced:

- typed SHELLeye operations: 55
- real Linux-provider operations: 38
- model calls between primitives: 0
- persistent connection: true
- process exec preserved same exact concept: true
- simultaneous same-executable processes remained distinct: true
- stale process actuation rejected: true
- Linux file rename preserved physical concept: true
- hardlink observed as same physical concept: true
- unlink/recreate produced a new concept: true
- old exact-write did not touch the replacement: true
- Windows/WSL provider identities merged: false
- structured PowerShell: true
- database quick-check: `ok`
- world.sync completed; measured stale count 0 in this invocation
- bounded delta count: 32

Under the frozen procedure this invocation supplied the measured L2 file-identity/exact-actuation pressure, L5 cross-provider non-merge pressure, and delta/world-sync pressure in addition to satisfying the Program Host threshold.

## 5. L3 - SHELLeye kernel-only death/recovery with Provider B live - PASS

Provider-authoritative evidence: `MEASURED-L3.json`, commit `2eed774b91907bccd7e82b0fd3ed835d29a56a25`.

Frozen prepare created:

- process concept `proc_266`
- Linux PID 3486
- start ticks 30561
- executable after exec: `/usr/bin/sleep`
- file concept `file_5`
- ext4 inode 51418
- mount ID 2147483732
- frozen strong exported-file-handle witness: true
- Linux world epoch `linux_epoch_e670dc03eafd2b924ea53c42`

Kernel-only gap:

- hard-stopped only final-measured kernel PID 115388
- after >=15 seconds the old kernel was absent and no replacement kernel was live
- exact provider-lifetime anchor remained alive with exact Windows start time
- versioned native-gap observer executed exactly once
- observer PASS after ~49.35 seconds from prepare
- same Linux boot ID, PID namespace, and PID 1 start ticks
- PID 3486 remained exact with start ticks 30561
- executable remained `/usr/bin/sleep`
- retained file remained present

Restart/recovery:

- new kernel PID 27444
- kernel epoch `kernel_2`
- same Windows BootEpoch `boot_1`
- same DB/pipe/binaries
- same exact WMI provider-lifetime anchor
- frozen recovery harness PASS
- same Linux world epoch: true
- process exact: true
- strong-handle file outcome: `current`
- file exact: true

This is the gate whose wrapper failed in Run 003. Under the prospectively corrected, frozen observer it passed without product-source repair inside the measured run.

## 6. L4 - actual Ubuntu-24.04 distribution restart - PASS

Provider-authoritative first measured L4 evidence: `MEASURED-L4.json`, commit `3ce5ef1397a024b8b280e5b452904bb9e280a9cb`.

- Windows BootEpoch stayed `boot_1`
- Linux provider-world epoch changed from `linux_epoch_e670dc03eafd2b924ea53c42` to `linux_epoch_a4789dbf717453db9c20b8c8`
- old retained Linux process state: `destroyed`
- old retained Linux file state: `destroyed`
- strong-handle witness had been true before the boundary; no false continuity was invented through the new provider incarnation
- false rebounds: 0
- wrong object mutations: 0
- SHELLeye kernel remained PID 27444 / kernel epoch 2

## 7. Terminal post-L4 provider-aware operation - PASS

Provider-authoritative terminal evidence: `MEASURED-POST-L4.json`, commit `20f52addb838a8b4b4a938bc483238ad6b203ac7`.

The required frozen post-L4 Program Host task completed with Task Scheduler result 0. Its completion metadata file was created at `2026-08-12T17:08:32.0647498Z`, proving completion before any later duplicate destructive action.

Controller-observed terminal result:

- PASS
- one persistent connection
- 55 typed operations
- 38 Linux-provider operations
- 0 model calls between primitives
- provider world `world_1`, state `current`
- process exactness assertions passed
- file recreate/new-concept assertion passed
- cross-provider identities merged: false
- DB quick-check: `ok`
- world.sync stale rows: 1, reconciled successfully

This was the terminal required runtime gate under the frozen measured procedure.

## 8. Post-campaign contamination - preserved / not counted

After the frozen terminal gate had already completed, another authority-aware task path ran a second L4 and second post-L4 Program Host pair against the same final runtime. The duplicate L4 began at 13:08:43 -04:00; the frozen terminal gate had already completed by approximately 13:08:32 -04:00.

The later activity returned exit 0 and remained conservative, but it is **POST-CAMPAIGN / NOT COUNTED**. It overwrote some local convenience copies but did not alter the already-published first L4 evidence, frozen source/harnesses, or any still-pending acceptance operation because none remained.

Full record: `POST-CAMPAIGN-CONTAMINATION.md`, commit `cd727403c2ec991aa8f1c7c5e4bcff7180207f99`.

## 9. Windows-depth regression preservation - PASS

Before the measured freeze, the final candidate passed:

- Release build: 0 warnings / 0 errors
- provider contracts: 5/5
- retained Build 001 Windows hostile core: 25/25
- false process rebounds: 0
- false file rebounds: 0
- false listener rebounds: 0
- wrong process mutations: 0
- wrong file mutations: 0
- wrong object mutations: 0

No measured Run 004 evidence required weakening a Build 001 Windows identity or actuation rule.

## 10. Final classification

Per the classification vocabulary frozen in `docs/11-BUILD-002-PROVIDER-NEUTRALITY-SPEC.md`:

> **PASS - provider-neutral spine survived with Windows depth preserved**

The common SHELLeye ontology survived materially different Provider B semantics without collapsing Windows and Linux identity, without false transient rebound across Linux provider-world epochs, without weakening exact actuation, and without losing the compact persistent Program Host model.

Build 002 is therefore **COMPLETE / MEASURED / PASSED** on the development branch. Main-branch promotion may proceed only as a non-force fast-forward after this result and `docs/14-BUILD-002-RESULTS.md` are present in branch authority.