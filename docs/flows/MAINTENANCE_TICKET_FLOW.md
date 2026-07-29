# Maintenance Ticket Flow

This document describes the maintenance/support ticket workflow, including manual creation and the bounded inventory-alert automation entry point.

## Search Keywords

`maintenance ticket`, `support ticket`, `manual support`, `staff support`, `technician assignment`, `kiosk maintenance`, `device issue`, `order issue`, `device event evidence`, `maintenance.create`, `maintenance.manage`

## Scope

Maintenance Ticket is an operations/support work-management aggregate. Most
tickets are created manually; configured inventory-empty alert automation may
create one ticket linked to the alert.

It is not:

- the owner of alert correlation or lifecycle;
- a chat workflow;
- a robot runtime workflow;
- a long-term analytics aggregate.

Tickets are kiosk-scoped work items. They may optionally link supporting evidence such as device, order, device event, or alert references.

Each ticket declares `OperationalImpact`: `None`, `BlocksNewOrders`, or `RequestsEmergencyStop`. Starting a blocking ticket atomically moves the kiosk to `Maintenance`; an emergency-impact ticket moves it to `EmergencyStopRequested`. A normal evidence-only ticket does not affect sales.

`EmergencyStopRequested` only holds new Cloud work and records that immediate safety intervention is required. It does not send a hardware command and does not assert that the robot stopped. Physical `EmergencyStopped` truth belongs to the typed Edge safety projection.

## Flow

```text
Staff / Manager / Technician sees an issue
  -> create maintenance ticket
  -> ticket starts Open
  -> manager / technician assigns owner
  -> technician starts work
  -> technician resolves with notes
  -> manager / authorized actor closes ticket
```

Alternative cancellation path:

```text
Open / Assigned / InProgress ticket
  -> cancelled with reason
  -> no further lifecycle transition in V1
```

Bounded automated entry path:

```text
InventoryAlertReconciler detects INVENTORY_EMPTY
  -> raises or correlates the Alert
  -> optionally creates one linked Open maintenance ticket
  -> later alert recovery does not close the ticket
```

## Status Lifecycle

Allowed V1 transitions:

| From | Action | To |
| --- | --- | --- |
| `Open` | assign | `Assigned` |
| `Open` / `Assigned` | start | `InProgress` |
| `InProgress` | resolve | `Resolved` |
| `Resolved` | close | `Closed` |
| `Open` / `Assigned` / `InProgress` | cancel | `Cancelled` |

Resolving, closing, or cancelling a ticket does not automatically return the kiosk to `Operational`. An authorized operator must verify the kiosk and explicitly resume it. This avoids reopening sales while another ticket, cleaning task, restock, or safety condition remains active.

Forbidden V1 transitions:

- `Resolved -> Cancelled`
- `Closed -> Resolved`
- `Closed -> Cancelled`
- `InProgress -> Assigned`
- `Cancelled -> any other status`

## Permissions

| Policy | Roles | Meaning |
| --- | --- | --- |
| `maintenance.view` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | View tickets within assigned scope |
| `maintenance.create` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Staff`, `Technician` | Create tickets within assigned scope |
| `maintenance.manage` | `SystemAdmin`, `OrgAdmin`, `Manager`, `Technician` | Assign, start, resolve, close, cancel, or update tickets within assigned scope |

Staff can create and view tickets in assigned scope, but cannot assign, resolve, close, or cancel by default.

An assignee must be an active account with an active `Technician`, `Manager`, or
`OrgAdmin` role assignment that matches the ticket kiosk, store, or organization
on that same role-scope record. An account's role in another tenant does not make
it assignable. A push-notification device is optional and does not determine
assignment eligibility.

## Evidence Links

A ticket may reference:

- `KioskId` as the primary scope anchor;
- `DeviceId` when the issue is tied to a physical device;
- `OrderId` when the issue affects a customer order;
- `DeviceEventId` when the issue is backed by a telemetry/event record.

Evidence links should stay lightweight. Do not embed full order, account, event payload, or raw telemetry objects into ticket responses.

## API Surface

Management REST endpoints are listed in [Management API Surface](../api/MANAGEMENT_API_SURFACE.md).

V1 does not expose a GraphQL maintenance aggregate. REST remains the current maintenance read/write surface.

## Excluded From Current Contract

The current contract excludes:

- chat/comment thread;
- ticket reopen;
- ticket SLA/escalation workflow;
- a GraphQL maintenance aggregate.

Inventory alert automation is implemented separately: when configured, an
`INVENTORY_EMPTY` alert creates one linked maintenance ticket. General device
events do not automatically create tickets, and resolving the alert does not
close its ticket.
- GraphQL maintenance dashboard aggregate;
- robot runtime integration.

## Related Docs

- [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
