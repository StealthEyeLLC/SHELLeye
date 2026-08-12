# SHELLeye Build 002 Results

Status: **COMPLETE / MEASURED / PASSED**  
Date: **2026-08-12**  
Final classification: **PASS - provider-neutral spine survived with Windows depth preserved**  
Product implementation: **6bb4806a64e27b82e7e664f6ad915364fe8d99b6**  
Operative measured freeze: **05545238e7a23091e86ae72174a4c0d329a1cf6d**

## Mission outcome

Build 002 pressure-tested whether SHELLeye's Windows-first machine-world ontology could survive contact with a materially different Linux/WSL2 provider without weakening the exact Windows behavior measured in Build 001.

It did.

The surviving architecture is provider-qualified rather than provider-flattened:

- Windows remains deep and exact under its native process/file/job/listener/service semantics;
- Ubuntu-24.04 WSL2 is represented as a distinct hosted Linux machine world rather than merged into the Windows machine identity;
- transient Linux process identity is native to the Linux provider and guarded by provider-world epoch + PID + `/proc` start ticks, strengthened by pidfd-backed actuation;
- Linux physical file identity uses statx/native filesystem identity and, where available, exported file-handle evidence;
- provider-world incarnation boundaries prevent old Linux transient identities from silently rebinding after a real distro restart;
- common SDK surfaces remain compact and provider-qualified rather than pretending Windows and Linux implementation witnesses are identical.

No measured gate required weakening Build 001's Windows identity or actuation rules.

## Frozen implementation and harness authority

Product implementation remained frozen through the final measured campaign:

```text
implementation commit: 6bb4806a64e27b82e7e664f6ad915364fe8d99b6
implementation tree:   ac54028898a4257cf04ca23dac7a373aacb69203
```

The only acceptance-instrumentation repair after earlier failed campaigns was prospectively versioned before the successful measured freeze:

```text
L3 observer commit:  cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d
acceptance tree:      b4f5942fbd6aa2a5e2cc82baf89d85400c6bd153
Program Host tree:    5411a5cd66865b024ef88d2a6232259dbe956575
observer blob:        651f4249c7e7f6e9add3a6cbeee39d49cf021004
observer SHA-256:     57ED62B0FCED171F618B9749C788452471ED3242FE0EB89E3350CC1210B269DD
```

The observer repair corrected execution instrumentation only: an earlier Run 003 wrapper had collided with PowerShell's reserved `$args` variable. No product source changed inside the successful measured campaign.

## Prospective prerequisite campaign

Before the final measured freeze, the unchanged candidate passed the entire prospective pressure sequence required after earlier execution-integrity failures:

- Release build: **PASS / 0 warnings / 0 errors**
- provider-contract tests: **5 / 5 PASS**
- retained Build 001 Windows hostile core: **25 / 25 PASS**
- false process rebounds: **0**
- false file rebounds: **0**
- false listener rebounds: **0**
- wrong process mutations: **0**
- wrong file mutations: **0**
- wrong-object mutations: **0**
- fresh owner-context Provider B L0: **PASS**
- prospective Linux PID stress: **256 / 256 PASS**
- repeated Program Host pressure: **3 / 3 PASS**, each 55 typed / 38 Linux / 0 model calls
- prospective L3 real SHELLeye-kernel gap: **PASS**
- prospective L4 actual Ubuntu distribution restart: **PASS**
- post-L4 Program Host: **PASS**, 55 / 38 / 0

Provider-authoritative evidence: `evidence/build002/run-004/PROSPECTIVE-FULL-PASS.md`.

## Final measured L0

The final measured runtime was created only after the full prospective sequence passed, the prospective runtime/anchor was retired, Ubuntu-24.04 was terminated once through the supported lifecycle under the authenticated owner, and final state/spool/temp roots were recreated empty.

Final measured bind:

```text
owner:                  stealtheyellc\stealtheye / Session 1
pipe:                   shelleye-build002-run004-final-measured
DB:                     C:\SHELLeye\state\build002-run004-final-measured\shelleye.db
initial kernel PID:     115388
kernel epoch:           kernel_1
Windows BootEpoch:      boot_1
Linux world:            world_1
Linux world epoch:      linux_epoch_e670dc03eafd2b924ea53c42
Linux provider epoch:   linux_provider_8fbc9a3f41f14f04b4d42d705cef26ae
Ubuntu:                 24.04.4 LTS
Linux kernel:           6.18.33.2-microsoft-standard-WSL2
Linux boot ID:          84231dbf-82a4-46a6-9adc-90f72bc3d6f5
PID namespace:          pid:[4026532221]
PID 1 start ticks:      269
DB quick-check:         ok
provider anchor PID:    78868 / exact
```

Fresh capability probe reported pidfd, statx, inotify, cgroup v2, systemd, exportable file handles, and unique mount IDs supported on the acceptance substrate.

The second freeze check matched the same Linux world epoch and boot ID before any measured acceptance case ran.

## L1 - Linux process identity / exact actuation

**PASS: 256 / 256 real iterations.**

```text
false Linux process rebounds = 0
wrong rebound = 0
```

The finite run observed no actual native PID reuse; deterministic start-time/provider-epoch mismatch rejection remained covered by the passing provider-contract suite.

Provider-authoritative evidence: `evidence/build002/run-004/MEASURED-L1.json`.

## Program Host / L2 / L5 / delta-world-sync pressure

**PASS.** Exactly one measured pre-L4 Program Host invocation used one persistent connection and executed:

```text
typed SHELLeye operations = 55
real Linux operations = 38
model calls between primitives = 0
```

Identity/actuation pressure passed:

- exec transition preserved the same exact Linux process concept;
- simultaneous instances of the same executable remained distinct;
- actuation through an obsolete Linux process concept was rejected;
- Linux physical-file identity survived rename;
- a hardlink mapped to the same physical file concept;
- unlink/recreate produced a new concept;
- old exact-write did not touch the replacement;
- Windows and WSL views of host-backed storage remained provider-qualified and **did not merge**;
- structured PowerShell remained available;
- DB quick-check remained `ok`;
- bounded delta/world-sync operation passed.

Under the frozen procedure, this invocation supplied the measured L2 file identity/exact-actuation pressure, L5 cross-provider hostile non-merge pressure, and Program Host threshold.

Provider-authoritative evidence: `evidence/build002/run-004/MEASURED-PROGRAM-HOST.json`.

## L3 - SHELLeye kernel recovery with Linux provider live

**PASS.**

Frozen prepare retained:

```text
process concept:        proc_266
Linux PID:              3486
start ticks:            30561
post-exec executable:   /usr/bin/sleep
file concept:           file_5
ext4 inode:             51418
mount ID:               2147483732
strong exported handle: true
world epoch:            linux_epoch_e670dc03eafd2b924ea53c42
```

The final measured kernel PID 115388 was hard-stopped while Provider B was deliberately left live.

After the required gap:

- old kernel absent: true;
- no replacement kernel during observation gap: true;
- exact WMI provider-lifetime anchor remained alive: true;
- versioned native-gap observer ran exactly once;
- observer PASS after approximately 49.35 seconds from prepare;
- Linux boot ID unchanged;
- PID namespace unchanged;
- PID 1 start ticks unchanged;
- PID 3486 / start ticks 30561 still exact;
- executable still `/usr/bin/sleep`;
- retained file still present.

SHELLeye then restarted on the same DB/pipe/binaries:

```text
new kernel PID: 27444
kernel epoch:   kernel_2
BootEpoch:      boot_1
```

Frozen recovery harness result:

```text
same Linux world epoch = true
processExact = true
fileOutcome = current
fileExact = true
strongGapHandle = true
```

This gate directly closes the failure seam that ended Run 003.

Provider-authoritative evidence: `evidence/build002/run-004/MEASURED-L3.json`.

## L4 - actual WSL distribution restart boundary

**PASS.** The selected `Ubuntu-24.04` distribution was terminated/restarted through the frozen acceptance harness while the Windows machine remained on the same BootEpoch.

First required measured L4 transition:

```text
Windows BootEpoch: boot_1 -> boot_1
Linux world epoch:
  linux_epoch_e670dc03eafd2b924ea53c42
  -> linux_epoch_a4789dbf717453db9c20b8c8
old process state: destroyed
old file state:    destroyed
false rebounds:    0
wrong mutations:   0
```

SHELLeye kernel PID 27444 / kernel epoch 2 remained live through the provider restart.

Provider-authoritative evidence: `evidence/build002/run-004/MEASURED-L4.json`.

## Terminal post-L4 operation

**PASS.** The required frozen post-L4 Program Host operation completed successfully on the new Linux provider incarnation:

```text
one persistent connection = true
typed operations = 55
Linux operations = 38
model calls = 0
provider world state = current
cross-provider identities merged = false
DB quick-check = ok
```

The task completed before any later duplicate destructive action. Its completion metadata file was created at `2026-08-12T17:08:32.0647498Z`.

Provider-authoritative evidence: `evidence/build002/run-004/MEASURED-POST-L4.json`.

## Post-campaign duplicate activity

A separate authority-aware path started another L4 at 13:08:43 -04:00 and another post-L4 Program Host at 13:09:36 -04:00. The required frozen terminal gate had already completed by approximately 13:08:32 -04:00.

The later activity returned exit 0 and remained conservative, but is classified **POST-CAMPAIGN / NOT COUNTED**. It overwrote local convenience copies of some raw L4/post-L4 files but could not retroactively alter the already-completed frozen campaign or the provider-authoritative first L4 result.

Full preservation record: `evidence/build002/run-004/POST-CAMPAIGN-CONTAMINATION.md`.

## Failed/ended campaigns preserved

Build 002 did not get a clean pass by erasing failed attempts.

- Run 001: preserved measured failure.
- Run 002: preserved execution-integrity failure/incompletion.
- Run 003: preserved execution-integrity failure at the L3 observation wrapper; product source not blamed for wrapper failure.
- early Run 004 freezes: prospectively superseded/inadmissible and explicitly excluded.
- final Run 004: complete measured campaign under operative freeze `05545238e7a23091e86ae72174a4c0d329a1cf6d`.

## Windows depth outcome

The Build 001 Windows provider remained a regression baseline rather than being redesigned downward to fit Linux.

The final candidate retained:

- Build 001 hostile core **25 / 25 PASS**;
- false process/file/listener rebounds **0**;
- wrong process/file/object mutations **0**;
- structured PowerShell;
- exact Windows native identity/actuation behavior;
- compact persistent Program Host semantics.

Provider neutrality therefore emerged through provider-qualified witnesses and world epochs, not lowest-common-denominator semantics.

## Final Build 002 declaration

All frozen final measured gates passed.

> **PASS - provider-neutral spine survived with Windows depth preserved**

SHELLeye Build 002 — Provider Neutrality Pressure Test is **COMPLETE / MEASURED / PASSED**.

The measured evidence supports canonical promotion of this Build 002 branch by **fast-forward only**. This result does not authorize Build 003 implementation or any new sibling-Eye/world-kernel redesign; subsequent sequencing remains separately evidence-driven.