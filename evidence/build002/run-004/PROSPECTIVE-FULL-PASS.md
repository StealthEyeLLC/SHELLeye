# SHELLeye Build 002 - Run 004 Full Prospective Preflight PASS

**Status:** FULL PROSPECTIVE PREREQUISITE SEQUENCE PASS / NEW MEASURED FREEZE NOW AUTHORIZED  
**Date:** 2026-08-12  
**Product implementation:** `6bb4806a64e27b82e7e664f6ad915364fe8d99b6`  
**Versioned observer commit:** `cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d`  
**Prior Freeze 3 invalidation:** `f3afe77b3a96d2b84c34c7ae9878886e0e05a6c4`

This artifact satisfies every prospective prerequisite named by the Freeze 3 invalidation before a new measured Run 004 freeze is created. All tests below ran against one clean serialized prospective runtime after an explicit owner-context Ubuntu reset. No product source or frozen harness was patched.

## Authority and deterministic baseline

- implementation tree: `ac54028898a4257cf04ca23dac7a373aacb69203`
- corrected acceptance tree: `b4f5942fbd6aa2a5e2cc82baf89d85400c6bd153`
- Program Host tree: `5411a5cd66865b024ef88d2a6232259dbe956575`
- L3 observer blob: `651f4249c7e7f6e9add3a6cbeee39d49cf021004`
- L3 observer SHA-256: `57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD`
- Release build: PASS / 0 warnings / 0 errors
- provider contracts: 5 / 5 PASS
- Windows Build 001 hostile core: 25 / 25 PASS
- false/wrong rebound and mutation metrics: all 0

## Serialized prospective runtime

- pipe: `shelleye-build002-run004-prospective-final2`
- DB: `C:\SHELLeye\Temp\build002-run004-prospective-final2\state\shelleye.db`
- first kernel PID: 128096 / kernel epoch 1
- L3 restart kernel PID: 77356 / kernel epoch 2
- apphost SHA-256: `CCBDAF2FBCA69E77F70977C2A4DDD524D3A054A809D5FEB32771629BE8C2F2E3`
- owner context: `stealtheyellc\\stealtheye`, Session 1

Fresh prospective L0 before pressure:

- world epoch `linux_epoch_02aa2c9a500b9a8ef0cfa4a5`
- provider epoch `linux_provider_5f204f672fdf4af1a5d008e6cc9e6572`
- Linux boot ID `c435be7f-b55b-466e-b62c-71953535ee87`
- PID namespace `pid:[4026532223]`
- PID 1 start ticks `47478`
- pidfd/statx/exportable-file-handle/systemd: supported
- WMI anchor PID 27060 exact
- raw L0 SHA-256 `3CC08D7E302E614402C81D44254D209D0564C7D054C01EFD1A0AEF991D3FAE1E`

## Prospective 256 Linux PID stress

**PASS:** 256 / 256 iterations.

- false process rebounds: 0
- wrong rebound: 0
- reuse observed: 0 (best-effort reuse; deterministic reuse rejection remains covered by provider contracts)
- result SHA-256 `C35ECB4EDC218CBD884A40E62AB2DD1486046644403A61D26426DADE84676B9D`
- meta SHA-256 `F4F47EA5985CF28975DBA325AFF870DA8993EEFA8F1A0A1B279B73847DE7F3C6`

## Repeated Program Host/provider pressure

**PASS: 3 / 3 consecutive full invocations on one runtime.**

Each invocation produced:

- typed operations: 55
- Linux-provider operations: 38
- model calls between primitives: 0
- persistent provider-aware workflow: PASS

Run 1 sync stale: 0. Runs 2 and 3 sync stale: 1, both reconciled successfully. Summary SHA-256 `35C01C9978A5002E072983F6757D876C39842F9DCF2568170235D90FF12D230F`.

## Prospective L3 - real SHELLeye kernel gap

Frozen prepare:

- process concept `proc_284`
- Linux PID 3683
- start ticks 58568
- executable after exec: `/usr/bin/sleep`
- file concept `file_13`
- ext4 inode 51478
- mount ID 2147487328
- strong exported-handle witness: true
- world epoch `linux_epoch_02aa2c9a500b9a8ef0cfa4a5`
- prepare SHA-256 `548BABF620157537A05E4E6AC2C632663A3732BE24CD2D58E8CA249CB8F1C6AE`

Hard gap:

- kernel PID 128096 forcibly stopped; no replacement kernel during the gap
- WMI anchor PID 27060 remained alive with exact Windows start-time match
- controller gap evidence SHA-256 `0FD7369509F35F77FAA0721ED8567E17C50276AC474B2D57FE846BF15ABE9619`

Versioned provider-published native-gap observer, after 46 seconds from prepare:

- observer exit: 0 / PASS
- provider observable: true
- PID 3683 still exact with start ticks 58568
- executable `/usr/bin/sleep`
- retained file present: true
- Linux boot ID unchanged `c435be7f-b55b-466e-b62c-71953535ee87`
- PID namespace unchanged `pid:[4026532223]`
- PID 1 start ticks unchanged `47478`
- observer evidence SHA-256 `995D5F99A328FFD0E7F79574C47C76E11FC8D3C4B330E1A99B0FD5B5CBFA5DEA`

Kernel restart:

- new kernel PID 77356
- kernel epoch 2
- same pipe and DB
- same exact WMI anchor

Frozen recovery harness:

- PASS
- same Linux world epoch: true
- process exact: true
- strong-handle file outcome: current
- file exact: true
- strong gap handle: true
- recovery evidence SHA-256 `C354208D8A764533998E739119579278B0E38834ACB53F48BD660A7CAF949520`

**Prospective L3: PASS.**

## Prospective L4 - actual Ubuntu distribution restart

Frozen `build002-distro-restart.js` executed through the real owner-context kernel.

- PASS
- Windows BootEpoch remained `boot_1`
- Linux world epoch changed from `linux_epoch_02aa2c9a500b9a8ef0cfa4a5` to `linux_epoch_d87b75a073e1de929d8475c5`
- old Linux process state: destroyed
- old Linux file state: destroyed
- frozen file had a strong handle, but no false continuation was invented across the real provider incarnation change
- false rebounds: 0
- wrong object mutations: 0
- L4 evidence SHA-256 `BAD6428BEB3190598AFD2A4A5AF4D08F7A61E36413A18D805B6A6FAF20756185`

**Prospective L4: PASS.**

## Post-L4 provider-aware pressure

A full Program Host invocation after the real distribution restart passed:

- typed operations: 55
- Linux-provider operations: 38
- model calls: 0
- provider world: `world_1`, state `current`
- world.sync stale rows: 1, reconciled successfully
- post-L4 meta SHA-256 `C2C02161A8F96A11F943C5E85C02131A181739A14C5B9904785AF25402C6CA03`
- raw stdout SHA-256 `0D571270BFD7FF291095364C6FF5B90CA0758C88916E0D66EA742D552F990B01`

Current post-L4 provider state at proof capture:

- world epoch `linux_epoch_d87b75a073e1de929d8475c5`
- provider epoch `linux_provider_090bdaec31ef4aa6a90e833057df8c9c`
- PID namespace `pid:[4026532222]`
- PID 1 start ticks `69919`

## Prospective adjudication

Every prerequisite enumerated in `FREEZE-3-INVALIDATION.json` is now green on the unchanged candidate:

1. Release build 0/0 - PASS
2. provider contracts 5/5 - PASS
3. Windows hostile core 25/25, zero false/wrong metrics - PASS
4. fresh owner L0 - PASS
5. prospective 256 PID stress - PASS
6. prospective repeated Program Host pressure - PASS 3/3
7. prospective L3 kernel-gap recovery using the versioned observer - PASS
8. prospective L4 actual Ubuntu restart - PASS
9. prospective post-L4 provider-aware operation - PASS

A new measured Run 004 freeze is therefore authorized. Measured evidence has **not** been inferred from these prospective results; the measured campaign must still start from a fresh zero-state provider/runtime bind after this artifact.