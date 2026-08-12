# SHELLeye Build 002 - Run 004 Execution Lease

**Status:** ACTIVE / PROSPECTIVE / PRE-MEASUREMENT  
**Date:** 2026-08-12  
**Implementation:** `6bb4806a64e27b82e7e664f6ad915364fe8d99b6`  
**Prior terminal Run 003:** `308c751ed37d557104596d72ad9c08696d4a7e65`

Run 003 remains failed/ended for execution integrity and is not reopened. Run 004 is a new measured campaign using the unchanged provider implementation at `6bb4806a64e27b82e7e664f6ad915364fe8d99b6`.

This lease serializes SHELLeye Build 002 execution on STEALTHEYELLC. Other SHELLeye Build 002/Run 003 task paths are non-authoritative while Run 004 is active and must not start, stop, restart, terminate Ubuntu, or mutate the Run 004 runtime/provider state.

The Run 003 L3 execution failure was wrapper-only: the native-gap observer used a PowerShell parameter named `$args`, causing the pathname argument to `/bin/cat` to be lost. Run 004 must freeze corrected observer plumbing before its first measured acceptance operation. No product source repair is authorized or required by that failure.

Before Run 004 measurement begins, bind prospectively:

- corrected L3 native-gap observer wrapper and exact hash;
- implementation commit and runtime binary hashes;
- frozen acceptance harness hashes;
- fresh owner-context L0 / Provider B incarnation;
- unique Run 004 pipe, DB, state/spool/temp/runtime paths;
- unchanged thresholds: PID stress 256; Program Host >=40 typed and >=12 Linux operations; 0 model calls; 0 false rebounds; 0 wrong mutations.

Any product source change after the Run 004 acceptance freeze ends/preserves Run 004 and requires another prospective run.