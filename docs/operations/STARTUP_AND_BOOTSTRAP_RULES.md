# Startup And Bootstrap Rules

## Search Keywords

`startup`, `bootstrap`, `seed`, `runtime repair`, `hosted service`, `migration job`,
`readiness`, `provider health`, `Cloudinary`, `MinIO`, `PayOS`

## Purpose

Startup establishes a usable API process. It must not hide data repair, mutate
production fixtures, or fail unrelated workflows because an optional remote
provider is temporarily unavailable.

## Startup Classification

| Concern | Startup behavior | Verification owner |
| --- | --- | --- |
| Database connection and core database configuration | May block readiness because authoritative state is unavailable. | readiness and deployment checks |
| Authentication keys, active client-device key versions, and required core security configuration | Fail fast with a specific configuration error. | startup validator and deployment secret check |
| EF migrations | Never applied by the API host. | deployment migration job |
| Production data repair | Never automatic. | explicit audited repair command/workflow |
| Development/demo seed | Explicitly enabled development bootstrap only. | local bootstrap procedure |
| Cloudinary, MinIO, PayOS, SMTP, Firebase, MQTT, and Edge reachability | No remote startup probe. The owning feature translates failure safely. | protected diagnostic or feature smoke |
| Background reconciliation | Catch failure per item and cycle; continue later cycles. | job metrics and logs |

Configuration validation may run at startup when it is local and the process
cannot safely interpret the configuration. It must not perform remote I/O merely
to prove an optional provider is reachable.

## Operational Sequence

1. Supply secrets and configuration through the deployment secret mechanism.
2. Run the EF migration job using the workflow in
   [EF Core Migration Workflow](../data/EF_CORE_MIGRATION_WORKFLOW.md).
3. Run any approved bootstrap or repair command explicitly and retain its
   operator/audit evidence.
4. Roll out the API.
5. Check core readiness.
6. Run protected provider diagnostics or a scoped smoke test for each enabled
   external feature.

## No Invisible Repair

- Do not seed tenants, devices, topology, accounts, or demo runtime state on a
  production API startup path.
- Do not swallow repair exceptions and continue with an unknown fixture state.
- Do not let a hosted service reconcile ambiguous data by recreating a legacy
  projection. Record a durable reconciliation/error state and require a defined
  operator action where the authoritative record is missing.

## Readiness And Diagnostics

Liveness and readiness answer whether the API can serve core requests. They do
not certify every external feature. Provider diagnostics must have their own
timeout, safe output, and authorization; a diagnostic failure does not change
the process readiness result.

## Related Docs

- [Deployment Configuration](DEPLOYMENT_CONFIG.md)
- [Local Development Bootstrap](LOCAL_DEVELOPMENT_BOOTSTRAP.md)
- [IceBot Demo Runtime Repair](ICEBOT_DEMO_RUNTIME_REPAIR.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
