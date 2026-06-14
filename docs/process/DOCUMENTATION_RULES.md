# Documentation Rules

This document defines how backend docs should be written so humans and RAG tools can find the right context without reading every file.

## Search Keywords

`documentation rules`, `docs standard`, `RAG-friendly docs`, `Search Keywords`, `lookup section`, `Related Docs`, `documentation structure`, `source of truth docs`, `routing hints`, `AI context`, `avoid duplicate docs`

## Purpose

Keep docs small, routed, and searchable.

Each doc should answer one ownership question:

- what topic this file owns
- when to read it
- which terms should retrieve it
- where related but separate rules live

Do not duplicate full explanations across docs. Link to the owning doc instead.

Use [RAG Context Map](../RAG_CONTEXT_MAP.md) only when the right backend doc is unclear after direct retrieval, metadata filters, or path filters.

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

- domain names: `Order`, `PaymentTransaction`, `RobotJob`
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
- Keep rejected/future ideas out of source-of-truth docs unless clearly marked.
- Do not put Vault/personal reasoning into backend docs.

## Retrieval Priority

RAG should use a lazy retrieval path:

1. Search specific source-of-truth docs with direct query terms and metadata filters.
2. Narrow by path, source type, document type, or lookup section when the query is ambiguous.
3. Use [RAG Context Map](../RAG_CONTEXT_MAP.md) as a fallback router when the correct doc family is still unclear.
4. Use reranking selectively for hard queries, not as a mandatory fix for weak docs or broad queries.

## Related Docs

- [Working Protocol](WORKING_PROTOCOL.md)
- [RAG Context Map](../RAG_CONTEXT_MAP.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
- [Naming Rules](NAMING_RULES.md)
