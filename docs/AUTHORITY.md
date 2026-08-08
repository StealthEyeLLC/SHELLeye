# SHELLeye Repository / Project Authority

Status: **Canonical**  
Owner: **StealthEyeLLC**

## Owner authority

StealthEyeLLC owns the SHELLeye repository and project direction.

The owner explicitly supplied a GitHub authority instruction stating that the connected GitHub app gives ChatGPT full GitHub administration and is to be used extremely aggressively for repository work.

During the 2026-08-08 setup pass, the connected GitHub integration reported admin/maintain/push/pull/triage permission on:

```text
StealthEyeLLC/SHELLeye
```

Repository state at discovery:

```text
visibility: public
default branch: main
initial size: 0 / empty repository
```

The repository already existed, so no repository-creation workaround was needed.

## Current authorization boundary

The owner explicitly authorized this architecture/research/repository-setup pass.

This pass may:

- inspect current StealthEye sibling architecture patterns;
- inspect the live STEALTHEYELLC machine;
- research current technical evidence;
- create/update SHELLeye repository documentation;
- create repository hygiene files;
- create Build 001 GitHub issues;
- canonicalize the specification on `main`.

This pass does **not** authorize SHELLeye product implementation.

Specifically, this pass must not:

- build the production kernel;
- implement production providers;
- implement the Program Host;
- create active SHELLeye runtime services/tasks;
- create product source code merely as scaffolding;
- claim any Build 001 acceptance gate passed.

Implementation begins in a separate owner-directed implementation tab.

This is task scope, not an additional SHELLeye permission/guardrail architecture.

## Canonical authority order

The project specification is interpreted in this order:

1. `README.md` — orientation/status;
2. numbered documents in `docs/` in numerical order;
3. `docs/AUTHORITY.md` — repository/project authority and current task boundary;
4. Build 001 GitHub issues — executable work tracking subordinate to the canonical documents.

The five exhaustive project constraints live in `00-CHARTER.md`.

If implementation evidence invalidates a frozen decision:

- update the relevant canonical document;
- update `06-DECISIONS.md`;
- do not leave a contradictory second specification beside it.

## Sibling-project precedent

SHELLeye may reuse proven **principles** from eyeBROWSE/CODEeye—persistent world state, logical IDs distinct from provider IDs, delta-first operation, local Program Host, conservative continuity, canonical-document discipline—but their ontologies are not automatically authoritative for the OS domain.

The SHELLeye process/file/job/listener lifecycle model is intentionally derived from Windows/OS evidence, including cases where it differs from CODEeye.

## Research authority

`05-RESEARCH-BASELINE.md` is the canonical synthesis of the 2026-08-08 research pass.

External research sources are evidence; they do not become independent project specifications. Architectural decisions live in `01-ARCHITECTURE.md` and `06-DECISIONS.md`.

## Experiment authority

Experiments are noncanonical by default.

A successful experiment may influence the project only after its result is deliberately promoted into the relevant canonical document and decisions record.

This rule applies especially to:

- PowerShell hosting topology;
- ETW/USN experiments;
- native helper prototypes;
- alternative process identity witnesses;
- cross-platform provider experiments;
- transport/serialization benchmarks.

## Build status authority

At the end of this setup pass:

```text
architecture: canonicalized
repository: initialized/canonicalized
Build 001 issues: created/planned
product implementation: NOT STARTED
Build 001 acceptance: NOT RUN
```

Do not create `09-BUILD-001-RESULTS.md` or mark milestone issues complete until the implementation tab has executed and measured the gates.
