# Build 002 Run 003 Execution Lease

Date: 2026-08-12
Status: **ACTIVE EXECUTION SERIALIZATION — NOT AN ACCEPTANCE FREEZE**

## Authority

Provider-authoritative Build 002 implementation source:

`6bb4806a64e27b82e7e664f6ad915364fe8d99b6`

Implementation source tree:

`ac54028898a4257cf04ca23dac7a373aacb69203`

This commit adds evidence/control-plane text only. It does not change product source.

## Purpose

Run 003 preparation has been repeatedly contaminated by concurrent SHELLeye Build 002 tabs starting, stopping, or replacing the single SHELLeye kernel and the shared Ubuntu-24.04 WSL provider while another prospective L3 was in flight.

The most recent prospective 6bb evidence is strong but **unfrozen**: Release build 0 warnings/0 errors; provider contracts 5/5; retained Windows hostile core 25/25 with zero false rebounds/wrong mutations; Program Host 3/3; Linux PID stress 256/256; and native L3 gap evidence proved the same WSL userspace, retained process lifetime, strong-handle file, and systemd unit survived a hard kernel gap. Recovery clients were twice preempted after a restarted kernel had reached `ready`, so no such prospective run is counted as measured acceptance.

## Execution serialization rule

Until this lease is replaced by a fresh Run 003 acceptance freeze or terminal Run 003 evidence, no parallel SHELLeye Build 002 execution path should:

- start, stop, replace, or kill a SHELLeye kernel;
- run `wsl.exe --terminate Ubuntu-24.04` or otherwise restart Provider B;
- create a competing SHELLeye Build 002 runtime/pipe;
- reuse any Run 003 measured pipe or state database;
- mutate Build 002 product source.

Sibling Eyes are outside this lease and must not be modified.

## Preserved local control evidence

Before activating this lease, the executing path snapshotted and disabled 184 legacy SHELLeye Build 002 / Run003 scheduled tasks. Snapshot SHA-256:

`AAB84C97B450B2E4F0B9D24CBC50FB8007D4C845BCEC8F0EEAB3E521D2A89855`

No sibling-Eye task was disabled.

## Next authorized sequence

1. Rebuild a fresh runtime from implementation commit `6bb4806a...` only.
2. Use a fresh unique measured pipe and fresh state database.
3. Capture fresh owner-context L0 and exact runtime/binary/provider-anchor binding.
4. Publish a new Run 003 acceptance freeze before any measured L1 operation.
5. After provider verification of that freeze, execute measured L1, Program Host, L3, L4, and post-L4 gates with source immutable.
6. Any measured required-gate failure ends the run and is preserved before prospective repair.

This lease does not declare Build 002 accepted, frozen, complete, or canonical.