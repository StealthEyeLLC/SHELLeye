# Build 002 measured Run 002 - preserved failure

Status: **FAILED / ENDED - preserved prospectively**

```text
Run 002 acceptance freeze: fea71ed342fb6c7f3e9202e5aba2fbe2110ca346
Run 002 pre-L1 runtime bind: ceb75291ed2335da3e890171500c844e6f528800
Frozen implementation: 2d81186040268c101497555f5a9425d0eba18ce2
Authenticated Windows context: STEALTHEYELLC\StealthEye, Session 1
```

Run 002 used the real registered `Ubuntu-24.04` WSL2 provider. SYSTEM evidence was not substituted.

## L1 bounded real PID stress

Measured result: **PASS**.

```text
iterations: 256
PID reuse observed: 0
wrong rebound: 0
false Linux process rebounds: 0
```

The lack of observed reuse is permitted by the frozen best-effort stress rule; deterministic start-time/world-epoch rejection remains covered by the frozen provider-contract tests.

## Program Host measured invocation

The frozen one-invocation Program Host gate was then started under the same authenticated owner context. It exited nonzero before producing its final result artifact.

Measured failure:

```text
code: native_error
message: statx(fd) failed.
```

The invocation context records:

```text
owner: stealtheyellc\stealtheye
session: 1
exit: 1
```

`program-host.stderr.txt` preserves the exact emitted failure and Node stack. `program-host.stdout.txt` is empty and `C:\SHELLeye\Temp\build002\build002-program-host.json` was not produced.

This is a genuine required-gate failure. Run 002 ends here. No L3, L4, or final Build 002 adjudication is claimed from this run. Any diagnosis/repair is prospective and requires a new Build 002 measured-run freeze before another acceptance campaign.