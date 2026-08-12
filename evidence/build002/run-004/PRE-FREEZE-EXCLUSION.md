# SHELLeye Build 002 Run 004 - Pre-Freeze Exclusion

The first Run 004 acceptance-freeze artifact (`ea3be21f8bd14ee4780935856ce01003f862e7ea`) bound an L0 snapshot captured before a subsequent **prospective** owner-context Ubuntu reset at 2026-08-12T16:38:12Z.

After that reset, the Run 004 kernel was re-established on the same frozen implementation/harness as kernel epoch 2 and a new L0 was captured at 2026-08-12T16:39:20Z. Therefore the old L0 binding was stale before any admissible measured acceptance operation.

A 256-cycle Linux PID stress ran from 2026-08-12T16:40:44Z through 16:41:13Z and passed 256/256 with zero false rebounds and zero wrong rebound. It is **EXCLUDED / PRE-FREEZE / NOT COUNTED** because the then-published freeze had not yet bound the post-reset Provider B incarnation.

This is an execution-order correction, not a product or threshold change. Run 004 measured acceptance begins only after a replacement prospective freeze binds the post-reset L0. Product implementation remains `6bb4806a64e27b82e7e664f6ad915364fe8d99b6`; frozen harness content remains unchanged from observer commit `cd7dd98f8ec8df910f41c89e083b855bf0cc3c4d`.