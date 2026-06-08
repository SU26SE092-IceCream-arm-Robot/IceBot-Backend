# Identity Onboarding Rules

This document is the backend source of truth for internal account onboarding, invitation links, and temporary password fallback.

## Search Keywords

`identity onboarding`, `account onboarding`, `admin creates account`, `internal account invitation`, `invitation link`, `accept invitation`, `CreateInvitation`, `SendInvitationEmail`, `InitialPassword`, `temporary password`, `Invited account`, `Active account`, `/api/v1/management/accounts`, `/api/v1/authentication/accept-invitation`

## Purpose

Internal accounts are created by authorized management users. Public signup is disabled for internal system accounts.

The default onboarding method is:

```text
admin creates account
  -> backend creates invitation link
  -> user accepts invitation
  -> user sets password
  -> account becomes Active
```

Do not use username + password delivery as the default onboarding flow.

## Default Flow

Default request behavior:

```text
CreateInvitation = true
SendInvitationEmail = true
```

Flow:

```text
POST /api/v1/management/accounts
  -> create Account
  -> Status = Invited
  -> create AccountInvitation
  -> return invitation token/link
  -> optionally send invitation email
```

The management response should include invitation details when an invitation is created:

```text
invitationToken
invitationUrl
expiresAt
emailSent
```

`invitationUrl` is available only when `Email:InvitationBaseUrl` is configured. If the URL is not configured, the token can still be returned and the frontend may construct the route when it owns the accept-invitation URL.

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

## Email Failure

SMTP failure must not make onboarding unrecoverable.

If invitation email delivery fails:

```text
account remains Invited
invitation remains usable
response includes invitation link/token
emailSent = false
```

The management UI can show a warning and let the admin copy the link manually or create another invitation later.

Do not leak raw SMTP exception details to the API response. Log the server-side exception instead.

## Create Or Regenerate Invitation

Management can create a new invitation for an account that is still `Invited`.

Route direction:

```text
POST /api/v1/management/accounts/{accountId}/invitation
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
  -> revoke previous active invitations after the new invitation is created
```

This route means "create a new invitation link". It is not only "resend email".

## Accept Invitation

Accept invitation is public because the invited user may not be logged in.

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
  -> set EmailConfirmed
  -> set account Status = Active
  -> mark invitation Accepted
  -> revoke existing sessions/refresh tokens
```

Invitation tokens must not activate accounts that are already `Active`, `Disabled`, or `Suspended`.

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

If the project later needs temporary password onboarding as a first-class flow, add explicit fields such as:

```text
ForcePasswordChange
PasswordChangedAt
PasswordSetByAccountId
```

and restrict authenticated API access until the user changes the password.

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
- [Data Modeling Rules](DATA_MODELING_RULES.md)
