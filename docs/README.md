# Backend Docs

This folder contains backend source-of-truth documentation.

Use [RAG Context Map](RAG_CONTEXT_MAP.md) only when the right doc is unclear. For direct work, start from the matching folder below.

## Folder Map

| Folder | Owns |
| --- | --- |
| `architecture/` | Backend architecture boundaries, bounded contexts, dependency rules, multi-tenancy |
| `api/` | API surfaces, authorization, identity onboarding |
| `data/` | Data modeling, JSON fields, idempotency, retry, indexes |
| `flows/` | Backend/system flows |
| `iot/` | Tablet, cloud, edge, local runtime contracts and ERD |
| `process/` | Working protocol, documentation rules, naming rules |

## Key Docs

| Need | Start With |
| --- | --- |
| Route/API ownership | [API Surface Rules](api/API_SURFACE_RULES.md) |
| Role policy and scoped RBAC | [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Internal account onboarding | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) |
| Domain ownership | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| Layer dependencies | [Dependency Rules](architecture/DEPENDENCY_RULES.md) |
| Tenant scope | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) |
| EF/data modeling rules | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| System flow | [System Flows](flows/SYSTEM_FLOWS.md) |
| Tablet/edge/cloud contract | [IoT Contract](iot/IOT_CONTRACT.md) |
| Naming conventions | [Naming Rules](process/NAMING_RULES.md) |

## Related

- [Architecture](../ARCHITECTURE.md)
- [RAG Context Map](RAG_CONTEXT_MAP.md)
