# SHELLeye Build 001 Results

Status: **COMPLETE / MEASURED / PASSED**
Date: **2026-08-08**
Implementation commit: **bfc9e27325dc24eb890daaf88889a111ac060aa0**

## Implementation topology

- Kernel: C# / .NET 10, Windows-targeted
- State: SQLite WAL at C:\SHELLeye\state\shelleye-dev.db
- IPC: local Windows named pipe shelleye-dev
- Runtime: highest-privilege interactive-owner scheduled task shelleye-kernel-dev
- Program Host: Node.js 24.18.1, disposable, one persistent pipe connection per invocation
- PowerShell: in-kernel Microsoft.PowerShell.SDK 7.6.4 runspace, real PSObject projection before formatting
- Persistent output: per-process/per-stream restart-independent spool files with logical cursors

## Milestone A — Persistent Machine World

**PASS.** Final measured run:

- kernel PID before hard kill: **16936**
- kernel PID after restart: **23628**
- BootEpoch: **boot_1**; native BootId: **5**
- logical job: **job_10**
- native job: $(@{passed=True; completedUtc=2026-08-08T20:22:24.768Z; kernelPidBefore=16936; kernelPidAfter=23628; kernelEpochBefore=kernel_11; kernelEpochAfter=kernel_12; bootEpoch=boot_1; nativeBootId=5; jobId=job_10; nativeJobName=Local\SHELLeye.d83c733c-5d80-44c3-af92-8f088f5fe1ce.9d8c6d12489d4595a6cc451f6d5b9461; rootProcessId=proc_44; rootPid=24000; childProcessId=proc_47; childPid=23000; httpSurvived=True; rootAliveDuringGap=True; childAliveDuringGap=True; outputGapRecovered=True; outputAfterCursorBytes=111; fileId=file_11; fileIdentity=; fileContinuity=; listenerBefore=listener_11; listenerAfter=listener_12; oldListenerStateAfterGap=unknown; session=; volume=; service=; recoveryDeltaTypes=System.Object[]; worldCursorBefore=427; worldCursorAfter=447; sync=; cleanup=}.nativeJobName)
- root process: **proc_44** / PID **24000**
- child process: **proc_47** / PID **23000**
- root alive while kernel count was zero: **True**
- child alive while kernel count was zero: **True**
- HTTP survived kernel death: **True**
- output recovered after the pre-kill cursor: **True**, **111 bytes**
- physical file: **file_11**; journal **0x01dd1a7cac101e02**; last USN **1389456384**
- listener continuity remained conservative: **listener_11 → listener_12**; old listener state **unknown**

The initial live A attempt exposed the native named-job reopen defect. D-031 (non-inheritable workload-held duplicated Job Object handle) fixed it; the final hard-kill run passed on the corrected architecture.

## Milestone B — Persistent Machine Objects / Delta First

**PASS.** Demonstrated typed concepts: machine, session, volume, directory, physical file, job, exact root/child processes, listener, service, and command. Process→session, job→members, child→parent with evidence quality, listener→exact process, service→current process, and file→volume relations were all exercised.

Final run emitted **19** scoped deltas from the acceptance cursor with event families: $([string]::Join(', ',@{passed=True; completedUtc=2026-08-08T20:22:34.925Z; concepts=; relationships=; waits=; delta=; output=; sync=; payloadDiscipline=; cleanup=}.delta.types)). Real file and exact process waits passed; normal model-facing observations did not surface full process/service/TCP tables.

## Milestone C — Recovery Continuity / Identity Killer

**PASS: 25 / 25 canonical cases.**

`	ext
false process rebounds = 0
false file rebounds = 0
false listener rebounds = 0
wrong process mutations = 0
wrong file mutations = 0
false rebounds = 0
wrong-object mutations = 0
conservative stale/destroyed outcomes observed = 3
`

Supplemental regressions also proved bounded world-cursor expiration, current C: NTFS journal/USN continuity tokens, and X: ReFS physical identity without claiming NTFS-equivalent post-gap continuity.

## Milestone D — Programmable Machine Operation

**PASS.** One Program Host invocation used one persistent pipe connection and executed **52 typed SHELLeye operations** across machine/session/volume/file/process/job/network/service/PowerShell/world/raw/state domains with **0 model calls between primitives**.

- first process: **proc_53**
- replacement process: **proc_58**; new exact concept: **True**
- first listener: **listener_14**
- replacement listener: **listener_15**; new concept: **True**
- PowerShell engine: **7.6.4**; structured object proof: **True**; primary type **System.Diagnostics.Process**
- same physical file identity across rename: **True**
- raw escape hatch exit code: **0**
- final SQLite quick-check: **ok**

## Benchmark

Representative machine-local comparison after A–D:

| Metric | Conventional shell rediscovery | SHELLeye scoped observation |
|---|---:|---:|
| primitive commands / typed operations | 4 | 8 |
| model-facing observation bytes | 30762 | 1721 |
| raw stdout bytes surfaced | 30762 | 0 |
| measured local duration | 1549.0 ms | 34.0 ms |
| Program Host operations/model turn | n/a | 52 |

This benchmark is illustrative, not a general throughput claim: the shell side intentionally performs broad rediscovery while SHELLeye uses retained/scoped concepts.

## Important implementation defects found and fixed

1. Named Job Object reopenability did not survive loss of the creator's last handle on the target despite live assigned members; fixed by D-031 workload-held non-inheritable duplicate handle.
2. Highest-privilege kernel pipe default ACL rejected the owner's medium-integrity Program Host; fixed with an explicit native same-owner/System pipe ACL.
3. IP Helper owner-module TCP rows require native alignment after the count field; corrected listener parsing and verified bind timestamps/owners.
4. Handle-based rename required DELETE access and a NUL-terminated FILE_RENAME_INFO filename buffer; corrected without falling back to pathname mutation.
5. Directory rename required physical-child path-binding reconciliation; stale namespace bindings are now removed when the same file ID appears at the moved path.
6. JavaScript cannot round-trip 64-bit FILETIME revisions safely as numbers; file inspection now returns opaque exact evisionToken and identityToken strings for preconditions/correlation.
7. Immediate process creation can precede appearance in SystemBasicProcessInformation; exact retain now waits a bounded interval while the created native handle remains authoritative.

## Remaining deferred capabilities

The canonical deferred set remains deferred: broad ETW, full USN journal replay/indexing, ConPTY terminal product, WSL product, Task Scheduler breadth, Linux/macOS providers, remote/SSH orchestration, broad registry/handle/thread/module graphs, and exact ReFS post-gap continuity on X:.

## Final Build 001 declaration

Milestones A, B, C, and D passed their measured canonical gates. Build 001 is complete. Build 002 has not begun.