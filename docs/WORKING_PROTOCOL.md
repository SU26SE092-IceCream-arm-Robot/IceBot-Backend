# Working Protocol

This project is still in discovery and pre-deployment. The domain model, API shape, and integration boundaries are being refined while the team learns from real robot, payment, tablet, and edge behavior.

This document defines how to work during this phase. It is not a frozen engineering process.

## Search Keywords

`working protocol`, `workflow`, `question versus action`, `apply`, `inspect`, `review`, `refactor rules`, `change scope`, `documentation rules`, `verification`, `done criteria`, `pre-deployment`, `do not create migrations`, `build command`, `AI agent workflow`

## Current Phase

Default assumptions:

- Architecture is directional, not fully frozen.
- Domain concepts should be clear, but implementation details may still change.
- Prefer small, reversible changes over broad reshuffles.
- Pre-deployment API compatibility is not required unless explicitly requested.
- Do not create EF Core migrations unless explicitly requested.

## Question Versus Action

Treat these as inspection or design discussion by default:

- "kiem tra"
- "danh gia"
- "vi sao"
- "sao khong"
- "co dang ... khong"
- "can nhac"
- "huong sua"

For those requests:

- inspect the current code/docs first;
- separate facts, assumptions, and recommendations;
- do not edit files unless the user explicitly asks to apply.

Treat these as action requests:

- "ap dung"
- "sua"
- "them"
- "xoa"
- "refactor"
- "tao file"
- "cap nhat doc"

For action requests, implement the smallest change that matches the decision.

## Decision Ownership

The assistant should challenge weak assumptions and present trade-offs. The user decides which option to implement.

When a proposal has multiple reasonable options:

- explain the options briefly;
- recommend one;
- wait for a clear implementation request if the choice changes architecture, data model, public API, or workflow semantics.

Do not simply agree with a statement if there is a stronger or safer alternative.

## Change Scope

Before editing:

- read the current implementation;
- search usages with `rg`;
- identify whether the request is repair, reshape, or removal;
- avoid broad renames, namespace moves, or folder reshuffles unless requested.

During editing:

- keep changes scoped to the requested concept;
- preserve existing abstractions unless removal is explicit;
- update interface and implementation signatures together;
- update DI and docs when contracts change;
- do not add future-facing infrastructure without a current use case.

## Refactor Rules

Build success does not prove design fit.

Do not justify broad movement, deletion, or new abstraction only because the project compiles.

When refactoring copied code from older projects:

- keep mature pieces that match the current domain;
- remove or reshape legacy assumptions that do not match IceBot;
- do not hide assumptions;
- state which parts were kept, changed, or deferred.

## API And Contract Rules

Before first deployment:

- do not keep legacy compatibility fields unless requested;
- prefer clean contracts over backward compatibility;
- route names should be business-facing and understandable by the team;
- application use cases may be action-oriented;
- WebAPI routes should stay resource/business-oriented.

After public or deployed clients exist, compatibility decisions must be explicit.

Detailed API naming rules live in [Naming Rules](NAMING_RULES.md).

## Documentation Rules

Docs should reduce repeated reasoning, not duplicate long explanations.

Use the project documentation index first when the task spans multiple repos or document areas:

- [Project Documentation Index](../../Docs/README.md)

Do not read all docs by default. Read the smallest relevant set, usually 1-3 files, then inspect code as needed.

Links are routing hints, not mandatory recursive reads. If a linked file was already read in the current task, do not reopen it unless the user asks, the file may have changed, or a specific section is needed.

Use:

- [RAG Context Map](RAG_CONTEXT_MAP.md) when the right backend doc is unclear.
- [Documentation Rules](DOCUMENTATION_RULES.md) for RAG-friendly document structure.
- [Architecture](../ARCHITECTURE.md) for high-level architecture.
- [Boundary Contexts](BOUNDARY_CONTEXTS.md) for domain ownership.
- [Dependency Rules](DEPENDENCY_RULES.md) for layer boundaries.
- [Naming Rules](NAMING_RULES.md) for naming conventions.
- [Data Modeling Rules](DATA_MODELING_RULES.md) for persistence and ERD checks.
- [System Flows](SYSTEM_FLOWS.md) for backend/tablet-edge-cloud flows.
- [IoT Contract](IOT_CONTRACT.md) for tablet-edge-cloud flow.

The project-level `Vault/` folder is a personal reasoning notebook, not implementation truth. Use it only as background context unless a decision has been promoted into `Docs/` or repository docs.

Do not load `Vault/` by default. Use it only when the user asks about reasoning history, trade-offs, rejected designs, unresolved ideas, or why a decision was considered.

When changing code that affects contracts, domain ownership, or data model rules, update the relevant doc.

Do not run build for documentation-only changes unless explicitly requested.

Do not run RAG ingest automatically after documentation-only changes unless explicitly requested. RAG ingest mutates the local vector database and can be slow on the current machine because embedding runs in small batches. Instead, report the manual command:

```powershell
cd ..\IceBot-Tools
python .\rag\commands\ingest_docs.py
python .\rag\commands\ingest_code.py
```

## Verification

For code changes, run:

```powershell
dotnet build src\IceBot.slnx
```

For documentation-only changes, no build is needed.

For EF Core:

- do not create migrations unless requested;
- prefer non-mutating design-time checks when possible;
- never update the database unless requested.

## Done Criteria

A change is done when:

- code compiles, unless the change is documentation-only;
- stale identifiers/usages were scanned;
- docs were updated if the decision changed architecture, contract, data model, or naming;
- any skipped verification is stated explicitly;
- remaining warnings or risks are reported.

## Related Docs

- [Documentation Rules](DOCUMENTATION_RULES.md)
- [RAG Context Map](RAG_CONTEXT_MAP.md)
- [Architecture](../ARCHITECTURE.md)
- [Boundary Contexts](BOUNDARY_CONTEXTS.md)
- [Dependency Rules](DEPENDENCY_RULES.md)
- [Naming Rules](NAMING_RULES.md)
