# SHELLeye Build 002 - Measured Acceptance Freeze, Run 004

**Status:** ACTIVE MEASURED-CANDIDATE FREEZE - RUN 004; L0 COMPLETE; L1-L5 NOT YET STARTED  
**Date:** 2026-08-12  
**Branch:** `build/build002-provider-neutrality`

Run 003 remains **FAILED / ENDED** for execution integrity at `308c751ed37d557104596d72ad9c08696d4a7e65`. It is not reopened. Run 004 is a full new measured campaign.

## 1. Frozen authority

- product implementation commit: `6bb4806a64e27b82e7e664f6ad915364fe8d99b6`
- product implementation tree: `ac54028898a4257cf04ca23dac7a373aacb69203`
- Run 004 execution lease: `20eee14807bbe2d11bd959682813a97c2f763571`
- corrected L3 observer commit: `cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d`
- corrected acceptance tree: `b4f5942fbd6aa2a5e2cc82baf89d85400c6bd153`
- Program Host tree: `5411a5cd66865b024ef88d2a6232259dbe956575`
- corrected L3 observer blob: `651f4249c7e7f6e9add3a6cbeee39d49cf021004`
- corrected L3 observer SHA-256: `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`

The observer change is execution instrumentation only. No product source under `src/` changed from implementation commit 6bb.

## 2. Corrected observer pre-freeze proof

`tests/acceptance/build002-l3-native-gap.ps1` was prospectively versioned before measurement. It uses an explicit `ArgumentList` parameter rather than PowerShell's reserved `$args` variable. An owner-context prospective smoke passed with:

- provider observable: true
- process exact: true
- file present: true
- owner: `stealtheyellc\\stealtheye`
- session: 1
- boot ID: `b914245d-a321-475d-858c-c8139d233a6c`
- PID namespace: `pid:[4026532221]`

## 3. Frozen harnesses

- `program-host/src/build002-acceptance.js` SHA-256 `4CFB2CABC3FBCDB458B8D1A0EA05C56355FC3878D11F109771988F8E94DBE742`
- `tests/acceptance/build002-linux-pid-reuse-stress.js` SHA-256 `C99152F29831C512351864F565711D562E60F7AB82E268A9CA8968009065F669`
- `tests/acceptance/build002-recovery-prepare.js` SHA-256 `28FA085F7CE4770D22E75ACC6BD46FADF8BDB365465678E1C315C41C04B69369`
- `tests/acceptance/build002-recovery-recover.js` SHA-256 `47C9DB87AA95D6CA39B662756EFBFC4BD4E51A94BD07CD1C6037796FD3B1EBA0`
- `tests/acceptance/build002-distro-restart.js` SHA-256 `6C2C152F2CD8C35D8FB3C31509ABC63320110A254A625A9F4302FDCEDBB42E5A`
- `tests/acceptance/build002-l3-native-gap.ps1` SHA-256 `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`

Node runtime: `v24.18.1`, SHA-256 `AC51903C4C111815D52280B1FDCC8DA067CBB37E2FE1A765097B85C3292C8582`.

## 4. Frozen runtime

Runtime/state layout:

- pipe: `shelleye-build002-run004-measured`
- state root: `C:\\SHELLeye\\state\\build002-run004`
- DB: `C:\\SHELLeye\\state\\build002-run004\\shelleye.db`
- kernel runtime: `C:\\SHELLeye\\runtime\\build002-run004\\kernel\\app`
- Linux helper runtime: `C:\\SHELLeye\\runtime\\build002-run004\\linux\\app`
- spool: `C:\\SHELLeye\\spool\\build002-run004`
- temp: `C:\\SHELLeye\\Temp\\build002-run004-runtime`

Self-contained published runtime:

- `SHELLeye.Run004.Measured.exe` SHA-256 `CCBDAF2FBCA69E77F70977C2A4DDD524D3A054A809D5FEB32771629BE8C2F2E3`
- `SHELLeye.Kernel.dll` SHA-256 `822237700CC99133E5C2376959B104D2190F33F09FAA163A82315D5677826617`
- `SHELLeye.Platform.Linux` SHA-256 `33533827987DAA5A7801CC2EC88407CA941379594D313557B6845DBCB5163F61`
- `SHELLeye.Platform.Linux.dll` SHA-256 `726E27D9016A9BBDBB0174D95CEC1AA66A3A2D4EBD14A20DC4AF4FF932D7A22C`
- Windows publish files: 271
- Linux publish files: 192

## 5. Deterministic validation from provider-published observer commit

Clean detached checkout at `cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d`:

- Release build: PASS / 0 warnings / 0 errors
- provider-contract tests: 5 / 5 PASS
- Build 001 Windows hostile core: 25 / 25 PASS
- false process rebounds: 0
- false file rebounds: 0
- false listener rebounds: 0
- wrong process mutations: 0
- wrong file mutations: 0
- wrong object mutations: 0

## 6. Fresh Run 004 L0

Provider-authoritative compact bind: `evidence/build002/run-004/L0-BIND.json`.

Raw L0 SHA-256: `4342C38399A705564B56B95110685B35F672C0E5EBFC4070819D4ECE75F4D3A5`  
Freeze-check SHA-256: `9CCEEA765BD94A482757FC49BEF90C2DB35D35EA080DEBD30B67CCC6FF8ED469`

Fresh provider facts:

- owner: `stealtheyellc\\stealtheye`, Session 1
- kernel PID at L0: 33828, kernel epoch `kernel_1`
- world: `world_1`, state `current`
- world epoch: `linux_epoch_dac3519303157c3d1e080a77`
- Linux provider epoch: `linux_provider_2f81a46ecc634c57905ed477e29fac44`
- Ubuntu 24.04.4 LTS
- Linux 6.18.33.2-microsoft-standard-WSL2
- machine-id `9bd442ff38984ccaadd4f6e3f55669f5`
- boot ID `b914245d-a321-475d-858c-c8139d233a6c`
- PID namespace `pid:[4026532221]`
- PID 1 start ticks 293
- ext4 root on `/dev/sdd`
- pidfd/statx/inotify/cgroup v2/systemd/exportable file handle/unique mount ID: all supported
- DB quick check: ok
- WMI anchor PID 121748: exact start-time/executable match at freeze check
- legacy Run 003 / old Build 002 tasks running: 0
- standard-name `SHELLeye.Kernel.exe` processes running: 0

## 7. Frozen measured thresholds

These remain unchanged from the governing Build 002 specification:

- real Linux PID stress: exactly 256 iterations
- Program Host: exactly one invocation / one persistent connection
- successful typed SHELLeye operations: >= 40
- successful real Linux-provider operations: >= 12
- model calls between primitives: 0
- false Linux process rebounds: 0
- wrong Linux process mutations: 0
- false Linux file rebounds: 0
- wrong Linux file mutations: 0
- L3: retain live Linux process + strong-handle file; hard-stop/restart SHELLeye kernel only; Provider B remains same native incarnation; exact process/file recovery required
- L4: actual selected WSL distribution termination/restart boundary; old transient process identities must not rebound
- L5: Windows/WSL provider views must remain provider-qualified and non-merged

## 8. Run boundary

At this freeze:

- Run 003: FAILED / ENDED / preserved
- Run 004 implementation: unchanged at 6bb
- Run 004 observer repair: published prospectively
- Run 004 deterministic preflight: PASS
- Run 004 L0: PASS
- Run 004 L1: NOT STARTED
- Run 004 L2: NOT STARTED
- Run 004 L3: NOT STARTED
- Run 004 L4: NOT STARTED
- Run 004 L5: NOT STARTED
- Run 004 measured Program Host: NOT STARTED

No source or harness patching is permitted after this freeze. A defect requiring either ends/preserves Run 004 and requires a new prospective run.