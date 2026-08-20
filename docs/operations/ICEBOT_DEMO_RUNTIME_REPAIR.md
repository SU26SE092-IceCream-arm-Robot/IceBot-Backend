# ICEBOT-DEMO Runtime Repair

Use this one-shot command when the isolated `ICEBOT-DEMO` fixture has stale
catalog or inventory data from an earlier seed version.

The repair is limited to `ICEBOT-DEMO` and does not reset orders, deployments,
releases, or global/non-demo tenant data. It normalizes the vanilla recipe to
the single operational consumable `VANILLA-SOFT-SERVE-MIX`, activates the demo
catalog/menu, and restores the demo hopper inventory state.

## Command

Run against the intended environment after the database is reachable:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet IceBot.dll --repair-icebot-demo-runtime
```

For a local source run:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/WebAPI/WebAPI.csproj -- --repair-icebot-demo-runtime
```

The command is idempotent. Running it again must not create duplicate product,
variant, recipe, menu, or dispenser records.

After repair, restart or reconnect Edge so it reports `Ready / Idle / Safe`,
then verify:

```text
GET /api/v1/kiosks/{kioskId}/runtime-menu
```

The response should contain the demo vanilla product in `items`.

`DemoCatalogSeed:SeedInventoryTopology=true` is enabled in the production
configuration for the demo fixture. It creates missing topology on startup;
the repair command is still required once when older records already exist in
an inactive or stale state.

For the current demo deployment, `DemoCatalogSeed:RepairExistingDataOnStartup`
is temporarily enabled in production configuration. The repair runs from a
hosted service after the demo tenant, catalog, and topology seed services have
completed; it does not execute before Kestrel startup. If the repair fails, the
failure is logged and the API host remains available. After the production demo
data has been verified, set this flag back to `false` in the deployment
configuration and deploy again.

## Search Keywords

`ICEBOT-DEMO`, runtime menu empty, vanilla soft serve, inventory topology,
`VANILLA-SOFT-SERVE-MIX`, demo runtime repair.

## Related Docs

- [Robot Lua Deployment and Activation Flow](../flows/ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md)
- [MQTT Operations](MQTT_OPERATIONS.md)
