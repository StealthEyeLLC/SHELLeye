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

Live `fsutil usn queryjournal` results:

### C: NTFS

```text
journal active: yes
Journal ID: 0x01dd1a7cac101e02
Maximum Size: 32 MB
Allocation Delta: 8 MB
supported record versions: 2 through 4
write range tracking: disabled
```

### X: ReFS

```text
Error 1179: The volume change journal is not active.
```

Architecture consequence:

Build 001 must **not depend on USN** for correctness/recovery. Native physical identity + watcher dirty signals + reconciliation are sufficient for the first slice. A later USN provider can exploit active journals without requiring SHELLeye to enable/configure one simply to satisfy the architecture.

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

Build 001 kernel development should initially run in the interactive `StealthEye` session through a persistent scheduled task/process, matching the workload session. Later system-service/multi-session architecture can be added when there is a concrete need to bridge multiple logon sessions.

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
- NTFS physical identity fixture plus ReFS smoke case.
- interactive-user development kernel/session for the first slice.

### Explicitly not required

- system-wide Node installation;
- system-wide PowerShell 7 installation;
- WSL execution;
- RDP;
- Docker;
- kernel driver;
- active USN journal on every volume;
- new Windows service creation just for the fixture.

## 14. Platform reinspection rule

Before Build 001 claims measured completion, re-query the exact runtime/toolchain/OS state used by the implementation and update this document if it materially differs.

The machine is concurrent reality; this file is a canonical baseline, not a claim that software versions can never change.
