# Failure Flow Index

This document routes failure questions to the contract that owns the current state transition, retry, or recovery behavior. It does not redefine those rules.

## Search Keywords

`failure flow`, `paid but edge cannot execute`, `edge offline`, `duplicate notification`, `retry`, `partial output`, `outcome unknown`, `power recovery`, `refund required`

## Failure Ownership

| Failure | Owning document |
| --- | --- |
| Payment succeeds but fulfillment cannot start | [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md) |
| Partial, defective, or unknown production output | [Production Incident Resolution Flow](PRODUCTION_INCIDENT_RESOLUTION_FLOW.md) |
| Edge is offline while payment or dispatch completes | [Checkout Execution Flow](CHECKOUT_EXECUTION_FLOW.md) and [Edge Command Contract](../iot/EDGE_COMMAND_CONTRACT.md) |
| Duplicate requests, callbacks, command delivery, ACKs, reports, or sync events | [Idempotency and Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md) |
| Cloud, database, MQTT, Edge, controller, tablet, or store power loss | [Restart And Power Recovery](../operations/RESTART_AND_POWER_RECOVERY.md) |
| MQTT delivery and credential failure | [MQTT Operations](../operations/MQTT_OPERATIONS.md) |
| Artifact download, verification, installation, or activation failure | [Robot Lua Deployment And Activation Flow](ROBOT_LUA_DEPLOYMENT_AND_ACTIVATION_FLOW.md) |
| Production Package installation failure | [Production Package Installation Flow](PRODUCTION_PACKAGE_INSTALLATION_FLOW.md) |
| Production Package upgrade materialization, cutover, or rollback failure | [Production Package Upgrade Flow](PRODUCTION_PACKAGE_UPGRADE_FLOW.md) |

## Boundary Rule

- Payment settlement, order fulfillment, machine execution, production incidents, and refunds are separate state machines.
- A downstream failure must not rewrite committed upstream evidence.
- Unknown or partial physical output requires incident resolution; it must not be inferred as a whole-order refund.
- Retry is allowed only under the idempotency and physical-output rules owned by the relevant workflow.

## Related Docs

- [System Flows](SYSTEM_FLOWS.md)
- [IoT Contract](../iot/IOT_CONTRACT.md)
- [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md)
- [Observability](../operations/OBSERVABILITY.md)
