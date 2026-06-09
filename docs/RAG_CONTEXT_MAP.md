# RAG Context Map

This document is an optional fallback routing map for humans and AI agents. Use it when a query spans multiple backend docs or when metadata/path filters do not make the right source obvious.

It is not a DDD bounded context map. Domain ownership lives in [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md).

## Search Keywords

`RAG context map`, `docs routing`, `documentation routing`, `which docs to read`, `AI context`, `context selection`, `smallest relevant docs`, `backend docs map`, `documentation index`, `source of truth routing`

## Routing Rules

- Do not read this file for every RAG query.
- Start with direct retrieval using source metadata, path filters, and precise query terms.
- Read this file only when the question spans multiple backend docs or when the right doc is unclear.
- Do not read every linked doc by default.
- Pick the smallest matching row, then inspect code if needed.
- Prefer source-of-truth docs over Vault or personal notes.
- Use `Vault/` only when the user asks about reasoning history, trade-offs, rejected designs, or learning notes.

## Topic To Document Map

| Ask about | Start with | Then read if needed |
| --- | --- | --- |
| High-level backend architecture | [Architecture](../ARCHITECTURE.md) | [Dependency Rules](architecture/DEPENDENCY_RULES.md), [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| Current work protocol, whether to edit, verification, migrations | [Working Protocol](process/WORKING_PROTOCOL.md) | [Documentation Rules](process/DOCUMENTATION_RULES.md) |
| How backend docs should be structured for RAG/search | [Documentation Rules](process/DOCUMENTATION_RULES.md) | this file |
| Domain ownership, entity belongs to which bounded context | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) | [Dependency Rules](architecture/DEPENDENCY_RULES.md) |
| Layer dependency, repository, DbContext, application/domain/infrastructure boundary | [Dependency Rules](architecture/DEPENDENCY_RULES.md) | [Architecture](../ARCHITECTURE.md) |
| Route prefixes, API surface, tablet vs management vs auth vs IoT API | [API Surface Rules](api/API_SURFACE_RULES.md) | [Naming Rules](process/NAMING_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Authentication endpoints, forgot/reset/change password, current account routes | [API Surface Rules](api/API_SURFACE_RULES.md) | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Admin-created account onboarding, invitation link, accept invitation, temporary password fallback | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Role policy, scoped RBAC, SystemAdmin/Manager/Staff/Technician/OrgAdmin | [Authorization Rules](api/AUTHORIZATION_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) |
| Naming conventions for entities, fields, APIs, application use cases | [Naming Rules](process/NAMING_RULES.md) | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| EF Core indexes, soft delete, unique constraints, snapshots, partitioning | [Data Modeling Rules](data/DATA_MODELING_RULES.md) | [JSON Field Rules](data/JSON_FIELD_RULES.md), [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
| JSONB fields, payloads, snapshots, robot parameters, schema versions | [JSON Field Rules](data/JSON_FIELD_RULES.md) | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| Idempotency, retry fields, dead letters, callback deduplication | [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| Tenant isolation, Organization/Store/Kiosk scope, override hierarchy | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) | [Authorization Rules](api/AUTHORIZATION_RULES.md), [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| Tenant tree, tenant scope lookup, RBAC scope selector | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) | [API Surface Rules](api/API_SURFACE_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Tablet, cloud, edge, payment, MQTT, execution flow | [System Flows](flows/SYSTEM_FLOWS.md) | [IoT Contract](iot/IOT_CONTRACT.md) |
| Exact tablet-edge-cloud API/message contract | [IoT Contract](iot/IOT_CONTRACT.md) | [System Flows](flows/SYSTEM_FLOWS.md), [API Surface Rules](api/API_SURFACE_RULES.md) |
| Local edge runtime database design | [Local Edge Runtime ERD](iot/LOCAL_EDGE_RUNTIME_ERD.md) | [IoT Contract](iot/IOT_CONTRACT.md), [Data Modeling Rules](data/DATA_MODELING_RULES.md) |

## Common Query Hints

| Query contains | Useful filters or docs |
| --- | --- |
| `auth`, `login`, `forgot password`, `reset password`, `refresh token` | [API Surface Rules](api/API_SURFACE_RULES.md), section `Authentication And Password Recovery APIs` |
| `invitation`, `accept invitation`, `admin creates account`, `temporary password`, `CreateInvitation`, `SendInvitationEmail` | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) |
| `management accounts`, `role scope`, `RBAC`, `policy` | [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `store`, `organization`, `kiosk`, `tenant scope` | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md), [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| `order paid`, `ready for execution`, `refund required`, `edge offline` | [System Flows](flows/SYSTEM_FLOWS.md), [IoT Contract](iot/IOT_CONTRACT.md) |
| `soft delete`, `unique index`, `DeletedAt IS NULL` | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| `PayloadJson`, `SnapshotJson`, `ConfigJson`, `JSONB` | [JSON Field Rules](data/JSON_FIELD_RULES.md) |
| `SyncEventInbox`, `NextRetryAt`, `LockedUntil`, `dead letter` | [Idempotency and Retry Rules](data/IDEMPOTENCY_RETRY_RULES.md) |
| `ProductVariant`, `MenuItem`, `KioskRecipeExecutionProfile` | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md), then owning context docs/code |
| `local edge db`, `runtime table`, `ExecutableOrder`, `RobotExecutionQueue` | [Local Edge Runtime ERD](iot/LOCAL_EDGE_RUNTIME_ERD.md) |

## Related Docs

- [Documentation Rules](process/DOCUMENTATION_RULES.md)
- [Working Protocol](process/WORKING_PROTOCOL.md)
- [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md)
- [API Surface Rules](api/API_SURFACE_RULES.md)
