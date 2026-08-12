# SHELLeye Build 002 - Run 004 Final Measured Acceptance Freeze

**Status:** PROSPECTIVE FINAL FREEZE EVIDENCE STAGED / MEASURED CASES NOT STARTED  
**Date:** 2026-08-12

The operative measured start line is the subsequent commit that updates `docs/13-BUILD-002-ACCEPTANCE-FREEZE.md` to this final Run 004 state. No measured case has executed on the final-measured runtime before that commit.

## Authority

- product implementation commit: `6bb4806a64e27b82e7e664f6ad915364fe8d99b6`
- product implementation tree: `ac54028898a4257cf04ca23dac7a373aacb69203`
- corrected observer commit: `cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d`
- corrected acceptance tree: `b4f5942fbd6aa2a5e2cc82baf89d85400c6bd153`
- Program Host tree: `5411a5cd66865b024ef88d2a6232259dbe956575`
- L3 observer blob: `651f4249c7e7f6e9add3a6cbeee39d49cf021004`
- L3 observer SHA-256: `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`
- full prospective prerequisite PASS commit: `a3e710ba73eb164c926458bef00402656e92492e`
- final L0 bind commit: `9dbeb3a85086dd22191a7f693dcbe1d56d0fd1f4`

Run 001, Run 002, and Run 003 historical failures remain preserved. Run 004 Freeze 1/2/3 are preserved as superseded/inadmissible. Their results are not counted.

## Prospective prerequisites before this freeze

All prerequisites imposed by `f3afe77b3a96d2b84c34c7ae9878886e0e05a6c4` passed on the unchanged candidate before this final measured bind:

- Release build: PASS / 0 warnings / 0 errors
- provider contracts: 5/5 PASS
- Windows Build 001 hostile core: 25/25 PASS with all false/wrong metrics 0
- owner-context prospective L0: PASS
- real Linux PID stress: 256/256 PASS, false rebound 0
- repeated Program Host pressure: 3/3 PASS, each 55 typed / 38 Linux / 0 model calls
- L3 real kernel gap with versioned observer: PASS; exact process + strong-handle file recovery
- L4 actual Ubuntu distro restart: PASS; Linux world epoch advanced; old transient process/file destroyed; false rebound 0
- post-L4 Program Host: PASS 55/38/0

Provider-authoritative detail: `evidence/build002/run-004/PROSPECTIVE-FULL-PASS.md`.

## Final zero-state measured bind

After the full prospective pass, the prospective kernel and its exact WMI anchor were retired. Ubuntu-24.04 was terminated once in authenticated owner Session 1. The final runtime was republished directly from clean implementation 6bb and all measured state/spool/temp directories were recreated empty before kernel launch.

Final measured layout:

- pipe: `shelleye-build002-run004-final-measured`
- state root: `C:\\SHELLeye\\state\\build002-run004-final-measured`
- DB: `C:\\SHELLeye\\state\\build002-run004-final-measured\\shelleye.db`
- kernel runtime: `C:\\SHELLeye\\runtime\\build002-run004-final-measured\\kernel\\app`
- Linux runtime: `C:\\SHELLeye\\runtime\\build002-run004-final-measured\\linux\\app`
- spool: `C:\\SHELLeye\\spool\\build002-run004-final-measured`
- temp: `C:\\SHELLeye\\Temp\\build002-run004-final-measured-runtime`

Clean direct-publish runtime:

- Windows publish files: 271 (270 published files + one byte-identical `SHELLeye.Run004.FinalMeasured.exe` alias)
- Linux publish files: 192
- final-measured apphost SHA-256 `CCBDAF2FBCA69E77F70977C2A4DDD524D3A054A809D5FEB32771629BE8C2F2E3`
- `SHELLeye.Kernel.dll` SHA-256 `822237700CC99133E5C2376959B104D2190F33F09FAA163A82315D5677826617`
- Linux helper executable SHA-256 `33533827987DAA5A7801CC2EC88407CA941379594D313557B6845DBCB5163F61`
- Linux helper DLL SHA-256 `726E27D9016A9BBDBB0174D95CEC1AA66A3A2D4EBD14A20DC4AF4FF932D7A22C`

Fresh final owner L0:

- raw bind: `evidence/build002/run-004/FINAL-L0-BIND.json`
- raw local L0 SHA-256 `6C5DDC28C15CF89FB120983D5004C429130561C50C536664923DCBDB8FA7F6D8`
- owner `stealtheyellc\\stealtheye`, Session 1
- kernel PID 115388 / kernel epoch `kernel_1`
- PowerShell provider epoch `provider_1`
- world `world_1`, state current
- Linux world epoch `linux_epoch_e670dc03eafd2b924ea53c42`
- Linux provider epoch `linux_provider_8fbc9a3f41f14f04b4d42d705cef26ae`
- Ubuntu 24.04.4 LTS
- Linux 6.18.33.2-microsoft-standard-WSL2
- boot ID `84231dbf-82a4-46a6-9adc-90f72bc3d6f5`
- PID namespace `pid:[4026532221]`
- PID 1 start ticks 269
- pidfd/statx/inotify/cgroup v2/systemd/exportable file handle/unique mount ID: all supported
- WMI anchor PID 78868: exact persisted start-time/executable/distro/provider-key match
- DB quick check: `ok`

Second freeze check:

- captured `2026-08-12T17:00:23.8583430+00:00`
- SHA-256 `39A00887B4CD2FC34EF3ACCEB4D767FCBF6BA16660E90E99130AD4DA8A51F29F`
- same Linux world epoch: true
- same boot ID: true
- DB quick check: ok
- competing Run 004 tasks: none; only final measured kernel task running
- standard-name `SHELLeye.Kernel.exe` processes: 0
- final-measured kernel processes: exactly 1

## Frozen harnesses and thresholds

- Program Host: `program-host/src/build002-acceptance.js`, SHA-256 `4CFB2CABC3FBCDB458B8D1A0EA05C56355FC3878D11F109771988F8E94DBE742`
- PID stress: `tests/acceptance/build002-linux-pid-reuse-stress.js`, SHA-256 `C99152F29831C512351864F565711D562E60F7AB82E268A9CA8968009065F669`
- recovery prepare SHA-256 `28FA085F7CE4770D22E75ACC6BD46FADF8BDB365465678E1C315C41C04B69369`
- recovery recover SHA-256 `47C9DB87AA95D6CA39B662756EFBFC4BD4E51A94BD07CD1C6037796FD3B1EBA0`
- distro restart SHA-256 `6C2C152F2CD8C35D8FB3C31509ABC63320110A254A625A9F4302FDCEDBB42E5A`
- versioned L3 observer SHA-256 `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`
- Node v24.18.1 SHA-256 `AC51903C4C111815D52280B1FDCC8DA067CBB37E2FE1A765097B85C3292C8582`

Thresholds remain unchanged:

- exactly 256 real Linux PID stress iterations
- exactly one measured Program Host invocation / one persistent connection
- >=40 successful typed operations
- >=12 successful real Linux-provider operations
- model calls between primitives: 0
- false Linux process rebounds: 0
- wrong Linux process mutations: 0
- false Linux file rebounds: 0
- wrong Linux file mutations: 0
- L3 exact process and strong-handle file recovery across SHELLeye-kernel-only death with Provider B incarnation preserved
- L4 actual Ubuntu distro restart with Linux provider-world epoch change and no transient-identity rebound
- L5 cross-provider non-merge
- post-L4 normal provider-aware operation required

## Measured order after operative freeze commit

1. L1 256-cycle real Linux process stress.
2. Exactly one Program Host invocation, whose process/file/cross-provider/delta cases also supply L2/L5 pressure.
3. L3 frozen prepare -> hard-stop final measured kernel only -> versioned native-gap observer -> restart same final measured DB/pipe/binaries -> frozen recover.
4. L4 frozen actual Ubuntu distribution restart.
5. Post-L4 provider-aware Program Host operation.
6. Evidence adjudication and `docs/14-BUILD-002-RESULTS.md` only if all frozen gates pass.

After the operative docs/13 freeze commit, no product source, frozen harness, threshold, or acceptance-envelope change is allowed. Any such change ends/preserves Run 004.