# Client Device API

This document owns the REST contract for a managed self-order tablet. A
`ClientDevice` is neither an internal `Account`, a physical kiosk `Device`, nor
an `ExecutionEndpoint`: it is the Cloud identity of one tablet installation
bound to one kiosk.

## Search Keywords

`client device`, `tablet provisioning`, `tablet login`, `installation credential`,
`runtime bearer`, `tablet rebind`, `tablet replacement`, `client-device-sessions`

## Management Lifecycle

```text
GET  /api/v1/management/kiosks/{kioskId}/client-devices
GET  /api/v1/management/client-devices/{clientDeviceId}
POST /api/v1/management/kiosks/{kioskId}/client-devices
POST /api/v1/management/client-devices/{clientDeviceId}/disable
POST /api/v1/management/client-devices/{clientDeviceId}/reactivate
POST /api/v1/management/client-devices/{clientDeviceId}/rotate-credential
POST /api/v1/management/client-devices/{clientDeviceId}/rebind
POST /api/v1/management/client-devices/{clientDeviceId}/retire
POST /api/v1/management/kiosks/{kioskId}/client-devices/replace
```

These are scoped internal-management routes. Provision, replace, rotate,
rebind, disable, reactivate, and retire require `Idempotency-Key`; lifecycle
and scope mutations require an operator reason and the current device revision
where the request contract exposes `expectedRevision`. An exact retry returns
the original operation result; a reused key with different content returns
`409`.

The installation credential is returned only at provisioning, replacement, or
credential rotation. It is never returned by a read endpoint, persisted in a
response model, or logged. Management credentials are not tablet credentials.

## Device Session

```http
POST /api/v1/client-device-sessions
```

The tablet submits its stable `clientDeviceId`, installation identity, and
installation credential over HTTPS. It must also send the same identifier in
`X-Client-Device-Id`; a missing or mismatched header is rejected before the
credential exchange. A successful exchange returns a short-lived
`ClientDeviceBearer` JWT. This is not an Account login and does not create a
refresh-token session.

The session endpoint has an 8 KiB body limit and is rate limited independently
by source IP and device header before authentication. Runtime requests are
partitioned by authenticated device and kiosk. Invalid credential attempts emit
only a reason-tagged metric and never log credential material or a raw
installation secret.

The runtime authenticator reloads the device's binding, lifecycle state,
credential version, and session version from PostgreSQL. Disable, credential
rotation, rebind, replacement, and retirement therefore invalidate an existing
device JWT immediately.

## Runtime Contract

```text
GET  /api/v1/runtime/menu
POST /api/v1/runtime/orders
GET  /api/v1/runtime/orders/{orderId}
POST /api/v1/runtime/orders/{orderId}/payment-sessions
GET  /api/v1/runtime/orders/{orderId}/payment-status
POST /api/v1/runtime/orders/{orderId}/cancel
```

Every route requires `Authorization: Bearer <client-device-jwt>`. The kiosk,
store, organization, and order channel are derived from the authenticated
device; tablet request bodies must not carry those authority fields. Every
order-specific route also requires the `Order-Access-Token` returned by order
creation. That token is bound to both the order and the originating
`ClientDevice`.

Order creation idempotency is scoped by `ClientDevice` plus `Idempotency-Key`.
The kiosk customer-session lock remains a kiosk-wide operational invariant.

Input lengths, line counts, and quantities are bounded by
`ClientDevices:Runtime`; the server does not accept unbounded customer notes or
client identifiers. The detailed checkout message and state contract belongs to
[Tablet and Cloud Contract](../iot/TABLET_CLOUD_CONTRACT.md).

## Related Docs

- [API Surface Rules](API_SURFACE_RULES.md)
- [Authorization Rules](AUTHORIZATION_RULES.md)
- [Tablet and Cloud Contract](../iot/TABLET_CLOUD_CONTRACT.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
