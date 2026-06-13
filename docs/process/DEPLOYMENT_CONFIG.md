# Deployment Configuration

This document lists the minimum backend configuration that must be provided outside source control before running a deployed environment.

## Search Keywords

`deployment`, `backend config`, `environment variables`, `appsettings`, `JWT`, `database connection`, `Firebase`, `SMTP`, `PayOS`, `PORT`, `health`, `info`

## Configuration Source

The WebAPI loads configuration in this order:

```text
appsettings.json
appsettings.{Environment}.json
environment variables
```

Use environment variables or deployment secrets for real credentials. Do not rely on sample values in `appsettings.json` outside local development.

## Required Runtime Settings

| Area | Configuration key |
| --- | --- |
| Database | `ConnectionStrings__IceBot_DB` |
| JWT secret | `Authentication__Jwt__Secret` |
| JWT issuer | `Authentication__Jwt__Issuer` |
| JWT audience | `Authentication__Jwt__Audience` |
| Email host | `Email__Host` |
| Email port | `Email__Port` |
| Email sender | `Email__From` |
| Email display name | `Email__DisplayName` |
| Email username | `Email__UserName` |
| Email password | `Email__Password` |
| Email TLS toggle | `Email__EnableSsl` |
| Password reset frontend URL | `Email__PasswordResetBaseUrl` |
| Invitation frontend URL | `Email__InvitationBaseUrl` |
| Firebase enabled flag | `Firebase__Enabled` |
| Firebase credentials path | `Firebase__CredentialsPath` |
| PayOS client id | `PayOS__ClientId` |
| PayOS API key | `PayOS__ApiKey` |
| PayOS checksum key | `PayOS__ChecksumKey` |
| PayOS base URL | `PayOS__BaseUrl` |
| PayOS return URL | `PayOS__ReturnUrl` |
| PayOS cancel URL | `PayOS__CancelUrl` |
| Browser frontend origins | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... |
| Expose stack traces | `ErrorHandling__ExposeStackTrace` |
| Expose sensitive request/response data in logs | `Logging__ExposeSensitiveData` |
| Public port, if hosting platform injects it | `PORT` |

## Operational Endpoints

Use these for deployment checks:

```text
GET /health
GET /info
```

`/info` may include build metadata if these values are provided:

```text
BUILD_COMMIT
BUILD_TIME
```

## Notes

- CORS allows any origin only in Development when no origin is configured. Deployed environments must set `Cors__AllowedOrigins__0` and additional indexed values as needed.
- Firebase can be disabled with `Firebase__Enabled=false`, but Google/Firebase login paths will then return service-unavailable behavior.
- SMTP failures must not make account onboarding unrecoverable; admins can resend invitations.
- PayOS webhook/payment behavior depends on correct public return/cancel URLs and checksum key.
- Set `ErrorHandling__ExposeStackTrace=false` and `Logging__ExposeSensitiveData=false` in deployed environments.
