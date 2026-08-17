# Service Registration And Content Flow

## Search Keywords

`service registration`, `landing form`, `lead approval`, `pre-tenant`,
`organization provisioning`, `initial OrgAdmin`, `content page`, `privacy
policy`, `published content`, `content revision`

## Purpose

This flow supports a visitor registering interest in an IceBot service and a
SystemAdmin reviewing that request. It is separate from organization-owned
FranchiseOnboarding, which begins only after a tenant exists.

## Public APIs

| Operation | Route | Rules |
| --- | --- | --- |
| Read a published page | `GET /api/v1/content-pages/{slug}` | Returns only an immutable published revision and an ETag. |
| Submit service registration | `POST /api/v1/service-registrations` | Anonymous. Requires `Idempotency-Key`, accepted current privacy-policy revision, and is rate-limited by source IP. |

The registration request includes contact and business details plus the exact
published privacy-policy revision accepted by the visitor. A later policy
publication never rewrites that consent record.

## Management APIs

All routes below are SystemAdmin-only.

| Operation | Route |
| --- | --- |
| List/detail registrations | `GET /api/v1/management/service-registrations`, `GET /api/v1/management/service-registrations/{id}` |
| Start review | `POST /api/v1/management/service-registrations/{id}/start-review` |
| Reject | `POST /api/v1/management/service-registrations/{id}/reject` |
| Approve and provision | `POST /api/v1/management/service-registrations/{id}/approve` |
| Retry failed provisioning | `POST /api/v1/management/service-registrations/{id}/retry-provisioning` |
| Manage long-form content | `/api/v1/management/content-pages/*` |

Management mutations require `expectedRevision`. A stale revision returns a
conflict; clients must reload before retrying.

## Provisioning

```text
Submitted -> UnderReview -> Provisioning -> Provisioned
                         -> Rejected
Provisioning -> ProvisioningFailed -> Provisioning (retry)
```

As a temporary demo override, approval creates exactly one Organization, one
active OrgAdmin Account with a generated local password, and one OrgAdmin role
assignment scoped to that Organization in one database transaction. Credential
email delivery happens after commit. Failure does not roll back the tenant;
management must reset the password before account handoff.

The target flow remains invitation-based: create an invited account and durable
invitation in the provisioning transaction, then deliver the invitation after
commit. Its entities, acceptance API, and identity service are intentionally
retained for restoration.

Do not implement approval in a frontend by chaining organization, account, role,
and invitation APIs.

## Content Authoring

Supported page keys are `about-us`, `privacy-policy`, `payment-policy`,
`terms-of-use`, and `contact-information`. A draft is private. Publishing creates
an immutable revision, and public reads return only that revision. Backend
sanitization removes executable and unsafe HTML; frontend editor output is never
trusted as already safe.

## Operational Recovery

- Reuse the same `Idempotency-Key` only with the same public payload.
- A provisioning conflict is retained as `ProvisioningFailed` with a safe
  management message. Correct the external conflict, then retry using the stored
  approved input.
- Never log raw registration bodies, invitation tokens, or generated passwords.
- Retention/deletion policy for rejected and cancelled registrations is a future
  operations decision; do not delete records ad hoc while it is unresolved.

## Related Docs

- [API Surface Rules](../api/API_SURFACE_RULES.md)
- [Identity Onboarding Rules](../api/IDENTITY_ONBOARDING_RULES.md)
- [Authorization Rules](../api/AUTHORIZATION_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
