# Back-Office Setup Flow

This document describes the setup flow that prepares a tenant, kiosk, users, catalog, and menu before a kiosk can sell.

## Search Keywords

`back-office setup flow`, `setup to sale`, `tenant setup`, `organization setup`, `store setup`, `kiosk setup`, `account invitation`, `RBAC scope`, `catalog setup`, `menu publishing`, `kiosk configuration`

## Flow

```text
1. SystemAdmin creates Organization.
2. SystemAdmin or scoped OrgAdmin creates Stores under the Organization.
3. SystemAdmin or scoped manager creates Kiosks under Stores.
4. SystemAdmin or scoped tenant management creates internal Accounts.
5. Temporary demo behavior generates a password and emails account credentials.
6. User logs in and may change the password.
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
10. Inventory topology is provisioned:
   - device model declares `IngredientDispenser`
   - device and container
   - active ingredient
   - capacity, unit, and level-to-quantity profile
   - kiosk topology read shows capable devices with and without configured containers
   - identity changes use rebind: retire old state, resolve its estimate explicitly, and create an audited replacement
   - replacing hardware transfers all mappings to an already-provisioned Device in the same kiosk, then retires the source Device
   - retiring a Device also retires its active dispenser states; stale Device/Ingredient references remain visible as warnings
11. Robot `.lua` artifacts and ordered robot programs are prepared.
12. Kiosk/edge configuration release is prepared for runtime deployment.
13. Inventory readiness compares required Recipe ingredients with the target kiosk topology before deployment.
```

## Rules

- Organization/Store/Kiosk hierarchy is tenant scope, not just UI navigation.
- Current demo onboarding emails backend-generated temporary credentials for tenant roles; `SystemAdmin` remains bootstrap-only.
- Target onboarding uses invitation links and user-owned credential setup. The invitation implementation is retained and should replace the temporary behavior when time permits.
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
