# 03 — Initial Platform: STEALTHEYELLC

Status: **Canonical live target baseline**  
Inspection date: **2026-08-08**  
Inspection source: direct live machine queries through the connected StealthEye/EYE machine interface, not only prior project memory.

## 1. Machine

```text
computer: STEALTHEYELLC
manufacturer: HP
model: OMEN Gaming Laptop 16-ap0xxx
OS: Microsoft Windows 11 Home x64
DisplayVersion: 24H2
version: 10.0.26100
build: 26100.8973
CPU: AMD Ryzen 9 8940HX with Radeon Graphics
logical processors: 32
physical RAM: 33,342,455,808 bytes (~33.34 GB decimal)
BIOS: AMI F.13
```

GPUs observed live:

```text
AMD Radeon(TM) 610M
NVIDIA GeForce RTX 5060 Laptop GPU
```

Build 001 is deliberately optimized for this real Windows target rather than an imaginary lowest-common-denominator POSIX machine.

## 2. Windows build capability consequence

Microsoft documents `SystemBasicProcessInformation` as available from Windows 11 build **26100.4770**. The live machine is **26100.8973**.

Therefore Build 001 should use the newer process `SequenceNumber` field as the primary process-enumeration PID-reuse witness on this machine, with process creation time and handle verification strengthening exact actuation.

Source: [NtQuerySystemInformation / SystemBasicProcessInformation](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation).

The provider design keeps a fallback seam for older Windows versions, but Build 001 should not voluntarily throw away this stronger native capability.

## 3. Storage

Observed volumes:

```text
C:
  label: Windows
  filesystem: NTFS
  size: 700,747,608,064 bytes
  free: 612,227,350,528 bytes

X:
  label: Eye Dev
  filesystem: ReFS
  size: 322,122,547,200 bytes
  free: 317,390,225,408 bytes
```

Architecture consequences:

- `C:` provides the primary Build 001 deterministic physical-file identity fixture.
- `X:` provides a valuable ReFS smoke case so 128-bit file identity assumptions are exercised early.
- drive letters are bindings, not volume identity.
- operating-state DB/runtime/spools should live outside target repositories.

Suggested development layout:

```text
X:\SHELLeye\repo
C:\SHELLeye\runtime\kernel
C:\SHELLeye\runtime\powershell    # if separate provider wins
C:\SHELLeye\state\shelleye-dev.db
C:\SHELLeye\spool
C:\SHELLeye\Temp
```

These paths are Build 001 implementation guidance; no runtime was created by the setup pass.

## 4. USN/change-journal reality

Final-synthesis live probes confirmed:

### C: NTFS

```text
journal active: yes
Journal ID: 0x01dd1a7cac101e02
Maximum Size: 32 MB
Allocation Delta: 8 MB
supported record versions: 2 through 4
write range tracking: disabled
FSCTL_READ_FILE_USN_DATA on a temporary file: record v3, nonzero last USN
```

### X: ReFS

```text
fsutil queryjournal: Error 1179 — volume change journal is not active
FSCTL_READ_FILE_USN_DATA on a temporary file: record v3, last USN = 0
```

Architecture consequence:

Broad USN journal ingestion/replay remains post-Build-001. However, final verification exposed a real correctness limit: Windows file IDs can be reused after deletion, so `FILE_ID_INFO` alone cannot prove exact file continuity across an unobserved kernel gap.

Build 001 therefore uses one **narrow C: NTFS continuity token** for its retained recovery fixture: current journal ID + that file's last USN (`FSCTL_READ_FILE_USN_DATA`), in addition to volume + 128-bit FileId. The fixture file is left unchanged during the kernel gap; exact recovery requires the journal ID and file USN to remain unchanged. Any gap in that evidence reduces continuity instead of causing a rebound.

This does not turn USN into the filesystem event architecture and does not require creating/enabling a journal. X: remains a current 128-bit ReFS identity smoke test, not an equivalent post-gap continuity test while its journal is inactive.
## 5. .NET

Observed:

```text
dotnet: C:\Program Files\dotnet\dotnet.exe
SDK: 10.0.302
Microsoft.NETCore.App: 10.0.10
Microsoft.AspNetCore.App: 10.0.10
Microsoft.WindowsDesktop.App: 10.0.10
```

This supports the frozen Build 001 kernel choice of C# / .NET 10.

## 6. Node.js / npm

Node is not on the SYSTEM PATH, but the known portable runtime was directly verified:

```text
C:\AgentBrowser\tools\node-v24.18.1-win-x64\node.exe
Node: v24.18.1
npm: 11.16.0
```

Build 001 Program Host and deterministic Node fixture should invoke this runtime explicitly rather than modifying machine-wide PATH.

## 7. Git

Observed:

```text
C:\Program Files\Git\cmd\git.exe
git version 2.55.0.windows.3
```

SHELLeye may execute Git generically, but Git/source/repository engineering semantics remain CODEeye's domain.

## 8. PowerShell

Observed in the live machine-control context:

```text
Windows PowerShell: 5.1.26100.8972
powershell.exe: available
pwsh.exe: not found on SYSTEM PATH
```

This materially affects Build 001's structured-PowerShell implementation experiment.

The architecture is **not** `powershell.exe + stdout`. PowerShell hosting APIs can return real `PSObject` results through runspaces. Build 001 must choose the cleanest supported engine-hosting path after a small compatibility experiment:

1. host the available Windows PowerShell 5.1 engine in a provider process; or
2. package a compatible modern `Microsoft.PowerShell.SDK` provider without requiring a global `pwsh` installation.

Do not install PowerShell 7 merely because it is fashionable if the provider can meet the slice cleanly without it.

## 9. WSL

Observed live:

```text
WSL version: 2.7.11.0
WSL kernel: 6.18.33.2-2
WSLg: 1.0.73.2
Windows version reported by WSL: 10.0.26100.8973
VirtualMachinePlatform: enabled
Microsoft-Windows-Subsystem-Linux: enabled
```

The interactive user's WSL registration was found under the user's registry hive:

```text
user: STEALTHEYELLC\StealthEye
distribution: Ubuntu-24.04
WSL version: 2
base path: C:\Users\StealthEye\AppData\Local\wsl\{aa957c59-794f-4ad3-ae28-9188cae51ee3}
```

Important live constraint:

Calling WSL from the machine-control process running as `NT AUTHORITY\SYSTEM` returns:

```text
WSL_E_LOCAL_SYSTEM_NOT_SUPPORTED
```

Therefore future WSL execution is explicitly user/session-contextual. It must not be designed as though LocalSystem and the interactive user's WSL environment were interchangeable.

WSL is post-Build-001.

## 10. User/session reality

The connected machine-control service reports:

```text
identity: NT AUTHORITY\SYSTEM
machine-control executable process: session/service context
```

Live interactive desktop process:

```text
explorer.exe PID: 9448
Windows session ID: 1
owner: STEALTHEYELLC\StealthEye
```

This validates the architectural requirement to model process/session context directly.

Build 001 kernel development should initially run in the interactive `StealthEye` session through a persistent scheduled task/process, matching the workload session. `StealthEye` is a member of the local Administrators group. For Build 001, configure the kernel task to run with that account's highest available privileges because Microsoft documents volume change-journal identifier queries as administrator operations. This is an intrinsic Windows access requirement for the narrow NTFS continuity witness, not a SHELLeye privilege architecture. Later system-service/multi-session architecture can be added when there is a concrete need to bridge multiple logon sessions.

## 11. Relevant Windows services/features

Observed service state:

```text
EventLog      running / automatic
LanmanServer  running / automatic
Schedule      running / automatic
Winmgmt       running / automatic
WSearch       running / automatic
TermService   stopped / manual
```

Implications:

- SCM/Task Scheduler/Event Log/WMI infrastructure is available for provider tests.
- Build 001 can inspect an existing service instead of creating one simply for acceptance.
- stopped Remote Desktop Services is not a blocker; RDP is not part of Build 001.

## 12. Native API availability relevant to Build 001

The target Windows generation supports the native families chosen by the architecture:

- process enumeration/handles/times;
- Windows Job Objects and nested jobs;
- named pipes;
- `FILE_ID_INFO` / 128-bit file identity;
- `ReadDirectoryChangesW`;
- Service Control Manager APIs;
- Task Scheduler COM APIs;
- WTS/session APIs;
- IP Helper TCP/UDP owner tables;
- Registry notifications;
- Event Log / ETW;
- ConPTY, including the Windows 11 24H2 pseudoconsole lifecycle improvements.

Not all are Build 001 core.

## 13. Build 001 platform constraints derived from reality

### Frozen

- Windows-first native provider.
- .NET 10 kernel.
- portable Node 24 Program Host.
- Windows named-pipe local RPC.
- SQLite WAL operating state.
- `SystemBasicProcessInformation.SequenceNumber` process witness on this build.
- handle-bound `ProcessTelemetryIdInformation` is live-tested as optional corroboration, not a sole dependency.
- `PROC_THREAD_ATTRIBUTE_JOB_LIST` creation-time Job Object assignment and explicit `PROC_THREAD_ATTRIBUTE_HANDLE_LIST` stream inheritance are available on this Windows generation.
- NTFS physical identity fixture plus narrow C: journal-ID/file-USN post-gap continuity token; ReFS current-identity smoke case.
- interactive-user development kernel/session for the first slice, scheduled with highest available owner-account privileges for the NTFS journal-ID query.

### Explicitly not required

- system-wide Node installation;
- system-wide PowerShell 7 installation;
- WSL execution;
- RDP;
- Docker;
- kernel driver;
- active USN journal on every volume (only the already-active C: journal supplies the narrow Build 001 continuity token);
- new Windows service creation just for the fixture.

## 14. Final-synthesis verification note

On 2026-08-08 the final synthesis pass re-verified Windows 11 24H2 build 26100.8973, .NET 10.0.302, Node 24.18.1, Windows PowerShell 5.1.26100.8972, C: NTFS, X: ReFS, the C:/X: journal state above, WSL 2.7.11.0, and Explorer session 1. A live `NtQueryInformationProcess(ProcessTelemetryIdInformation)` probe succeeded and returned process sequence/start/boot metadata on this target. No product runtime was created by these probes.

## 15. Platform reinspection rule

Before Build 001 claims measured completion, re-query the exact runtime/toolchain/OS state used by the implementation and update this document if it materially differs.

The machine is concurrent reality; this file is a canonical baseline, not a claim that software versions can never change.

## Build 001 measured runtime

Build 001 implementation runtime:

- canonical clone: `X:\SHELLeye\repo`
- kernel publish: `C:\SHELLeye\runtime\kernel\app`
- state database: `C:\SHELLeye\state\shelleye-dev.db`
- spool root: `C:\SHELLeye\spool`
- temporary acceptance root: `C:\SHELLeye\Temp`
- named pipe: `shelleye-dev`
- scheduled task: `shelleye-kernel-dev`
- principal: `STEALTHEYELLC\StealthEye`, interactive, highest available privileges
- .NET SDK used: 10.0.302
- Node Program Host/runtime: 24.18.1
- PowerShell provider: in-kernel `Microsoft.PowerShell.SDK` 7.6.4
- C: exact gap-continuity provider: NTFS journal ID + per-file USN
- X: ReFS: current physical identity only; no Build 001 exact post-gap continuity claim
