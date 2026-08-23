# Local Development Bootstrap

## Search Keywords

`local development`, `demo account`, `development seed`, `bootstrap account`, `orgadmin`, `manager`, `staff`, `technician`, `BootstrapAdmin`, `ICEBOT-DEMO`

## Purpose

The development bootstrap creates local test identities with valid tenant scopes. It is for local development only; it is not a production account-provisioning workflow.

## Enablement And Secret

The role-account seed runs only when both conditions are true:

1. `ASPNETCORE_ENVIRONMENT` is `Development`.
2. `DevelopmentRoleAccounts:Enabled` is `true`.

New local accounts use `BootstrapAdmin:Password`, with `BOOTSTRAP_ADMIN_PASSWORD` as the direct environment fallback. Keep this password in user secrets or a local environment file, never in tracked `appsettings` files.

Example local setup:

```powershell
dotnet user-secrets set "BootstrapAdmin:UserName" "admin" --project src\WebAPI\WebAPI.csproj
dotnet user-secrets set "BootstrapAdmin:Email" "admin@icebot.local" --project src\WebAPI\WebAPI.csproj
dotnet user-secrets set "BootstrapAdmin:Password" "<local-secret>" --project src\WebAPI\WebAPI.csproj
```

The `SystemAdmin` bootstrap is documented separately in [Deployment Configuration](DEPLOYMENT_CONFIG.md#initial-systemadmin-bootstrap).

## Seeded Tenant Scope

When at least one development role needs an account, the bootstrap creates or reuses this local tenant tree:

```text
ICEBOT-DEMO
  -> ICEBOT-DEMO-STORE
       -> ICEBOT-DEMO-KIOSK
            -> ICEBOT-DEMO-EDGE (Provisioning Full Edge endpoint)
```

| Role | Demo identity | Scope assigned when seeded |
| --- | --- | --- |
| `OrgAdmin` | `demo-orgadmin@icebot.local` | `ICEBOT-DEMO` organization |
| `Manager` | `demo-manager@icebot.local` | `ICEBOT-DEMO` organization and `ICEBOT-DEMO-STORE` |
| `Staff` | `demo-staff@icebot.local` | `ICEBOT-DEMO` organization and `ICEBOT-DEMO-STORE` |

All newly created demo identities use local login and the configured bootstrap password.

`Technician` is not seeded as a tenant account. It is a platform-owned identity
managed through the Platform Technician API and receives Store/Kiosk access only
through `TechnicianSupportGrant`.

`DevelopmentExecutionEndpointSeed:Enabled` controls the endpoint seed in
Development. The endpoint is idempotent by kiosk and endpoint code. It remains
`Provisioning`: a real Edge runtime must still provision credentials, report
readiness, and acknowledge a deployment before it can be selected as an active
deployment target.

## Idempotency Rule

The seed is identity-based. For each configured demo identity, it reuses the
matching `UserName` or email and ensures the exact active role/scope assignment
exists.

- Restarting the backend does not reset an existing account password or replace
  its existing scopes.
- An existing demo account without its configured active role/scope receives that
  missing assignment.
- The demo tenant tree is created by the tenant seed, not inferred from whether
  an unrelated account already has the same role.

To recreate a demo identity, reset the local database or remove that exact demo
account. Do this only for local development data.

## Scope Boundary

Do not enable this setting in deployed environments. Real accounts must use the normal invitation and role-assignment flows.

## Reset IceBot Demo Authoring Data

Use the demo reset to repeat robot-authoring bundle tests against `ICEBOT-DEMO`.
It also removes the obsolete `ICEBOT-AUTOMATION-TEST` fixture organization.

Run the reset only after stopping the local WebAPI process:

```powershell
.\scripts\reset-icebot-demo.ps1
```

The command is available only with `ASPNETCORE_ENVIRONMENT=Development`. It:

- preserves the `ICEBOT-DEMO` organization, its Store/Kiosk baseline, and account scopes;
- deletes `ICEBOT-DEMO` authoring imports, Draft/Published test programs,
  artifacts, technical contracts, and their object-storage bytes;
- deletes the Vanilla demo product's operator-confirmed bindings, configuration releases,
  routes, and menu items that reference the reset product;
- rebuilds the organization-owned Vanilla soft-serve product, variants, options, and
  active Recipe from the global template.

The legacy `ICEBOT-AUTOMATION-TEST` organization is hard-deleted only when it
has no Store/Kiosk or production-package state. Its authoring data, local object
storage bytes, and account-role assignments are removed first.

To remove only that obsolete fixture without resetting `ICEBOT-DEMO`, run:

```powershell
.\scripts\delete-legacy-automation-fixture.ps1
```

The command may run after authoring and publication, but refuses to erase the
demo data when it has order, deployment, execution, active-release, or production
package evidence. Do not use it as an operational rollback or against production data.
If object storage is unavailable after the database reset, metadata remains
reset and the command logs retained orphaned object keys for normal orphan
cleanup.

## Related Docs

- [Deployment Configuration](DEPLOYMENT_CONFIG.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Identity Onboarding Rules](../api/IDENTITY_ONBOARDING_RULES.md)
