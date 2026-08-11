# Identity Onboarding Rules

This document is the backend source of truth for internal account onboarding, invitation links, email ownership proof, and temporary password fallback.

## Search Keywords

`identity onboarding`, `account onboarding`, `admin creates account`, `internal account invitation`, `invitation link`, `accept invitation`, `GoogleEmail`, `GoogleSubjectId`, `Google login policy`, `email confirmed`, `EmailConfirmedAt`, `email ownership proof`, `CreateInvitation`, `SendInvitationEmail`, `InitialPassword`, `temporary password`, `Invited account`, `Active account`, `/api/v1/management/organizations/{organizationId}/accounts`, `/api/v1/authentication/accept-invitation`

## Purpose

Internal accounts are created by authorized management users. Public signup is disabled for internal system accounts. Account management is organization-owned: `SystemAdmin` may create and administer accounts for any selected organization, while `OrgAdmin` may do so only in its own organization scope. Either actor may assign `OrgAdmin`, `Manager`, `Staff`, and `Technician` within that route organization; neither organization route can assign the global `SystemAdmin` role. Initial/recovery `SystemAdmin` provisioning remains bootstrap-only.

The default onboarding method is:

```text
admin creates account
  -> backend creates invitation link
  -> user accepts invitation
  -> user completes credential setup required by the admin-enabled login methods
  -> account becomes Active
```

Do not use username + password delivery as the default onboarding flow.

## Authentication Method Ownership

Management chooses which authentication methods an account is allowed to use through `LocalLoginEnabled` and `GoogleLoginEnabled`. Invitation acceptance does not let the invited user enable an authentication method that management did not authorize.

For Google login, `GoogleEmail` is the management-configured identity allowlist value:

- the verified email from Firebase must match `GoogleEmail`;
- first login binds the verified Google subject to `GoogleSubjectId`;
- later logins must match both the configured email and the bound subject;
- authentication must not overwrite `GoogleEmail` from token claims;
- changing `GoogleEmail` through account management clears the old subject binding so the newly authorized identity can bind on its first successful login.

The invited user only supplies credential material for an enabled method, such as choosing a password when local login is enabled. The user does not choose the account's allowed login policy.

The management password-reset/set-password command also changes credential material only. It does not implicitly enable local login; management must change `LocalLoginEnabled` through the account policy update contract.

## Default Flow

Default request behavior:

```text
CreateInvitation = true
SendInvitationEmail = true
```

Flow:

```text
POST /api/v1/management/organizations/{organizationId}/accounts
  -> create Account
  -> Status = Invited
  -> create AccountInvitation
  -> return invitation link
  -> optionally send invitation email
```

The management response should include invitation details when an invitation is created:

```text
invitationUrl
expiresAt
emailSentAt
```

`Email:InvitationBaseUrl` is required at startup. Management responses return the complete invitation URL and never expose the raw bearer token as a separate field.

## Invitation Generation Vs Delivery

Invitation generation and invitation delivery are separate responsibilities.

Invitation generation owns:

- creating the raw token
- hashing the token before storage
- storing lifecycle fields
- validating token on accept
- expiration and revocation
- activating the account after successful acceptance

Invitation delivery owns:

- sending email when requested
- allowing admin/manual delivery through another channel

Email is only one delivery channel. Admin users may copy and send the invitation link through another approved channel such as email, Zalo, Messenger, Slack, Teams, QR code, printed paper, or an internal message.

## Invitation Accepted Vs Email Confirmed Vs Account Active

Do not collapse these three concepts into one state.

| Concept | Meaning | Stored as |
| --- | --- | --- |
| Invitation accepted | User presented a valid invitation token and completed the accept flow | `AccountInvitation.AcceptedAt` |
| Email confirmed | User proved ownership of the account mailbox | `Account.EmailConfirmedAt` |
| Account active | User is allowed to log in through an enabled auth method | `Account.Status = Active` |

Final rule:

```text
Accept invitation
  -> may activate account

Verified mailbox ownership
  -> may confirm email
```

These states are independent.

For multi-tenant systems, do not infer email ownership from the domain:

```text
@gmail.com
@outlook.com
@company.com
@corp.xyz.vn
```

The same tenant may contain company email, Gmail, Yahoo, contractors, or external accounts. The security criterion is ownership proof, not domain shape.

Valid email ownership proof can come from:

- a separate verify-email link
- Firebase/Google token with `email_verified = true` and email matching the management-configured `GoogleEmail`
- an invitation link sent by the backend to the same mailbox and accepted from that email delivery path

Manual invitation delivery is not email ownership proof.

Examples:

| Case | Result |
| --- | --- |
| Backend sends invitation email to `user@gmail.com`, user clicks and accepts | `Active`, `EmailConfirmedAt` can be set |
| Firebase returns verified `employee@corp.com` matching `GoogleEmail` | `Active`, `EmailConfirmedAt` can be set |
| Admin copies invitation link and sends through Zalo/Messenger/QR/paper | `Active`, `EmailConfirmedAt` remains null |
| Google login invitation where Firebase verified email matches `GoogleEmail` | `Active`, `EmailConfirmedAt` can be set |

## Email Failure

SMTP failure must not make onboarding unrecoverable.

If invitation email delivery fails:

```text
account remains Invited
invitation remains usable
response includes invitation link
EmailSentAt remains null
```

The management UI can show a warning and let the admin copy the link manually or create another invitation later.

Do not leak raw SMTP exception details to the API response. Log the server-side exception instead.

## Create Or Regenerate Invitation

Management can create a new invitation for an account that is still `Invited`.

One account should have at most one active invitation.

Invitation generation is serialized by account. Revoking prior active links and
persisting the replacement link are one transaction; concurrent regeneration
requests cannot leave multiple active invitation records.

Route direction:

```text
POST /api/v1/management/organizations/{organizationId}/accounts/{accountId}/invitation
```

Request direction:

```json
{
  "sendEmail": true
}
```

Behavior:

```text
create new invitation
  -> optionally send email
  -> revoke previous active invitations
```

This route means "create a new invitation link". It is not only "resend email".

Expired invitations are not extended or revived. Create a new invitation instead.

## Accept Invitation

Invitation acceptance is a public endpoint and does not require an existing login.

Route direction:

```text
POST /api/v1/authentication/accept-invitation
```

Flow:

```text
user submits token and new password
  -> backend hashes token
  -> find active, non-expired, non-revoked invitation
  -> require account Status = Invited
  -> set password for local login when needed
  -> set account Status = Active
  -> mark invitation Accepted
  -> set EmailConfirmed only when mailbox ownership proof exists
  -> revoke existing sessions/refresh tokens
```

Invitation tokens must not activate accounts that are already `Active`, `Disabled`, or `Suspended`.

Acceptance is serialized by token. Account activation, invitation acceptance,
and revocation of existing refresh sessions commit in one transaction. A
failure while revoking sessions rolls back activation instead of leaving a
partially accepted account.

Accepting a valid invitation token proves token possession. It does not always prove mailbox ownership.

Acceptance is not idempotent. If a token has already been accepted, return an explicit error such as:

```text
400 Invitation already accepted.
```

If a token is expired or revoked, return an explicit error and require management to create a new invitation.

Email delivery proof uses:

```text
AccountInvitation.EmailSentAt
```

as the backend-email delivery proof for invitation acceptance. If `EmailSentAt` is null, accepting the invitation must not set `Account.EmailConfirmedAt`.

## Temporary Password Fallback

Temporary password creation is not the default flow.

It is allowed only when:

```text
CreateInvitation = false
```

For local login without invitation:

```text
InitialPassword is required
```

For invitation onboarding:

```text
InitialPassword is not allowed
```

Reason: if admin creates the password, admin knows the user's password. That contradicts the invitation-link onboarding rule.

Current backend behavior does not force password change for active accounts created with `InitialPassword`. Therefore, do not use active account + temporary password as the normal onboarding method.

Temporary-password onboarding is not part of the current contract. It requires
a separate forced-password-change lifecycle and restricted authenticated access;
do not infer that behavior from `InitialPassword`.

## Account Status Rules

| Status | Meaning |
| --- | --- |
| `Invited` | Account exists but cannot log in until invitation is accepted |
| `Active` | Account can log in through enabled auth methods |
| `Disabled` | Account is blocked by management action |
| `Suspended` | Account is blocked due to security or operational policy |

Login must reject non-`Active` accounts.

## Related Docs

- [API Surface Rules](API_SURFACE_RULES.md)
- [Authorization Rules](AUTHORIZATION_RULES.md)
- [Data Modeling Rules](../data/DATA_MODELING_RULES.md)
