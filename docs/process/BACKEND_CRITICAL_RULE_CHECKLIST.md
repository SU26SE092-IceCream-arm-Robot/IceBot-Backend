# Backend Critical Rule Checklist

Use this checklist only for handoff checks that need deployed infrastructure, provider credentials, a real client, or physical runtime evidence. Domain and persistence invariants belong in automated tests and their owning contract documents.

## Search Keywords

`manual backend verification`, `deployment handoff`, `external provider smoke`, `physical robot smoke`, `MQTT recovery`, `PayOS callback`, `Firebase delivery`, `SMTP invitation`

## Automated Coverage Boundary

Do not manually duplicate these checks during every handoff:

| Rule family | Primary automated evidence |
| --- | --- |
| Manual, Packaged, and MachineProduced fulfillment transitions | `OrderItemFulfillmentTests`, `PaidOrderFulfillmentConcurrencyIntegrationTests` |
| Production incident lifecycle and exact-unit remake | `ProductionIncidentTests`, Edge production integration tests |
| Duplicate payment settlement and refund targeting | `PaymentWebhookConcurrencyIntegrationTests`, `RefundConfirmationTests` |
| Kiosk operational-state sales gating | `KioskOperationalStateTests` and checkout/runtime-menu tests |
| Production Package installation and upgrade invariants | Production Package integration tests |
| MQTT credential lifecycle | MQTT credential unit and integration tests |

If one of these test suites is disabled or failing, report that missing evidence instead of replacing it with an undocumented manual assumption.

## Deployment Environment

Verify against the target environment:

- migrations apply to an empty database and the intended upgrade baseline;
- core database and security configuration satisfy startup validation;
- required object-storage configuration is valid, and bucket ownership/reachability is verified through a controlled smoke or protected diagnostic rather than an unrelated host-startup failure;
- provider settings for MinIO, MQTT, SMTP, PayOS, Firebase, and Cloudinary are present for the enabled features, without requiring a remote reachability call during generic API startup;
- health endpoints distinguish startup readiness from optional dependency degradation;
- secrets are supplied through the deployment secret mechanism and do not appear in responses or logs.

## External Provider Smoke

Verify with non-production provider credentials where available:

- PayOS accepts a valid signed callback and rejects an invalid signature;
- a duplicate PayOS callback does not create a second settlement;
- SMTP invitation delivery produces only the configured invitation URL;
- Firebase sends a critical operational notification to an eligible registered device;
- an invalid Firebase token is retired without retrying it indefinitely.

## Edge And Robot Smoke

Verify only when the real Edge/controller runtime is available:

- MQTT wake-up is followed by durable command pull;
- command ACK and execution reports preserve the original `CommandId`;
- artifact checksum and production-definition provenance are verified before activation;
- restart during execution follows the published restart policy and does not assume an unsafe resume;
- uncertain, partial, or defective physical output opens the expected production incident;
- Edge refuses a command when it cannot persist state before ACK.

## Recovery Smoke

- restart MQTT and confirm pending commands remain pullable;
- temporarily interrupt MinIO, Cloudinary, or another enabled provider and confirm the owning feature fails safely without making unrelated APIs unready;
- restart Cloud/API during a pending workflow and confirm reconciliation resumes without duplicate effects;
- confirm stuck-workflow timeout and retry-failure metrics are emitted and alertable.

## Related Docs

- [Vertical Slice Review](VERTICAL_SLICE_REVIEW.md)
- [Working Protocol](WORKING_PROTOCOL.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
- [Robot Artifact Operational Smoke Test](../operations/ROBOT_ARTIFACT_OPERATIONAL_SMOKE.md)
- [Restart And Power Recovery](../operations/RESTART_AND_POWER_RECOVERY.md)
