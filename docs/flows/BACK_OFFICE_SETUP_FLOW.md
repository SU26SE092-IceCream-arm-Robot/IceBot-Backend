# Back-Office Setup Flow

This document describes the setup flow that prepares a tenant, kiosk, users, catalog, and menu before a kiosk can sell.

## Search Keywords

`back-office setup flow`, `setup to sale`, `tenant setup`, `organization setup`, `store setup`, `kiosk setup`, `account invitation`, `RBAC scope`, `catalog setup`, `menu publishing`, `kiosk configuration`

## Flow

```text
1. SystemAdmin creates Organization.
2. SystemAdmin or scoped OrgAdmin creates Stores under the Organization.
3. SystemAdmin or scoped manager creates Kiosks under Stores.
4. SystemAdmin creates internal Accounts.
5. System generates invitation link.
6. User accepts invitation and becomes Active.
7. SystemAdmin assigns role scopes:
   - organization
   - store
   - kiosk
8. Catalog/Product manager configures:
   - products
   - variants
   - options
   - recipes
   - ingredients
9. Sales manager configures:
   - menus
   - menu items
   - prices
   - availability windows
10. Robot `.lua` artifacts and ordered robot programs are prepared.
11. Kiosk/edge configuration release is prepared for runtime deployment.
```

## Rules

- Organization/Store/Kiosk hierarchy is tenant scope, not just UI navigation.
- Internal account onboarding uses invitation links; do not send admin-generated permanent passwords as the default flow.
- Role scopes decide which management data a user can read or manage.
- Menu sellability in Cloud is not the same as live machine readiness at Edge.
- Cloud manages immutable robot artifacts and ordered program manifests; it does not parse or control motion steps inside exported `.lua` files.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [Identity Onboarding Rules](../api/IDENTITY_ONBOARDING_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Multi-Tenancy Rules](../architecture/MULTI_TENANCY_RULES.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Robot Lua Artifact Flow](ROBOT_LUA_ARTIFACT_FLOW.md)
