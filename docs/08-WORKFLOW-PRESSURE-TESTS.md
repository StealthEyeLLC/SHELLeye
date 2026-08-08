# 08 — Workflow Pressure Tests

Status: **Canonical acceptance/architecture pressure tests**  
Baseline: **2026-08-08**

These workflows exist to attack the architecture before breadth is built. They are not demonstrations designed to make SHELLeye look good; they are cases that should expose false identity, hidden lifecycle coupling, event-gap assumptions, and text-shell fallback dependence.

## 1. Persistent server / kernel-death workflow

### Goal

Prove that a long-running workload and its meaningful machine concepts are not owned by the SHELLeye kernel process.

### Flow

```text
create disposable NTFS workspace
→ create config file_*
→ create named job_*
→ start deterministic HTTP server proc_* under native Job Object
→ server spawns child proc_*
→ capture output through restart-independent spool
→ wait for listener_*
→ record output/world cursors
→ hard-kill SHELLeye kernel only
→ server + child remain running
→ endpoint continues responding
→ server emits output during gap
→ restart kernel
→ detect same BootEpoch
→ reopen named native Job Object
→ recover same job_*
→ recover same live proc_* incarnations by sequence + creation witnesses
→ recover same physical file_*
→ resume output after old cursor
→ reconcile listener/service/session state
→ continue operation
```

### Required result

- workload survives kernel death;
- same `job_*` recovered;
- same still-live `proc_*` recovered exactly;
- same physical `file_*` recovered exactly;
- output produced during gap remains available within bounded spool retention;
- listener current state is rediscovered without fabricating socket continuity across the observation gap;
- no full-machine historical replay is invented.

### Failure examples

- server dies because kernel pipe closes;
- job disappears because kill-on-close was set;
- process recovered merely by PID without sequence/creation verification;
- listener silently rebound by port number;
- output after gap unavailable because kernel owned the only pipe reader.

## 2. Process identity attack

### Goal

Prove that `proc_*` is one native process lifetime and cannot drift onto a reused PID.

### Flow

```text
launch fixture A
→ proc_A / PID P / sequence S1 / creation T1
→ launch same executable fixture B simultaneously
→ proc_B distinct
→ exit A
→ relaunch same executable C
→ proc_C distinct
→ feed resolver hostile row PID P / sequence S2 / creation T2
→ attempt terminate(proc_A)
```

### Required result

```text
proc_A → terminal/destroyed
proc_B → current and distinct
proc_C → current and distinct
hostile reused-PID row → not proc_A
terminate(proc_A) → destroyed/stale result
replacement process untouched
```

Hard metrics:

```text
false process rebounds = 0
wrong process mutations = 0
```

### Why deterministic simulation is required

Real PID reuse timing is nondeterministic and should not make the test suite probabilistic. Build 001 may additionally stress real reuse, but the resolver must have a deterministic adversarial fixture that injects the exact condition it must reject.

## 3. Parent-process identity attack

### Goal

Prove that reported parent PID is not treated as eternal parent identity.

### Flow

```text
observe child with reported parent PID P
→ original parent exits
→ current process table later contains unrelated process at P
→ resolve child.parent
```

### Required result

If SHELLeye did not observe enough exact lineage evidence, the edge remains `reported`/`unknown`, not an exact `PARENT_OF` relation to the unrelated current process.

## 4. Service restart / process replacement workflow

### Goal

Prove service identity and process identity are separate.

### Generic semantic case

```text
svc_7 state running
→ current_process proc_A
→ service restarts or provider observes process replacement
→ proc_A exits
→ current_process proc_B
→ svc_7 remains svc_7
```

### Build 001 practical case

Build 001 does not need to create/restart a Windows service solely for acceptance. It must inspect at least one existing service and resolve its current PID to an exact process when the SCM state makes that PID valid.

The full restart mutation case can be implemented once a deterministic service fixture is justified.

### Required ontology

- service state/config revision belongs to `svc_*`;
- native service process belongs to `proc_*`;
- shared-service host processes are permitted;
- workers spawned by the service do not become the service concept.

## 5. SHELLeye workload restart workflow

### Goal

Prove the higher-level `job_*` concept can remain meaningful while individual worker processes change.

### Flow

```text
job_18
→ root worker proc_A
→ listener_A
→ stop proc_A
→ proc_A exits; listener_A closes
→ start replacement proc_B into same logical/native job
→ listener_B opens
```

### Required result

```text
job_18 remains current
proc_A terminal
a new proc_B allocated
listener_A closed
a new listener_B allocated
```

No process/listener rebound is needed to preserve meaningful workload continuity.

## 6. File rename workflow

### Goal

Prove path is not identity.

### Flow

```text
create C:\...\alpha.txt
→ file_A = volume V + file ID F
→ rename alpha.txt to beta.txt
→ query file identity
```

### Required result

```text
same file_A
same physical file ID
new path binding
revision may change as metadata changes
```

An old path lookup can fail while `file_A` remains current.

## 7. Directory rename workflow

### Goal

Prove descendants are not logically destroyed merely because ancestor path text changes.

### Flow

```text
create dir_A\file_A
→ rename dir_A to dir_B
→ reconcile
```

### Required result

- same physical directory concept where filesystem identity confirms it;
- same physical child file concept;
- path bindings update;
- watcher event text is not itself treated as final identity proof.

## 8. File atomic-replacement attack

### Goal

Prevent an old retained file handle from targeting a replacement at the same path.

### Flow

```text
file_A at C:\...\config.json
→ create replacement temp file_B
→ atomic replace path so file_B occupies config.json
→ attempt write(file_A)
```

### Required result

```text
file_A → destroyed/replaced
file_B → new concept at old path
write(file_A) → stale/destroyed
file_B remains untouched by old handle operation
```

Hard metric:

```text
wrong file mutations = 0
```

## 9. File delete/recreate attack

### Goal

Prevent false continuity based on same path or same content.

### Flow

```text
file_A exists at path P with content X
→ delete file_A
→ create new physical file at P with same content X
→ resolve old file_A
```

### Required result

Old `file_A` stays destroyed; replacement receives a new concept even though path/content match.

## 10. Hard-link workflow

### Goal

Prove one physical file can have multiple paths.

### Flow

```text
file_A at path P1
→ create hard link P2
→ inspect by P1 and P2
```

### Required result

Both path bindings resolve to the same physical file identity. Removing one hard link does not imply the physical object is destroyed while another link remains.

## 11. Listener/port reuse attack

### Goal

Prove endpoint reuse is not listener identity.

### Flow

```text
server A proc_A binds 127.0.0.1:P
→ retain listener_A
→ stop server A
→ wait listener_A closed
→ server B proc_B binds same 127.0.0.1:P
→ inspect endpoint
```

### Required result

```text
listener_A remains closed
listener_B is new
listener_B owner = exact proc_B
```

The number `P` is only endpoint data.

## 12. Filesystem watcher-overflow/gap workflow

### Goal

Prove watcher events do not become a fake event ledger.

### Flow

```text
retain watched directory/file set
→ create enough churn or simulate watcher overflow/provider gap
→ watcher signals error/dirty state
→ world.sync(scope)
→ current physical identities/revisions queried
```

### Required result

- SHELLeye reconciles current truth;
- reports that an observation gap occurred if relevant;
- does not manufacture exact missing event order/history;
- retained file handles remain exact only where current identity supports them.

## 13. BootEpoch hostile transition

### Goal

Prove transient machine objects cannot accidentally cross reboot.

### Deterministic Build 001 resolver simulation

```text
BootEpoch A contains proc_A / listener_A / job_A
→ simulate persisted restart record with BootEpoch B
→ attempt resolve old transient handles
```

### Required result

- old `proc_*` terminal/destroyed;
- old listener/connection/terminal-run state terminal/destroyed;
- old native Job Object cannot be reopened across boot and job lifetime ends;
- services/tasks/files/volumes are reconciled independently as persistent domains;
- no transient rebinding into BootEpoch B.

A real reboot campaign belongs in later hardening once Build 001's deterministic resolver gate is stable.

## 14. PowerShell provider death workflow

Only applies if Build 001 implements a separate PowerShell provider process.

### Flow

```text
kernel + fixture alive
→ invoke structured PowerShell successfully
→ retain no fake permanent PSObject identity
→ kill PowerShell provider
→ kernel continues observing OS concepts
→ restart provider with new provider epoch
→ invoke structured query again
```

### Required result

- `proc_*`, `job_*`, `file_*`, `svc_*`, listener current state remain unaffected;
- runspace-local variables/functions/providers that actually died are reported lost/reinitialized;
- provider death does not masquerade as OS object death.

## 15. Raw-shell escape-hatch workflow

### Goal

Prove high-level semantics do not remove ordinary machine capability.

### Flow

```text
raw.exec("whoami")
```

or equivalent direct raw execution.

### Required result

Raw stdout/stderr/exit status available.

This proves only the escape hatch; it does **not** count as proof of SHELLeye's architecture.

## 16. Structured-PowerShell workflow

### Goal

Prove PowerShell is not just a formatted text shell.

### Flow

```text
powershell.invoke(Get-Process for retained fixture PID)
→ Collection<PSObject>/projected object values
→ compare relevant returned process ID/name to exact SHELLeye proc_*
```

### Required result

- object properties returned before formatting;
- error/warning streams separate;
- no `Format-Table` parsing;
- the PowerShell result is a provider facet/correlation, not the durable process ID.

## 17. Program Host workflow

### Goal

Prove ChatGPT does not need to reason between every machine primitive.

### One invocation

```text
inspect machine/session/volume
→ create directory
→ create/retain config file
→ create named job
→ start fixture under job
→ wait ready output
→ inspect process tree/job membership
→ wait/retain listener
→ launch client
→ wait client exit
→ structured PowerShell query
→ resource sample
→ output delta
→ guarded config mutation
→ file wait + world delta
→ rename same physical file
→ stop exact old worker
→ wait exit/listener close
→ start new worker in same job
→ prove new process/listener concepts
→ service query
→ move file
→ final deltas/output
→ terminate job
→ wait empty/listener absent
→ clean workspace
→ return compact object
```

Required: at least 30 meaningful typed SDK operations and no model round trip between them.

## 18. Concurrent external-writer workflow

### Goal

Prove SHELLeye coexists with other machine actors.

### Flow

```text
retain file_A revision N
→ external process changes/replaces file
→ SHELLeye attempts guarded mutation based on old revision/identity
```

### Required result

- same physical file with changed revision → precondition conflict/stale, unless operation explicitly allows current revision;
- replaced physical file → old file destroyed, mutation rejected;
- no assumption that SHELLeye is sole writer.

Equivalent stale-precondition behavior applies to process/service/native object mutations where the target incarnation can change.

## 19. Cross-substrate future workflow

### Goal

Prove composition without ontology takeover.

```text
CODEeye builds server artifact
→ CODEeye exposes engineering artifact/source/build meaning
→ SHELLeye correlates physical file_* and launches job_/proc_*
→ SHELLeye observes listener/process/file state
→ eyeBROWSE correlates browser_* to its SHELLeye browser proc_*
→ eyeBROWSE drives real browser behavior
→ SHELLeye observes generic machine/network changes
→ CODEeye correlates runtime/build evidence back to engineering concepts
```

Ownership must remain:

```text
CODEeye: build/source/compiler/test meaning
SHELLeye: generic machine/process/file/network meaning
eyeBROWSE: browser/DOM/AX/browser-network meaning
```

No substrate should copy the others' ontology merely to create a link.

## 20. Failure philosophy

The pressure tests intentionally prefer honest loss of continuity over false continuity.

Acceptable outcomes when evidence is insufficient:

```text
stale
destroyed
ambiguous
unknown
inaccessible
```

Unacceptable outcome:

```text
"looks similar, so operate on it anyway"
```

Build 001 is not complete until the deterministic identity-killer suite reports:

```text
false rebounds = 0
wrong-object mutations = 0
```
