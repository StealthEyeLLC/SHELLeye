# SHELLeye Build 002 - Measured Acceptance Freeze, Run 004 / Freeze 2

**Status:** ACTIVE MEASURED-CANDIDATE FREEZE - RUN 004; POST-RESET L0 COMPLETE; L1-L5 NOT YET STARTED  
**Date:** 2026-08-12

Freeze `ea3be21f8bd14ee4780935856ce01003f862e7ea` is superseded prospectively because its L0 preceded a later prospective provider reset. The passed PID-stress attempt executed before this replacement freeze and is preserved as excluded in `PRE-FREEZE-EXCLUSION.md`.

## Frozen authority

- product implementation: `6bb4806a64e27b82e7e664f6ad915364fe8d99b6`
- product tree: `ac54028898a4257cf04ca23dac7a373aacb69203`
- Run 004 execution lease: `20eee14807bbe2d11bd959682813a97c2f763571`
- corrected observer commit: `cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d`
- acceptance tree: `b4f5942fbd6aa2a5e2cc82baf89d85400c6bd153`
- Program Host tree: `5411a5cd66865b024ef88d2a6232259dbe956575`
- L3 observer blob: `651f4249c7e7f6e9add3a6cbeee39d49cf021004`
- L3 observer SHA-256: `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`

No product source changed from 6bb. The observer was fixed and smoke-tested prospectively before this freeze.

## Frozen runtime

- pipe: `shelleye-build002-run004-measured`
- DB: `C:\\SHELLeye\\state\\build002-run004\\shelleye.db`
- kernel runtime: `C:\\SHELLeye\\runtime\\build002-run004\\kernel\\app`
- Linux helper runtime: `C:\\SHELLeye\\runtime\\build002-run004\\linux\\app`
- `SHELLeye.Run004.Measured.exe` SHA-256 `CCBDAF2FBCA69E77F70977C2A4DDD524D3A054A809D5FEB32771629BE8C2F2E3`
- `SHELLeye.Kernel.dll` SHA-256 `822237700CC99133E5C2376959B104D2190F33F09FAA163A82315D5677826617`
- `SHELLeye.Platform.Linux` SHA-256 `33533827987DAA5A7801CC2EC88407CA941379594D313557B6845DBCB5163F61`
- `SHELLeye.Platform.Linux.dll` SHA-256 `726E27D9016A9BBDBB0174D95CEC1AA66A3A2D4EBD14A20DC4AF4FF932D7A22C`
- Windows publish files: 271
- Linux publish files: 192

## Frozen harnesses

- Program Host SHA-256 `4CFB2CABC3FBCDB458B8D1A0EA05C56355FC3878D11F109771988F8E94DBE742`
- PID stress SHA-256 `C99152F29831C512351864F565711D562E60F7AB82E268A9CA8968009065F669`
- recovery prepare SHA-256 `28FA085F7CE4770D22E75ACC6BD46FADF8BDB365465678E1C315C41C04B69369`
- recovery recover SHA-256 `47C9DB87AA95D6CA39B662756EFBFC4BD4E51A94BD07CD1C6037796FD3B1EBA0`
- distro restart SHA-256 `6C2C152F2CD8C35D8FB3C31509ABC63320110A254A625A9F4302FDCEDBB42E5A`
- L3 native-gap observer SHA-256 `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`
- Node v24.18.1 SHA-256 `AC51903C4C111815D52280B1FDCC8DA067CBB37E2FE1A765097B85C3292C8582`

## Deterministic validation

From clean detached `cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d`:

- Release build PASS / 0 warnings / 0 errors
- provider contracts 5/5 PASS
- Build 001 hostile core 25/25 PASS
- all false-rebound metrics 0
- all wrong-mutation metrics 0

## Fresh post-reset L0

Provider-authoritative compact bind: `evidence/build002/run-004/L0-BIND-2.json`.

- captured: `2026-08-12T16:39:20.9341454+00:00`
- owner: `stealtheyellc\\stealtheye`, Session 1
- kernel PID: 50920 / kernel epoch `kernel_2`
- world `world_1`, state current
- world epoch `linux_epoch_b01d7f688625a0c4a7a6f76d`
- provider epoch `linux_provider_d0a372ef9895413dadf8c91fcece15f7`
- boot ID `30755c03-7afe-4407-ae78-5116e87be4d1`
- PID namespace `pid:[4026532221]`
- PID 1 start ticks 84
- anchor PID 93592 exact
- raw L0 SHA-256 `AD7DBBF7019E5404835D50147F86768083540028461E8A9C00CD14B958F99253`
- freeze check SHA-256 `1C13738A34276C6EFE909E39FFD199B62C0C38D4B78EE40CCC7A8B4C19B8E6BE`
- freeze check: world epoch match true / boot ID match true / DB quick check ok

## Frozen thresholds

- PID stress exactly 256
- Program Host exactly one invocation / one persistent connection
- >=40 typed operations
- >=12 real Linux operations
- model calls between primitives 0
- false Linux process rebounds 0
- wrong Linux process mutations 0
- false Linux file rebounds 0
- wrong Linux file mutations 0
- L3 exact kernel-gap recovery with Provider B native incarnation preserved
- L4 actual Ubuntu distribution restart boundary with no transient-identity rebound
- L5 cross-provider non-merge

No product, harness, threshold, or acceptance-envelope change is permitted after this freeze. Any such change ends/preserves Run 004 and requires a new run.