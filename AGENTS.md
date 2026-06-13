# AGENTS.md

Operational rules for coding agents working in this repository.

## Source Of Truth

- Architecture decisions: [ARCHITECTURE.md](ARCHITECTURE.md)
- Domain context map: [docs/architecture/BOUNDARY_CONTEXTS.md](docs/architecture/BOUNDARY_CONTEXTS.md)
- Dependency boundaries: [docs/architecture/DEPENDENCY_RULES.md](docs/architecture/DEPENDENCY_RULES.md)
- RAG/docs routing map: [docs/RAG_CONTEXT_MAP.md](docs/RAG_CONTEXT_MAP.md)
- Documentation structure: [docs/process/DOCUMENTATION_RULES.md](docs/process/DOCUMENTATION_RULES.md)

Do not duplicate long architecture explanations here. This file is for execution rules.

## Workspace Role

This is the main implementation repository for IceBot backend work.

- Use this file as the primary operational guide for backend/API/domain/database/auth/payment/order/sync tasks.
- Use `../IceBot-Tools` only as auxiliary tooling: RAG, MCP, code intelligence, docs checks, diagnostics, and local scripts.
- Use `../Vault` for advisory decisions, research, trade-offs, and history. Vault is not implementation truth.
- Use `../Docs` for shared project/business docs when the task needs cross-repo project context.
- The workspace root `AGENTS.md` is only a router/fallback and does not override this file for backend work.

## Documentation Reading

- Treat links as routing hints, not mandatory recursive reads.
- Do not follow every link in every document.
- If a linked file was already read in the current task, do not reopen it unless the user asks, the file may have changed, or a specific section is needed.
- Prefer reading the smallest relevant set of docs, then inspect code.
- When the right backend doc is unclear after direct retrieval or metadata/path filters, use [docs/RAG_CONTEXT_MAP.md](docs/RAG_CONTEXT_MAP.md) as a fallback router.

## Working Workflow

- Use current task context first; do not reread docs/code when the answer is already settled in context.
- For concrete symbols/endpoints/handlers, use Code Intelligence before broad search.
- For rules/flows/contracts, use docs/RAG only when current context is insufficient.
- Make the smallest scoped change, then verify with the narrowest relevant check.
- After meaningful backend code/API/domain changes, run backend preflight as the final check.

## Change Guardrails

- Preserve existing API route contracts unless the user explicitly asks to change them.
- Do not keep backward-compatibility or legacy response/request fields unless the user explicitly asks for compatibility, especially before first deployment.
- Keep changes scoped to the requested work.
- Do not convert a review, challenge, or "why did you do this?" question into code edits. Explain first; wait for an explicit action request before changing files.
- Treat phrases such as "vì sao", "sao không", "có đang ... không", "cân nhắc", "kiểm tra", and "đánh giá" as inspection/explanation by default, not permission to refactor.
- Do not justify broad movement, renaming, or added abstraction only because the build passes. Build success verifies compilation, not design fit.
- State assumptions explicitly when they affect structure, ownership, integration boundaries, or future extensibility. If an assumption is challenged, stop editing and correct the reasoning before proposing more changes.
- Do not hide or dilute a mistaken assumption. Name the wrong assumption, identify the affected files, and separate explanation from proposed remediation.
- When the user asks to inspect or adjust an existing abstraction, preserve it and repair it first. Do not delete it just because it is currently unused or copied from another project.
- Do not perform broad renames, namespace moves, or folder reshuffles unless requested.
- Do not remove files, abstractions, or extension points unless the user explicitly asks for removal, or the file is proven obsolete and the removal is stated as part of the intended fix before editing.
- Do not create EF Core migrations unless the user explicitly asks for migrations.
- Do not use destructive git commands unless explicitly requested.
- Work with existing uncommitted changes; do not revert user changes.

## Domain And Application Rules

- Place new domain concepts in the owning bounded context.
- Keep context-specific enums inside that context.
- Put only genuinely shared primitives in `Domain.Common`.
- Prefer ids and snapshots across contexts instead of large navigation graphs.
- Do not introduce generic repository/service/controller layers for domain workflows.
- Use EF Core `DbContext` as the default unit of work; keep repositories thin when they exist.
- Repository abstractions should support handler-composed queries and focused persistence operations. They must not become CRUD service layers with hidden business rules.
- Keep actor concerns in WebAPI/auth, not in Application folder organization.

## Refactor Checklist

- Read the current implementation and search all usages before changing an abstraction.
- Identify whether the request is to repair, reshape, or remove. Default to repair/reshape when removal is not explicit.
- Before moving files, renaming public/internal concepts, or introducing a new abstraction, verify that the user asked for implementation rather than explanation. If not explicit, provide a short recommendation only.
- Prefer the smallest change that resolves the current mismatch. Do not add future-facing infrastructure until there is a present use case or the user asks for it.
- Keep class names, file names, namespaces, and `using` references consistent.
- Update interface and implementation signatures together.
- Update DI registrations when service contracts change.
- Scan for stale identifiers before finishing; prefer `rg`.
- Re-run build after code changes.

## Verification

Preferred final check in the full workspace after meaningful backend code/API/domain changes:

```powershell
cd ..\IceBot-Tools
python .\backend-preflight\commands\check_backend.py
```

Fallback/direct compile check:

```powershell
dotnet build src\IceBot.slnx
```

Use focused lookup or docs tools during investigation. Do not run backend preflight at the start of a task or for design-only discussion.

Do not run build for documentation-only changes unless the user explicitly asks for verification.

For EF model checks, prefer design-time commands that do not mutate the database unless the user asked for migration/database changes.

If a required tool is unavailable, report exactly what could not be verified.

## Bulk And Distributed Workflow Rules

- Transaction behavior must be explicit.
- If a bulk requirement says item-level atomicity, rollback only the failed item, commit successful items, and return partial-failure details.
- Public commands that can be retried need idempotency behavior.
- Payment callbacks, sync ingestion, robot events, and stock movements should use typed retry/idempotency fields rather than parsed JSON state.

## Ambiguity

Make reasonable assumptions when they are low risk and consistent with the existing codebase. Ask for clarification only when the choice would change public contracts, data model ownership, migrations, or workflow semantics.

## Technical Challenge And Decision Ownership

- Do not simply agree with the user's architectural or design statement. Evaluate it against the current codebase, stated requirements, and likely future constraints.
- When the user's proposal is valid but not the only good option, present the trade-offs and at least one stronger or simpler alternative when one exists.
- Separate facts, assumptions, and recommendations. State which parts are proven from code and which parts are inferred.
- For design choices with meaningful trade-offs, provide options and a recommendation, then let the user decide which option to implement.
- Do not implement a debatable architectural choice immediately after discussing it unless the user explicitly asks to apply that specific option.
- If the user challenges a previous decision, reassess it on technical grounds instead of defending the prior change. Acknowledge mistakes directly, but still explain any parts of the previous approach that were technically reasonable.
