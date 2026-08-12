# SHELLeye Build 002 — Provider Neutrality Research

Status: **RESEARCH / NOT CANONICAL**
Date: **2026-08-12**
Build 001 baseline: `54d2070365c04205fe4593e1d95ea76c302a709a` on `main`
Research target: STEALTHEYELLC + one materially different second provider

## 1. Research question

Which SHELLeye machine-world semantics are genuinely provider-neutral when pressed against a real Linux provider, without weakening the measured Windows provider or manufacturing cross-platform sameness?

This artifact records evidence and the synthesis used to freeze the Build 002 experimental envelope. It is not itself a canonical architecture declaration.

## 2. Authority and fresh baseline

Repository authority was read in the repository-defined order before source changes:

1. `README.md`
2. `docs/AUTHORITY.md`
3. `docs/00-CHARTER.md`
4. `docs/01-ARCHITECTURE.md`
5. `docs/02-BUILD-001-SLICE.md`
6. `docs/03-PLATFORM-STEALTHEYELLC.md`
7. `docs/04-ROADMAP.md`
8. `docs/05-RESEARCH-BASELINE.md`
9. `docs/06-DECISIONS.md`
10. `docs/07-CAPABILITY-MATRIX.md`
11. `docs/08-WORKFLOW-PRESSURE-TESTS.md`
12. `docs/09-BUILD-001-RESULTS.md`

No `AGENTS.md` exists on the verified Build 001 main tree.

Fresh pre-change Windows regression on 2026-08-12:

- Release solution build: PASS, 0 warnings, 0 errors.
- Build 001 hostile core: PASS, 25/25.
- real PID reuse observed during the bounded stress case: yes.
- false process rebounds: 0.
- false file rebounds: 0.
- false listener rebounds: 0.
- wrong process mutations: 0.
- wrong file mutations: 0.
- wrong-object mutations: 0.

Build 002 may not earn portability by regressing these Windows properties.

## 3. Live STEALTHEYELLC provider reconnaissance

### Windows host

Fresh direct machine queries through the owner-controlled EYE substrate:

```text
machine: STEALTHEYELLC
execution identity: NT AUTHORITY\SYSTEM
Windows DisplayVersion: 25H2
CurrentBuild: 26200
UBR: 9168
edition: Core / Home
architecture: x64
```

The legacy `ProductName` registry/computer-info field still reports `Windows 10 Home`; Build/DisplayVersion are the useful current version witnesses.

### SHELLeye installation/runtime

```text
repository: X:\SHELLeye\repo
local HEAD before Build 002 branch: 54d2070365c04205fe4593e1d95ea76c302a709a
working tree before branch: clean
runtime root: C:\SHELLeye\runtime
state root: C:\SHELLeye\state
spool root: C:\SHELLeye\spool
scheduled task: shelleye-kernel-dev
task principal: StealthEye
task logon type: Interactive
task run level: Highest
task current state during reconnaissance: Ready
```

The current machine-control path is LocalSystem, while the product kernel is intentionally configured for the owner account.

### WSL installation

Fresh Windows-side evidence:

```text
Microsoft-Windows-Subsystem-Linux: Enabled
VirtualMachinePlatform: Enabled
Microsoft Store WSL package: 2.7.11.0
WSLService: Running / Automatic
owner default WSL version: 2
owner distribution: Ubuntu-24.04
distribution registration ID: {aa957c59-794f-4ad3-ae28-9188cae51ee3}
distribution version: WSL2
distribution base path:
  C:\Users\StealthEye\AppData\Local\wsl\{aa957c59-794f-4ad3-ae28-9188cae51ee3}
ext4.vhdx present: yes
```

The canonical 2026-08-08 live platform pass measured WSL kernel `6.18.33.2-2`. A fresh Linux-side kernel query is not currently possible because no owner interactive token/session is available to this execution path.

### Critical current constraint

Both `wsl.exe --status` and `wsl.exe -l -v` executed as LocalSystem return:

```text
Running WSL as local system is not supported.
Error code: Wsl/WSL_E_LOCAL_SYSTEM_NOT_SUPPORTED
```

This is provider evidence, not an incidental tooling inconvenience. The second provider is genuinely **user-contextual** on this host.

No Linux acceptance result may be inferred from the Windows-side registration or from mocks.

## 4. Candidate second provider

**Selected for the frozen Build 002 experiment: Ubuntu-24.04 on WSL2, owner registration `{aa957c59-794f-4ad3-ae28-9188cae51ee3}`.**

Why it is materially different:

- WSL2 runs a real Linux kernel with Linux syscalls.
- Microsoft documents WSL2 distributions as isolated containers in a managed utility VM.
- distributions have their own PID, mount, user, cgroup namespaces and init process while sharing kernel/network/device substrate with other WSL2 distributions.
- native Linux process/file/cgroup/systemd primitives therefore pressure the Windows assumptions instead of wrapping another Windows API.

Primary source:
- https://learn.microsoft.com/windows/wsl/about
- https://learn.microsoft.com/windows/wsl/compare-versions

## 5. Machine-world boundary finding

A WSL distribution should not be merged into the Windows `machine_*` identity.

Build 002 synthesis:

```text
Windows host machine_* 
    HOSTS
provider-qualified Linux machine world (WSL distro registration)
```

The Linux world has a stable provider registration identity plus a provider-world epoch.

Why:

- PID/mount/user/cgroup namespaces and init are distro-specific.
- the Linux kernel boot is shared at the WSL utility-VM level and therefore is not by itself a distro lifecycle witness.
- `wsl --terminate <distro>` can terminate one distribution without being equivalent to a Windows boot.
- `wsl --shutdown` terminates all WSL distributions and the WSL2 utility VM.

Therefore common `BootEpoch` language must narrow to **provider-world epoch** for portability. The existing Windows `BootEpoch` remains its deep Windows facet; it is not deleted or weakened.

Linux/WSL epoch evidence candidate:

```text
Linux kernel boot_id
+ distro PID namespace identity
+ distro init (/proc/1) starttime
+ stable distro machine/registration identity
```

A provider/distro restart that changes the distro incarnation must advance the Linux provider-world epoch even when the shared WSL kernel `boot_id` does not change.

Primary source:
- https://learn.microsoft.com/windows/wsl/basic-commands
- https://learn.microsoft.com/windows/wsl/about

## 6. Process identity finding

Linux PID is a locator, not persistent identity.

Strong native evidence:

- `pidfd_open()` returns a file descriptor referring to one task.
- pidfds are pollable for process exit.
- `pidfd_send_signal()` is specifically documented to avoid PID-reuse races; a pidfd is a stable reference to a specific process and signaling fails after that process is gone.
- `/proc/<pid>/stat` field 22 reports process start time since system boot in clock ticks.

Primary sources:
- https://man7.org/linux/man-pages/man2/pidfd_open.2.html
- https://man7.org/linux/man-pages/man2/pidfd_send_signal.2.html
- https://man7.org/linux/man-pages/man5/proc_pid_stat.5.html

Build 002 process contract:

```text
persistent witness:
  provider-world epoch
  + PID
  + /proc starttime ticks
  + namespace/provider qualification

exact actuation:
  pidfd_open(PID)
  -> verify retained provider-world epoch/starttime while pidfd is held
  -> pidfd_send_signal(pidfd, ...)
```

pidfd is a strong **live operation witness**, not a persistent cross-provider-restart token. It must not be serialized and claimed to survive WSL/kernel restart.

`exec` does not create a new Linux process lifetime merely because executable image changes; the native process incarnation remains the same PID/starttime/pidfd lifetime. This differs from "same executable launched again", which is a new process concept.

## 7. File identity finding

Path is a binding on Linux just as it is on Windows, but Windows `FILE_ID_INFO` is not a Linux abstraction.

### Current Linux witness

`statx()` can supply:

- inode number;
- device major/minor;
- mount ID;
- creation time where supported;
- `STATX_MNT_ID_UNIQUE` on Linux 6.8+, whose unique mount ID is documented not to be reused while the system is running.

Primary source:
- https://man7.org/linux/man-pages/man2/statx.2.html

Candidate current witness:

```text
provider-world epoch
+ filesystem/device identity
+ unique mount identity when available
+ inode
```

That is useful for current correspondence, but inode/device equality alone is not frozen as proof across an unobserved provider gap.

### Stronger filesystem-handle facet

Linux `name_to_handle_at()` / `open_by_handle_at()` can expose a filesystem-specific persistent handle where the filesystem and permissions support it. The Linux man-pages example explicitly demonstrates delete/recreate with the same inode while the old handle correctly resolves stale instead of rebinding.

Primary source:
- https://man7.org/linux/man-pages/man2/open_by_handle_at.2.html

Build 002 therefore treats an exportable file handle as an **optional Linux facet capability**, not a universal requirement. If unavailable, exact retained file continuity across a provider gap is conservatively lost.

### Exact mutation

Where a Linux operation can use an already-open exact file descriptor, act through that descriptor.

A generic Linux rename/unlink does not automatically have the same "rename/delete by exact open handle" property as Windows handle-based mutation. Build 002 must not lie about this. Exact retained write is in scope; provider operations that cannot close the verify-to-path race remain unsupported/qualified rather than being falsely universalized.

## 8. File observation finding

`inotify` is suitable as a targeted dirty signal:

- it emits structured events;
- file watches are inode based;
- rename pairs carry cookies;
- queue overflow is explicit via `IN_Q_OVERFLOW`;
- pathnames can already be stale by the time user space handles an event.

Primary source:
- https://man7.org/linux/man-pages/man7/inotify.7.html

This fits Build 001's existing principle:

```text
event/watcher = dirty signal
current native query = truth
overflow/gap = reconcile
```

`fanotify` can expose file handles in FID reporting modes and may be valuable later, but it is not required for the bounded Build 002 slice and must not become broad telemetry ingestion.

## 9. Workload/job pressure finding

Windows Job Objects and Linux cgroup v2 are overlapping but non-identical.

Linux cgroup v2:

- organizes processes hierarchically;
- exposes membership through `cgroup.procs`;
- membership can migrate;
- `cgroup.procs` itself warns that PID reuse can occur while reading;
- `cgroup.events` exposes populated/frozen state;
- `cgroup.kill` can kill a cgroup subtree with concurrency semantics.

Primary source:
- https://www.kernel.org/doc/html/latest/admin-guide/cgroup-v2.html

Synthesis:

- the shared idea is a provider-scoped **workload/group membership and lifecycle facet**;
- Windows `job_*` remains deep and retains named Job Object lifetime semantics;
- cgroup hierarchy/controllers/delegation are Linux facets;
- Build 002 does not equate a Windows Job Object and an arbitrary cgroup in every dimension.

Because current live Linux access is blocked and Build 002 can pressure provider neutrality without inventing a cgroup ownership policy, cgroup creation/mutation is **research-classified but outside the frozen implementation gate**. Capability discovery records cgroup v2 availability for future measured expansion.

## 10. Service pressure finding

Windows SCM services and systemd units overlap only at a narrow managed-service level.

systemd exposes a stable D-Bus manager/unit API with generic unit states and service-specific facets. Unit types extend beyond services (socket, mount, target, scope, slice, etc.), and systemd uses cgroups for process tracking.

Primary sources:
- https://www.freedesktop.org/software/systemd/man/org.freedesktop.systemd1.html
- https://systemd.io/

Synthesis:

```text
shared core:
  provider-scoped managed service registration
  coarse current state
  exact current-process relation only when provider evidence supports one
  provider-qualified start/stop/restart capability

Windows facet:
  SCM service semantics

Linux facet:
  systemd service-unit semantics
```

systemd unit implementation details are not universalized. Build 002 capability discovery records systemd state; full service actuation is outside the frozen implementation gate.

## 11. Listener/network pressure finding

Linux provides `NETLINK_SOCK_DIAG` / `inet_diag` to query socket state. `/proc/net/tcp*` is documented by the kernel as deprecated in favor of tcp_diag.

Primary sources:
- https://man7.org/linux/man-pages/man7/sock_diag.7.html
- https://www.kernel.org/doc/html/latest/networking/diagnostic/index.html
- https://www.kernel.org/doc/html/latest/networking/proc_net_tcp.html

The Build 001 common listener concept survives research pressure:

```text
transient endpoint/protocol
+ provider-qualified socket witness
+ exact owning process relation when proven
+ observation generation
```

Windows IP Helper bind timestamp remains a Windows facet. Linux socket inode/diag/cookie evidence remains a Linux facet. Port number remains a scalar, never identity.

Listener implementation is outside the bounded Build 002 measured slice because process/file/world semantics already provide the required deep identity pressure and the current locked session prevents live netlink validation.

## 12. Execution-context pressure finding

Windows session/token and Linux execution context are not one thing.

Provider-neutral core:

```text
world/provider
credentials/principal facts
working directory when known
environment at launch when known
provider-native namespace/session facets
```

Windows facet:
- user token;
- logon/session identity;
- WTS session.

Linux facet:
- UID/GID/groups;
- PID/user/mount/cgroup namespace identities;
- distro registration/world;
- cwd/environment where accessible.

Do not translate a Linux UID or PID namespace into a fake Windows session.

## 13. Cross-provider file pressure

Microsoft supports the same Windows-hosted file being reachable from WSL through `/mnt/<drive>`, and Linux files being reachable from Windows through `\\wsl$`.

Primary sources:
- https://learn.microsoft.com/windows/wsl/faq
- https://learn.microsoft.com/windows/wsl/file-permissions

Path translation is **not physical identity proof**.

Build 002 rejects automatic merging of:

```text
C:\x
/mnt/c/x
\\wsl$\Ubuntu-24.04\...
Linux-root /...
```

A future sparse correspondence may be recorded only when both providers independently prove a relation strong enough for the exact operation at hand.

## 14. Capability-descriptor finding

Do not create a giant feature-flag matrix.

Typed descriptors are warranted only where behavior changes semantics:

```text
provider kind / world identity
provider availability
provider-world epoch evidence
process identity/actuation witness kind
file current-witness kind
file persistent-handle capability
exact mutation capabilities
cgroup v2 availability
systemd availability
inotify availability
listener diagnostic capability
```

Errors remain structured:

- `unsupported_by_provider`
- `provider_unavailable`
- `stale`
- `ambiguous`
- `permission_denied`
- `destroyed`
- `native_error`

## 15. SDK pressure classification

| Existing surface | Build 002 classification |
|---|---|
| `machine.inspect` | Windows-default compatibility surface; common world enumeration added separately |
| `session.inspect` | Windows facet |
| `volume.inspect` | Windows facet for Build 002 |
| `process.retain/inspect/wait/terminate` | common semantics survive; provider/world qualification added without breaking Windows default |
| `process.start` | common direct executable + argv survives; WSL transport/provider facet required |
| `job.*` | Windows facet in Build 002; workload common meaning survives research only |
| `file.retain/inspect/read/write` | common physical-file semantics survive with provider facets |
| `file.rename/delete/hardlink` | Windows remains deep; Linux capability-qualified and not falsely promised |
| `network/listener.*` | common concept survives; Linux implementation deferred from bounded slice |
| `service.inspect` | common managed-service meaning survives narrowed; Windows implementation remains; Linux implementation deferred |
| `powershell.invoke` | Windows provider facet |
| `raw.exec` | Windows raw compatibility facet; no invented "Bash objects" |
| `world.cursor/delta/sync` | common coherence/delta semantics survive; provider scope added |
| `state.health` | common kernel state operation |

## 16. Synthesis classification

| Concept | Build 002 research classification |
|---|---|
| machine identity | **narrowed / provider-faceted**: OS execution worlds are explicit |
| BootEpoch | **narrowed / provider-faceted**: portable form is provider-world epoch; Windows BootEpoch preserved |
| process lifetime | **survives unchanged** |
| PID locator rule | **survives unchanged** |
| process exact actuation | **survives with provider facets**: Windows handle vs Linux pidfd |
| parent relation evidence quality | **survives unchanged** |
| physical file concept | **survives unchanged** |
| path as binding | **survives unchanged** |
| file current witness | **provider-faceted** |
| exact retained file write | **survives with provider facets** |
| exact rename/delete | **provider-specific capability** |
| watcher as dirty signal | **survives unchanged** |
| job/workload | **narrower common meaning + provider facets** |
| service | **narrower common meaning + provider facets** |
| listener | **survives with provider facets** |
| execution context | **narrowed / provider-faceted** |
| structured PowerShell | **Windows-only facet** |
| systemd | **Linux-only facet** |
| cgroup | **Linux-only facet** |
| pidfd | **Linux-only facet** |
| Windows Job Object | **Windows-only facet** |
| SCM | **Windows-only facet** |
| cross-provider path equivalence | **rejected universalization** |

## 17. Recommended Build 002 experimental slice

Implement and measure:

1. provider-qualified machine-world descriptors;
2. Ubuntu-24.04 WSL2 provider bridge in owner context;
3. native Linux helper, cross-published from the existing .NET toolchain;
4. Linux provider capability probe;
5. provider-world epoch evidence;
6. Linux process retain/start/inspect/wait/terminate using `/proc` + pidfd;
7. Linux physical file retain/inspect/read/exact-write using statx and optional file handles;
8. conservative file continuity when strong handle evidence is unavailable;
9. provider-qualified deltas and `world.sync`;
10. provider-aware Program Host SDK;
11. common contract and hostile tests that do not weaken Windows;
12. measured cross-provider acceptance only when the real owner WSL context is available.

Research-only / not required for Build 002 measured gate:

- cgroup creation/mutation;
- systemd service mutation;
- Linux listener implementation;
- fanotify provider;
- broad telemetry;
- cross-provider identity merge.

This is the smallest slice that materially pressure-tests provider-neutral identity, exact actuation, recovery, execution context, and local programmability without turning Build 002 into Build 004 or a Linux feature tour.
