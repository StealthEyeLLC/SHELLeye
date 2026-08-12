# SHELLeye Build 002 Run 004 - Post-Campaign Contamination Record

**Classification:** POST-CAMPAIGN / NOT COUNTED / DOES NOT RETROACTIVELY INVALIDATE TERMINAL MEASURED RESULT  
**Date:** 2026-08-12

The operative Run 004 measured freeze was `05545238e7a23091e86ae72174a4c0d329a1cf6d`.

The required measured sequence completed through its terminal post-L4 provider-aware Program Host gate before a second authority-aware task path executed another L4/post-L4 pair.

## Measured terminal chronology

- required measured L4 task: `SHELLeye Run004 Final Measured L4`
  - Task Scheduler start: 2026-08-12 13:07:34 -04:00
  - result: 0 / PASS
  - provider-authoritative compact result was published before any duplicate at `evidence/build002/run-004/MEASURED-L4.json`
  - first measured transition: `linux_epoch_e670dc03eafd2b924ea53c42` -> `linux_epoch_a4789dbf717453db9c20b8c8`
- required terminal post-L4 task: `SHELLeye Run004 Final Measured PostL4 ProgramHost`
  - Task Scheduler start: 2026-08-12 13:08:28 -04:00
  - Task Scheduler result: 0 / PASS
  - its completion metadata file was **created at 2026-08-12T17:08:32.0647498Z**; the script writes that metadata only after the frozen Program Host process has exited
  - controller-observed terminal result: PASS, one persistent connection, 55 typed operations, 38 Linux operations, 0 model calls, process/file assertions pass, cross-provider identities not merged, DB quick check `ok`

Therefore the required measured campaign was terminally complete by approximately 13:08:32 -04:00.

## Later duplicate activity

A separate authority-aware path subsequently launched:

- `SHELLeye Run004 Final L4`
  - start: 2026-08-12 13:08:43 -04:00
  - result: 0
- `SHELLeye Run004 Final PostL4 ProgramHost`
  - start: 2026-08-12 13:09:36 -04:00
  - result: 0

The duplicate L4 began about 11 seconds **after** the frozen campaign's terminal post-L4 task had already completed. It caused an additional conservative provider-world transition and later overwrote local convenience copies of `measured-l4*` / `measured-post-l4*` files. It did not alter the already-published first measured L4 evidence, the frozen source/harnesses, or any acceptance operation that had not yet occurred—there were no required runtime gates remaining.

## Harness provenance

The duplicate and original L4/post-L4 tasks referenced the same clean frozen harness content:

- Program Host tree `5411a5cd66865b024ef88d2a6232259dbe956575`
- Program Host SHA-256 `4CFB2CABC3FBCDB458B8D1A0EA05C56355FC3878D11F109771988F8E94DBE742`
- distro-restart SHA-256 `6C2C152F2CD8C35D8FB3C31509ABC63320110A254A625A9F4302FDCEDBB42E5A`

The later duplicate itself also returned exit 0 and remained conservative (another world-epoch boundary rather than identity rebound), but it is explicitly **not counted** as measured acceptance evidence.

## Adjudication rule

Scientific acceptance is adjudicated at the completion of the frozen terminal gate, not by requiring the live provider to remain forever untouched after the experiment has ended. Because the later L4 began only after the required terminal gate had completed and because the first measured L4 result was already provider-authoritatively published, this later activity is preserved as post-campaign contamination rather than retroactively converting a completed passing campaign into a failure.