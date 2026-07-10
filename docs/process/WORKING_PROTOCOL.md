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
- assemble the smallest relevant context from the task/module instead of reading all docs or all code;
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

## Working With Other AI Models

Other models such as Gemini can be used for implementation, audit, and checklist work, but their output must be reviewed against this repository's rules before being treated as accepted.

Rules learned from previous collaboration:

- Give Gemini explicit scope, stop conditions, and non-goals.
- Ask for an audit/plan first when the work touches architecture, validation, tenant isolation, lifecycle, retry, or persistence.
- For implementation tasks, state clearly:
  - no EF migrations unless explicitly requested;
  - preserve existing public routes unless explicitly requested;
  - do not add dynamic permission entities, generic repositories, or broad service layers unless the plan says so;
  - build after code changes;
  - report whether migrations were created.
- After Gemini finishes, review `git diff` and code behavior directly. Do not rely only on its summary.
- Do not infer who made a change from `git diff` alone. If package versions, broad file moves, or unrelated edits appear, identify the source before writing a model-specific failure note.
- Treat "build succeeded" as a compile check, not design approval.
- If Gemini claims "warning-free", verify the build output when warnings matter.
- If Gemini creates local audit/checklist files in `.project-memory`, keep them only while they are actively useful.
- Promote durable rules into `docs/`; promote reasoning, trade-offs, and deferred ideas into `../Vault/`; delete completed temporary notes from `.project-memory`.
- When Gemini proposes large foundation work, separate:
  - short-term tasks that can be done in hours/days;
  - long-term topics that need business use cases or integration details.
- Do not let another model implement deferred topics simply because they are architecturally valid.

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

- [Documentation Routing Map](../DOCUMENTATION_ROUTING_MAP.md) when the right backend doc is unclear.
- [Documentation Rules](DOCUMENTATION_RULES.md) for RAG-friendly document structure.
- [Architecture](../../ARCHITECTURE.md) for high-level architecture.
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md) for domain ownership.
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md) for layer boundaries.
- [Naming Rules](NAMING_RULES.md) for naming conventions.
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md) for persistence and ERD checks.
- [System Flows](../flows/SYSTEM_FLOWS.md) for the flow index, then the matching flow-specific document.
- [IoT Contract](../iot/IOT_CONTRACT.md) for tablet-edge-cloud flow.

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
dotnet build IceBot.slnx
```

Unit tests run without external infrastructure:

```powershell
dotnet test tests\IceBot.UnitTests\IceBot.UnitTests.csproj
```

PostgreSQL and MinIO integration tests are opt-in and require Docker:

```powershell
$env:ICEBOT_RUN_INTEGRATION_TESTS='true'
dotnet test tests\IceBot.IntegrationTests\IceBot.IntegrationTests.csproj
```

Without `ICEBOT_RUN_INTEGRATION_TESTS=true`, integration tests are discovered and skipped without starting containers.

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
- [Documentation Routing Map](../DOCUMENTATION_ROUTING_MAP.md)
- [Architecture](../../ARCHITECTURE.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Naming Rules](NAMING_RULES.md)
