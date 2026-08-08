# SHELLeye Build 001 Results

Status: **COMPLETE / MEASURED / PASSED**
Date: **2026-08-08**
Implementation commit: **bfc9e27325dc24eb890daaf88889a111ac060aa0**

## Implementation topology

- Kernel: C# / .NET 10, Windows-targeted
- State: SQLite WAL at `C:\SHELLeye\state\shelleye-dev.db`
- IPC: local Windows named pipe `shelleye-dev`
- Runtime: highest-privilege interactive-owner scheduled task `shelleye-kernel-dev`
- Program Host: Node.js 24.18.1, disposable, one persistent pipe connection per invocation
- PowerShell: in-kernel `Microsoft.PowerShell.SDK` 7.6.4 runspace, real `PSObject` projection before formatting
- Persistent output: per-process/per-stream restart-independent spool files with logical cursors

## Milestone A — Persistent Machine World

**PASS.** Final measured run:

- kernel PID before hard kill: **16936**
- kernel PID after restart: **23628**
- kernel epochs: **kernel_11 → kernel_12**
- BootEpoch: **boot_1**; native BootId: **5**
- logical job: **job_10**
- native job: `Local\SHELLeye.d83c733c-5d80-44c3-af92-8f088f5fe1ce.9d8c6d12489d4595a6cc451f6d5b9461`
- root process: **proc_44** / PID **24000**
- child process: **proc_47** / PID **23000**
- root alive while kernel count was zero: **true**
- child alive while kernel count was zero: **true**
- HTTP survived kernel death: **true**
- output recovered after the pre-kill cursor: **true**, **111 bytes**
- physical file: **file_11**; FILE_ID_128 `6835010000000b000000000000000000`; journal `0x01dd1a7cac101e02`; last USN `1389456384`
- listener continuity remained conservative: **listener_11 → listener_12**; old listener state **unknown**
- recovery delta families: `job.created`, `job.member_added`, `process.started`, `listener.opened`, `file.changed`, `listener.closed`, `world.reconciled`

The initial live A attempt exposed the native named-job reopen defect. D-031 (non-inheritable workload-held duplicated Job Object handle) fixed it; the final hard-kill run passed on the corrected architecture.

## Milestone B — Persistent Machine Objects / Delta First

**PASS.** Demonstrated typed concepts: machine, session, volume, directory, physical file, job, exact root/child processes, listener, service, and command. Process→session, job→members, child→parent with evidence quality, listener→exact process, service→current process, and file→volume relations were all exercised.

Final run emitted **19** scoped deltas from the acceptance cursor with event families: `job.created`, `job.member_added`, `process.started`, `listener.opened`, `file.changed`, `process.exited`. Real file and exact process waits passed; normal model-facing observations did not surface full process/service/TCP tables.

Final retained concepts included `job_11`, root `proc_48`, child `proc_51`, `file_12`, `listener_13`, and `svc_1`.

## Milestone C — Recovery Continuity / Identity Killer

**PASS: 25 / 25 canonical cases.**

```text
false process rebounds = 0
false file rebounds = 0
false listener rebounds = 0
wrong process mutations = 0
wrong file mutations = 0
false rebounds = 0
wrong-object mutations = 0
conservative stale/destroyed outcomes observed = 3
```

Supplemental regressions also proved bounded world-cursor expiration, current C: NTFS journal/USN continuity tokens, and X: ReFS physical identity without claiming NTFS-equivalent post-gap continuity.

## Milestone D — Programmable Machine Operation

**PASS.** One Program Host invocation used one persistent pipe connection and executed **52 typed SHELLeye operations** across machine/session/volume/file/process/job/network/service/PowerShell/world/raw/state domains with **0 model calls between primitives**.

- job: **job_12**
- first process: **proc_53**
- replacement process: **proc_58**; new exact concept: **true**
- file: **file_13**; same physical identity across rename: **true**
- first listener: **listener_14**
- replacement listener: **listener_15**; new concept: **true**
- PowerShell engine: **7.6.4**; provider `Microsoft.PowerShell.SDK`; structured object proof: **true**; primary type `System.Diagnostics.Process`
- service: `EventLog` / **svc_1** / current process `proc_16`
- raw escape hatch exit code: **0**
- final SQLite quick-check: **ok** / journal mode **wal**

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
4. Handle-based rename required DELETE access and a NUL-terminated `FILE_RENAME_INFO` filename buffer; corrected without falling back to pathname mutation.
5. Directory rename required physical-child path-binding reconciliation; stale namespace bindings are now removed when the same file ID appears at the moved path.
6. JavaScript cannot round-trip 64-bit FILETIME revisions safely as numbers; file inspection now returns opaque exact `revisionToken` and `identityToken` strings for preconditions/correlation.
7. Immediate process creation can precede appearance in `SystemBasicProcessInformation`; exact retain now waits a bounded interval while the created native handle remains authoritative.

## Architecture outcome

One frozen native implementation assumption was invalidated by Build 001: live job members alone did not preserve named Job Object reopenability after the kernel's last handle closed on STEALTHEYELLC. D-031 corrects that with a workload-held native lifetime anchor. No SHELLeye concept-lifetime decision, identity rule, or milestone semantic was invalidated.

## Remaining deferred capabilities

The canonical deferred set remains deferred: broad ETW, full USN journal replay/indexing, ConPTY terminal product, WSL product, Task Scheduler breadth, Linux/macOS providers, remote/SSH orchestration, broad registry/handle/thread/module graphs, and exact ReFS post-gap continuity on X:.

## Final Build 001 declaration

Milestones A, B, C, and D passed their measured canonical gates. Build 001 is complete. Build 002 has not begun.
