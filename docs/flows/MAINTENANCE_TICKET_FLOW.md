# Maintenance Ticket Flow

This document describes the V1 manual maintenance/support ticket workflow.

## Search Keywords

`maintenance ticket`, `support ticket`, `manual support`, `staff support`, `technician assignment`, `kiosk maintenance`, `device issue`, `order issue`, `device event evidence`, `maintenance.create`, `maintenance.manage`

## Scope

Maintenance Ticket V1 is a manual operations/support workflow.

It is not:

- an alert engine;
- an auto-ticket generation system;
- a chat workflow;
- a robot runtime workflow;
- a long-term analytics aggregate.

Tickets are kiosk-scoped work items. They may optionally link supporting evidence such as device, order, or device event references.

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

## Status Lifecycle

Allowed V1 transitions:

| From | Action | To |
| --- | --- | --- |
| `Open` | assign | `Assigned` |
| `Open` / `Assigned` | start | `InProgress` |
| `InProgress` | resolve | `Resolved` |
| `Resolved` | close | `Closed` |
| `Open` / `Assigned` / `InProgress` | cancel | `Cancelled` |

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

Management REST endpoints are listed in [API Surface Rules](../api/API_SURFACE_RULES.md#management-rest-surface).

V1 does not expose a GraphQL maintenance aggregate. Add one later only when the management UI needs an aggregated read model.

## Deferred

Do not implement these in V1 unless explicitly requested:

- auto-create ticket from device event;
- alert state machine;
- chat/comment thread;
- ticket reopen;
- SLA/escalation workflow;
- GraphQL maintenance dashboard aggregate;
- robot runtime integration.

## Related Docs

- [Operations Support Flow](OPERATIONS_SUPPORT_FLOW.md)
- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Boundary Contexts](../architecture/BOUNDARY_CONTEXTS.md)
