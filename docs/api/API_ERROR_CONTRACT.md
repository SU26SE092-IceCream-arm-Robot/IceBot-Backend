# API Error Contract

## Search Keywords

`ApiResult`, `API error`, `status code`, `503`, `dependency unavailable`,
`validationErrors`, `businessError`, `SystemError`, `retry`, `mutation state`

## Purpose

This document defines the public error semantics for REST endpoints. It keeps
WebApp, Tablet, and external integrations from inferring behavior from localized
messages, provider exceptions, or controller-specific response shapes.

## Envelope

Controller-facing handlers return `ApiResult<T>` and controllers preserve its
HTTP status code. The envelope may contain `succeeded`, `statusCode`, `message`,
`data`, `details`, `validationErrors`, `businessError`, and `systemError`.

- The HTTP status and `statusCode` must match.
- Consumers branch on the HTTP status and a documented structured error code.
  They must not parse `message`, `details`, or `systemError`.
- `validationErrors` identifies request fields when request-shape validation
  fails. It is not a substitute for a business conflict code.
- `businessError` is optional structured business metadata when an endpoint
  documents it. New consumers must tolerate it being absent.
- `systemError` is diagnostic metadata only. It must never contain a stack
  trace, provider response, credential, secret, connection string, or token.

## Status Semantics

| Status | Meaning | Consumer action |
| --- | --- | --- |
| `400` | Request shape or validation is invalid. | Correct input; do not retry unchanged. |
| `401` | Authentication is absent, expired, or invalid. | Reauthenticate. |
| `403` | Actor lacks permission or effective scope. | Do not retry; request access if appropriate. |
| `404` | Resource is absent or intentionally not visible in scope. | Refresh the owning view; do not infer cross-tenant existence. |
| `409` | Current lifecycle, revision, duplicate, or idempotency state conflicts. | Refresh authoritative state before a new action. |
| `423` | Account or resource is temporarily locked. | Wait for the documented unlock condition. |
| `429` | Rate limit exceeded. | Retry only after the advertised delay. |
| `503` | An enabled feature cannot complete because its external dependency is unavailable or safely rejected the operation. | Preserve input, clear terminal mutation state, and allow an explicit retry. |
| `500` | Unexpected backend defect. | Show a generic failure and record correlation information for support. |

`503` is not a generic replacement for validation, authorization, or an
authoritative business decision. It is only for a temporary dependency failure
at the owning feature boundary.

## Provider Failure Rules

- Provider transport, timeout, authentication, quota, and malformed-provider
  responses are translated by the owning adapter or application workflow.
- The failure must not make unrelated API routes, authentication, catalog reads,
  or readiness fail.
- Public responses use a safe message and stable application code. Detailed
  provider diagnostics stay in protected logs or diagnostics.
- A command that may be retried must preserve its idempotency behavior. Do not
  issue a second provider side effect merely because the caller received `503`.
- A client mutation enters a terminal error state for `400`, `403`, `404`,
  `409`, `423`, and `503`; it must not remain disabled indefinitely after the
  response completes.

## Error Contract Changes

Adding a new public error code or changing an endpoint's retry behavior is an
API contract change. Document it in the owning API or flow document and cover
the terminal response in a focused test.

## Related Docs

- [API Surface Rules](API_SURFACE_RULES.md)
- [Idempotency And Retry Rules](../data/IDEMPOTENCY_RETRY_RULES.md)
- [Dependency Rules](../architecture/DEPENDENCY_RULES.md)
- [Startup And Bootstrap Rules](../operations/STARTUP_AND_BOOTSTRAP_RULES.md)
