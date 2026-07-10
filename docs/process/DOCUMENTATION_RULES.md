# Documentation Rules

This document defines how backend docs should be written so humans and RAG tools can find the right context without reading every file.

## Search Keywords

`documentation rules`, `docs standard`, `RAG-friendly docs`, `Search Keywords`, `lookup section`, `Related Docs`, `documentation structure`, `source of truth docs`, `routing hints`, `AI context`, `avoid duplicate docs`

## Purpose

Keep backend docs small, current, routed, and searchable.

Backend docs are operational source of truth. They prioritize information needed to implement, integrate, operate, or verify the behavior that runs now:

- current contracts, invariants, routes, payloads, states, and ownership boundaries
- commands and procedures a developer or operator must execute
- current failure, retry, security, and verification behavior

Keep concise rationale, examples, future constraints, and implementation guidance when readers need them to apply the contract correctly or avoid a known unsafe interpretation. Move extended discussion history, option comparison, rejected alternatives, and standalone decision records to the smallest owning `Vault/Decisions`, `Vault/Discussions`, or `Vault/Evolution` note. Backend docs may link to that note, but must remain understandable without reading Vault.

Do not document implementation work merely because it was performed. Add documentation only when readers need the resulting contract or procedure.

Each doc should answer one ownership question:

- what topic this file owns
- when to read it
- which terms should retrieve it
- where related but separate rules live

Do not duplicate full explanations across docs. Link to the owning doc instead.

Use [Documentation Routing Map](../DOCUMENTATION_ROUTING_MAP.md) only when the right backend doc is unclear after direct retrieval, metadata filters, or path filters.

## Standard Shape

Use this shape for new backend docs:

```text
# Document Name

Short ownership statement.

## Search Keywords

`keyword`, `related term`, `route`, `entity`, `workflow`

## Purpose / Rules / Lookup / Flow

The actual content owned by the doc.

## Related Docs

- Other related doc name and path
```

`Search Keywords` should be near the top, but not inside the opening paragraph. This keeps overview chunks narrow while still giving RAG a clean keyword chunk.

## Search Keyword Rules

Include keywords that a team member or AI agent is likely to ask for:

- domain names: `Order`, `PaymentTransaction`, `RobotArtifact`, `EdgeCommand`
- route names: `/api/v1/authentication/login`, `/management/accounts`
- workflow phrases: `forgot password`, `edge command pull`, `payment callback`
- common synonyms: `auth`, `authentication`, `login`, `external login`
- policy names: `scoped RBAC`, `soft delete unique index`, `jsonb`

Avoid keyword dumping.

Do not include unrelated hot terms just to make a doc appear in more searches. That makes RAG worse.

## Lookup Sections

For route maps, policies, entities, or table groups, prefer compact lookup tables.

Good lookup sections:

- API surface by client/route.
- Bounded context by namespace/entity.
- Authorization policy by role.
- Local edge table by runtime group.
- JSON field by role.

These tables help humans scan and help RAG retrieve exact chunks.

## Link Rules

Links are routing hints, not required reading order.

Use related docs to point to ownership boundaries:

- API routes -> `API_SURFACE_RULES.md`
- authorization -> `AUTHORIZATION_RULES.md`
- bounded contexts -> `BOUNDARY_CONTEXTS.md`
- persistence/indexes -> `DATA_MODELING_RULES.md`
- JSON columns -> `JSON_FIELD_RULES.md`
- sync/idempotency/retry -> `IDEMPOTENCY_RETRY_RULES.md`
- tablet/edge/cloud contract -> `IOT_CONTRACT.md`
- deployment, diagnostics, observability, and smoke tests -> `operations/`

Do not copy full route maps, entity lists, or rules from the linked doc unless the current doc owns that rule.

## RAG-Friendly Writing

- Keep the first paragraph narrow.
- Put search terms in `Search Keywords`.
- Use precise section headings.
- Prefer tables for lookup data.
- Prefer specific lookup sections and metadata-friendly terms over generic overview prose.
- Keep extended decision history, rejected alternatives, and unrelated proposals out of backend source-of-truth docs.
- Describe the current rule directly. Keep “because”, examples, and future constraints when removing them would make the rule ambiguous or easier to misuse.
- Remove stale behavior when the implementation changes; do not preserve it as history in the contract document.
- Avoid duplicating the same section or rule. Keep one owner and link to it.

## Change Workflow

When backend behavior changes:

1. Update only the owning contract or procedure.
2. Remove superseded behavior from backend docs.
3. Keep locally necessary rationale with the contract; record broader decision history and trade-offs in Vault when worth preserving.
4. Check headings, links, duplicated rules, stale future language, and `git diff --check`.

For a documentation cleanup or restructure, preserve the current uncommitted version before editing and compare content coverage afterward. Intentional removals are allowed when the user explicitly requests cleanup, condensation, or deletion; report what was removed or moved.

## Retrieval Priority

RAG should use a lazy retrieval path:

1. Search specific source-of-truth docs with direct query terms and metadata filters.
2. Narrow by path, source type, document type, or lookup section when the query is ambiguous.
3. Use [Documentation Routing Map](../DOCUMENTATION_ROUTING_MAP.md) as a fallback router when the correct doc family is still unclear.
4. Use reranking selectively for hard queries, not as a mandatory fix for weak docs or broad queries.

## Related Docs

- [Working Protocol](WORKING_PROTOCOL.md)
- [Documentation Routing Map](../DOCUMENTATION_ROUTING_MAP.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
- [Naming Rules](NAMING_RULES.md)
