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
| `OrgAdmin` | `orgadmin@icebot.local` | `ICEBOT-DEMO` organization |
| `Manager` | `manager@icebot.local` | `ICEBOT-DEMO` organization and `ICEBOT-DEMO-STORE` |
| `Staff` | `staff@icebot.local` | `ICEBOT-DEMO` organization and `ICEBOT-DEMO-STORE` |
| `Technician` | `technician@icebot.local` | `ICEBOT-DEMO` organization, `ICEBOT-DEMO-STORE`, and `ICEBOT-DEMO-KIOSK` |

All newly created demo identities use local login and the configured bootstrap password.

`DevelopmentExecutionEndpointSeed:Enabled` controls the endpoint seed in
Development. The endpoint is idempotent by kiosk and endpoint code. It remains
`Provisioning`: a real Edge runtime must still provision credentials, report
readiness, and acknowledge a deployment before it can be selected as an active
deployment target.

## Idempotency Rule

The seed is role-based, not username-based. For each configured role, it creates a demo identity only when **no non-deleted account has an active assignment for that role**.

- If any non-deleted account already has an active `Manager` assignment, the manager demo account is skipped, even when its username is different.
- Restarting the backend does not reset passwords, replace scopes, or add a second account for a role already represented.
- The demo tenant tree is not created solely for this seed when all four roles already have active assignments.
- A deleted account or inactive role assignment does not count as an existing role holder.

To deliberately seed a role again, remove or deactivate every active assignment for that role, or reset the local database. Do this only for local development data.

## Scope Boundary

Do not enable this setting in deployed environments. Real accounts must use the normal invitation and role-assignment flows.

## Robot Authoring Automation Fixture

Use the isolated `ICEBOT-AUTOMATION-TEST` organization to repeat robot-authoring
bundle tests without changing `ICEBOT-DEMO` or editing a ZIP between attempts.

Run the reset only after stopping the local WebAPI process:

```powershell
.\scripts\reset-robot-authoring-automation-test.ps1
```

The command is available only with `ASPNETCORE_ENVIRONMENT=Development`. It:

- creates or reuses `ICEBOT-AUTOMATION-TEST`;
- grants the existing local `orgadmin@icebot.local` an additional `OrgAdmin`
  assignment for that organization when the account exists;
- deletes that tenant's authoring imports, Draft/Published test programs,
  artifacts, technical contracts, and their object-storage bytes;
- rebuilds the organization-owned Vanilla soft-serve product, variant, and
  active Recipe from the global template.

The command refuses to run once the test tenant has a menu or configuration
release. Do not use it as an operational rollback or against production data.
If object storage is unavailable after the database reset, metadata remains
reset and the command logs retained orphaned object keys for normal orphan
cleanup.

## Related Docs

- [Deployment Configuration](DEPLOYMENT_CONFIG.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Identity Onboarding Rules](../api/IDENTITY_ONBOARDING_RULES.md)
