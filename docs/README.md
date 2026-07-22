# Backend Docs

This folder contains backend source-of-truth documentation.

Use [Documentation Routing Map](DOCUMENTATION_ROUTING_MAP.md) only when the right doc is unclear. For direct work, start from the matching folder below.

## Folder Map

| Folder | Owns |
| --- | --- |
| `architecture/` | Backend architecture boundaries, bounded contexts, dependency rules, multi-tenancy |
| `api/` | API surfaces, authorization, identity onboarding |
| `data/` | Data modeling, JSON fields, idempotency, retry, indexes |
| `flows/` | Backend/system flows |
| `iot/` | Tablet, cloud, edge, local runtime contracts and ERD |
| `operations/` | Deployment config, observability, diagnostics, smoke tests |
| `process/` | Working protocol, documentation rules, naming rules, handoff checklists |

## Key Docs

| Need | Start With |
| --- | --- |
| Route/API ownership | [API Surface Rules](api/API_SURFACE_RULES.md) |
| Internal management route catalog | [Management API Surface](api/MANAGEMENT_API_SURFACE.md) |
| Role policy and scoped RBAC | [Authorization Rules](api/AUTHORIZATION_RULES.md) |
| Internal account onboarding | [Identity Onboarding Rules](api/IDENTITY_ONBOARDING_RULES.md) |
| Domain ownership | [Boundary Contexts](architecture/BOUNDARY_CONTEXTS.md) |
| Layer dependencies | [Dependency Rules](architecture/DEPENDENCY_RULES.md) |
| Tenant scope | [Multi-Tenancy Rules](architecture/MULTI_TENANCY_RULES.md) |
| EF/data modeling rules | [Data Modeling Rules](data/DATA_MODELING_RULES.md) |
| System flow index | [System Flows](flows/SYSTEM_FLOWS.md) |
| Checkout/payment/edge execution flow | [Checkout Execution Flow](flows/CHECKOUT_EXECUTION_FLOW.md) |
| Fairino Lua artifact/program deployment | [Robot Lua Artifact Flow](flows/ROBOT_LUA_ARTIFACT_FLOW.md) |
| Production Package installation | [Production Package Installation Flow](flows/PRODUCTION_PACKAGE_INSTALLATION_FLOW.md) |
| Production Package upgrade | [Production Package Upgrade Flow](flows/PRODUCTION_PACKAGE_UPGRADE_FLOW.md) |
| Tablet/edge/cloud contract | [IoT Contract](iot/IOT_CONTRACT.md) |
| Naming conventions | [Naming Rules](process/NAMING_RULES.md) |
| Deployment/runtime configuration | [Deployment Configuration](operations/DEPLOYMENT_CONFIG.md) |
| Robot artifact operational smoke | [Robot Artifact Operational Smoke Test](operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md) |
| Observability/logging/traces/metrics | [Observability](operations/OBSERVABILITY.md) |
| Manual critical rule checks | [Backend Critical Rule Checklist](process/BACKEND_CRITICAL_RULE_CHECKLIST.md) |

## Related

- [Architecture](../ARCHITECTURE.md)
- [Documentation Routing Map](DOCUMENTATION_ROUTING_MAP.md)
