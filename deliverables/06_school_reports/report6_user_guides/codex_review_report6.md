# Codex Review — Report 6 Software User Guides

## Review scope

This file contains review comments only. `report6_user_guides.md` and all comparison baselines were left unchanged.

The review compares Report 6 with the university template notes, the SRS and RTM, UML and database-design baselines, Reports 3–5, repository-evidence inventories, and the team open-question register. Repository presence is treated as evidence that an item exists in the workspace, not proof that it belongs in an approved release package or that an installation procedure has been executed successfully.

Severity labels:

- **Critical** — could cause an unsafe installation, false release claim, or materially false user instruction.
- **Major** — substantial template, operability, coverage, or evidence gap.
- **Moderate** — ambiguity or overstatement likely to mislead a reviewer/operator.
- **Minor** — presentation or maintainability improvement.

## Overall assessment

Report 6 follows the required university heading structure and is disciplined about uncertainty. It does not pretend that a release manifest, deployment runbook, frontend manual, screenshots, credentials, or physical robot procedure already exists. Release-specific domains, ports, environment variables, secrets, people, versions, and commands are visibly marked for team completion. Its Cloud/Edge/payment boundary is also appropriately cautious.

The document is nevertheless a **release and user-guide framework**, not a final operational guide. The package table is a candidate inventory without versioned artifacts; installation steps cannot yet be followed end to end; and the User Manual contains backend-oriented workflow summaries rather than screen-level user instructions. Its top-level Markdown structure can be converted to DOCX, but completing the UI/manual content after conversion would require material insertion and layout work unless screenshot and screen-step placeholders are added now.

## 1. University template compliance

### R6-01 — Positive finding — Required top-level structure is present

Report 6 contains the required Record of Changes; Deliverable Package; Installation Guides with System Requirements and Installation Instruction; and User Manual with Overview and workflow subsections. The nine-step installation framework, expected results, warnings, and troubleshooting table are structurally appropriate.

### R6-02 — Major — Deliverable inventory lacks the template’s explicit version field

The template expects a deliverable-package inventory with item description and version. Report 6 combines availability, qualification, and unresolved version information in a `Status / Notes` cell. This makes it difficult to freeze and audit the final package.

**Recommendation:** Add separate columns for filename/path, artifact/document version, source commit/build, checksum, owner, approval status, and confidentiality. Do not populate these from current workspace state unless the release owner approves the exact artifact.

### R6-03 — Major — Report 2 and the final Report 7/package are not explicit inventory items

The package table lists Reports 1, 3, 4, 5, and 6 plus schedule/backlog and presentation placeholders, but it does not explicitly list the Project Management Plan (Report 2) or the final consolidated Report 7/final report. Both are relevant to the university release package described by the template notes.

**Recommendation:** Add explicit candidate rows for Report 2 and Report 7, with `Not available / [Needs Team Review]` where appropriate. Keep the package index distinct from the final report itself.

### R6-04 — Major — Required UI screenshots are acknowledged but not structurally reserved

The template expects step-by-step workflows with UI screenshots. Report 6 correctly refuses to invent screenshots, but it provides only a global completion note. There are no per-step figure placeholders, captions, screen identifiers, or screenshot ownership fields.

**Recommendation:** Add a consistent table under every human workflow with Screen ID, approved screen name, navigation, user action, expected visible result/error, screenshot placeholder/caption, UI build, and owner. This allows later screenshots to be inserted without reorganizing the document.

### R6-05 — Moderate — Record of Changes and cover metadata remain incomplete

Official project name/code/group/date and accountable author/date are clearly marked, but remain submission blockers.

**Recommendation:** Obtain them from the project/report owner; do not infer them from repository history.

## 2. Deliverable package realism

### R6-06 — Positive finding — Candidate versus approved artifacts are clearly distinguished

The introduction and package table explicitly say that workspace availability is not release approval. Missing schedules, defects, executed tests, slides, checksums, package locations, and sign-off are not invented. Retain this distinction.

### R6-07 — Moderate — “Backend API” is a capability, not yet a package artifact

The API row describes implemented REST, GraphQL, and SignalR surfaces, but an API is not independently deliverable unless the package includes a concrete artifact such as an OpenAPI document, GraphQL schema, client contract, examples, or published endpoint documentation. Those artifacts are currently unresolved.

**Recommendation:** Rename the row to the exact artifact once produced, or mark it as a release-package gap. Do not imply that the SRS/evidence inventory is a versioned client contract.

### R6-08 — Moderate — “Database scripts and migrations” need a packaged artifact boundary

EF migrations and manual-step classes exist in source, but the row does not identify whether the delivered database artifact is source, generated SQL, a migration bundle, an image startup action, or an operator runbook. These are not interchangeable and have different review/rollback implications.

**Recommendation:** Define the approved migration deliverable and checksum it separately. Include invocation order, preflight/manual-step treatment, required privileges, backup, rollback/forward-fix policy, and evidence record.

### R6-09 — Moderate — Configuration files must not be packaged without a secret review

The report appropriately requests non-secret templates, but “current repository configuration” should not automatically become a release artifact. Environment-specific settings may contain unsafe defaults or accidentally retained secrets.

**Recommendation:** Deliver an approved redacted configuration template and a separate key catalogue containing names, purpose, required/optional status, validation, and secret source—not secret values. Record a secret scan before packaging.

### R6-10 — Minor — The Report 5 inventory row is accurate but fragile

The stated 62 high-level cases currently matches Report 5. A manually copied count will become stale if Report 5 changes.

**Recommendation:** Prefer a version reference and generated manifest metadata over embedding counts, or verify the count during final package assembly.

### R6-11 — Moderate — Known questions are not equivalent to release-known issues

`open_questions.md` is useful review input, but unresolved design/product questions, accepted limitations, test defects, and release-known issues have different ownership and disposition.

**Recommendation:** Package them as separate artifacts. Only verified defects/accepted limitations should appear in release known issues; unresolved blockers should prevent release or carry formal conditional approval.

## 3. System and installation requirements

### R6-12 — Positive finding — Unsupported infrastructure details are visibly qualified

Exact .NET/container versions, operating systems, CPU/RAM/storage, scaling, domains, ports, certificates, reverse proxy, firewall, CORS, limits, credentials, provider endpoints, and deployment profiles are consistently marked `[Needs Team Review]`. PostgreSQL 17 and `IceBotDB` are correctly described as current configuration rather than permanent production requirements.

### R6-13 — Moderate — Architecture layering is not a server requirement

`WebAPI → Infrastructure → Application → Domain` is a supported compile-time architecture fact, but it does not tell an installer what runtime/software/hardware is required.

**Recommendation:** Move layer direction to an architecture reference and replace it in System Requirements with exact approved runtime, OS/container, architecture, CPU, memory, disk, network, and capacity requirements when known.

### R6-14 — Major — Environment-variable/configuration catalogue is incomplete

Only the two database keys are named exactly. PayOS, Firebase/Google, FCM, MinIO, MQTT, email, jobs, retention, logging, health, and Edge security remain broad areas without exact key names, formats, defaults, required/optional rules, or validation behavior. They are clearly marked, so this is not an invented-detail problem; it is an installation-readiness gap.

**Recommendation:** Generate and review a non-secret configuration matrix from the approved release code/configuration. Include environment-variable name, configuration path, description, type/format, default, required profile, secret classification, restart/reload behavior, and example placeholder.

### R6-15 — Major — Network/port/domain matrix is missing

The report correctly flags exact domains and ports as unknown, but an installer needs a directional connectivity matrix, not a single prose placeholder.

**Recommendation:** Add Source → Destination → protocol/port → DNS name → TLS/mTLS → authentication → inbound/outbound firewall → environment owner. Include backend HTTP, PostgreSQL, MinIO/S3, MQTT, PayOS callback/outbound API, Firebase/Google/FCM, clients, and Edge.

### R6-16 — Major — Installation instructions remain a framework, not an executable runbook

Steps 3–8 depend on unknown commands, versions, startup order, service identities, configuration keys, migration invocation, rollback, and provider procedures. The report explicitly acknowledges this, so it does not overclaim availability; however, the sequence cannot yet be followed or independently verified.

**Recommendation:** Keep the framework in the school report, but link every step to a versioned executable runbook once approved. Add commands only after testing them against the declared release/environment.

### R6-17 — Moderate — The build command is evidence for compilation, not deployment

`dotnet build IceBot.slnx` is a supported direct compile check from the repository operational guide. It does not produce or validate an approved deployment artifact by itself.

**Recommendation:** Retain it as a build check, then document publish/image commands, artifact contents, runtime entry point, image base/digest, startup arguments, and release gates separately.

### R6-18 — Major — Manual migration-step execution remains unresolved

The evidence confirms manual-step classes but leaves unclear whether/how they are invoked relative to standard EF migration execution. Report 6 says to execute preflights according to a future approved runbook, which is appropriately cautious, but the final guide must not leave this decision to the installation operator.

**Recommendation:** Resolve the invocation order and authority before release. State whether failure means stop/restore, remediate data then retry, or use another approved forward-fix path. Capture successful execution evidence.

### R6-19 — Moderate — Release blockers should be resolved before operator installation

Step 2 instructs the installer to review `open_questions.md`. An installer should not decide architecture, authorization, schema, payment, or Edge semantics during deployment.

**Recommendation:** The release owner should triage questions before packaging and publish only the accepted blockers/waivers relevant to operators. The runbook should contain decisions, not ask operators to interpret the review register.

### R6-20 — Moderate — “Signed installation manifest” is a proposed process, not repository evidence

The Step 1 expected result mandates a signed installation manifest. This is sensible governance, but neither the supplied evidence nor template establishes that exact approval artifact.

**Recommendation:** Mark it `[Recommendation]` or `[Needs Team Review]` until the team adopts it, then identify the approving roles and signature mechanism.

### R6-21 — Critical — Backup/rollback should not be merely “verified or formally accepted” for stateful production changes

The installation checklist allows backup/rollback to be either verified or “formally accepted.” It is unclear whether that means accepted without verification. For a production database migration, accepting the absence of a workable recovery/forward-fix procedure is a material operational risk.

**Recommendation:** Define environment-specific gates. A disposable development environment may waive backup; a stateful staging/production migration should require tested recovery/forward-fix evidence and an authorized go/no-go decision.

### R6-22 — Moderate — Disabled-integration behavior is not defined

Step 4 expects disabled integrations to have explicit supported behavior, but no matrix says which integrations are mandatory per deployment profile or what the application does when each is absent.

**Recommendation:** Add per-profile Required/Optional/Disabled status, startup behavior, degraded features, health status, retry/noise behavior, and operator alerting.

### R6-23 — Moderate — Health verification is too abstract for a final runbook

`/health...` and `/info` are supported surface families, but the exact probes, authentication/disclosure, dependency checks, expected status/body, timeout, and readiness criteria remain unknown.

**Recommendation:** Record exact safe endpoints and assertions per deployment profile. Do not expose sensitive configuration/version data through public probes.

## 4. User-workflow clarity and coverage

### R6-24 — Major — Current “User Manual” sections are workflow outlines, not usable instructions

The workflows are clear enough for team review of intent and backend sequence, but not for a user to perform a task. They generally lack client/application name, entry screen, navigation path, field labels, required values, validation messages, visible success/failure state, recovery steps, and screenshot.

**Recommendation:** Label the current content “Workflow design for team review” until approved UI details are supplied. Later add human-facing steps without replacing the backend outcome/boundary notes.

### R6-25 — Major — Human instructions and machine integration behavior are mixed

Local Edge Backend and PayOS appear in the User Manual actor table alongside people, and Section 3.5 describes a protocol sequence rather than a human workflow. Operators need monitoring/recovery instructions; system integrators need a contract/runbook; end users need screen steps.

**Recommendation:** Separate:

- end-user/management client manual;
- operator support and recovery guide;
- Edge/provider integration guide.

Cross-reference shared flows rather than forcing protocol actors into the human user manual.

### R6-26 — Major — Several major human workflows are missing or too compressed

The report should add or explicitly exclude step-level guides for:

- local/external sign-in, token/session lifecycle, forgot/reset/change password;
- own profile/effective-access and notification-device management;
- account list/update/disable and administrator password reset;
- role-scope lookup and tenant-tree navigation;
- device and execution-endpoint provisioning, credential rotation/revocation, and MQTT credential reconciliation;
- kiosk status/telemetry/history and dashboard reads;
- payment-method catalogue status management;
- alert and maintenance-ticket lifecycle;
- detailed package/configuration recovery and operator escalation.

**Recommendation:** Map each manual subsection to FR IDs and identify whether it is UI, API/operator, automated, or external-system behavior.

### R6-27 — Moderate — Authorization wording remains too broad for task execution

Phrases such as “where permitted” and “according to the exact policy/scope” prevent false authorization claims, but do not tell reviewers which actor should perform a step. Report 3/RTM also leaves exhaustive authorization coverage open.

**Recommendation:** Add a reviewed workflow authorization matrix. Until then, keep roles qualified and do not convert summary actor labels into UI visibility rules.

### R6-28 — Moderate — User-visible error and recovery guidance is sparse

Most workflows state that invalid actions are rejected but do not explain what the user sees or should do next. Exact messages are unavailable because the UI/application-message catalogue is missing.

**Recommendation:** Add placeholders for validation/error code, approved user-facing message, safe corrective action, escalation path, and screenshot. Do not invent wording from HTTP status codes.

### R6-29 — Moderate — Customer “request support” wording lacks a direct customer workflow

The overview says the customer can request support through the tablet workflow, and the order steps say to request staff support when cancellation is unavailable. The evidence confirms an order response flag such as `RequiresStaffSupport` and staff-managed incidents/refunds, but the supplied inventory does not establish a customer support-request endpoint or UI procedure.

**Recommendation:** Change this to “contact staff through the approved channel `[Needs UI/Team Review]`” or cite an actual client/support contract. Do not imply the backend exposes a customer support request that is not evidenced.

### R6-30 — Positive finding — Backend-only evidence is not converted into invented screen detail

The report avoids fabricated page names, menu positions, buttons, screenshots, and messages. It consistently marks client navigation/presentation `[Needs UI/Team Review]`. Retain this discipline.

## 5. Payment, Edge, and physical robot boundaries

### R6-31 — Positive finding — Payment confirmation is correctly authoritative on the backend

The customer flow distinguishes displayed QR/provider interaction from verified backend payment status and marks late/conflicting callback precedence and exact transaction boundaries unresolved. The manual refund boundary is also stated without inventing automatic provider payout.

### R6-32 — Positive finding — Cloud evidence is not presented as physical proof

Report 6 repeatedly says that deployment/package reports, command acknowledgement, timeout, and execution reports do not independently prove installation, robot motion, dispensed quantity, quality, or safety. MQTT is correctly described as best-effort notification rather than the durable command payload. Retain these statements.

### R6-33 — Moderate — Edge-to-robot workflow remains an external assumption

The sequence diagram depicts Edge invoking robot/devices. The report labels robot/device implementation outside backend evidence and marks physical procedures unresolved, which is appropriate. For an as-built final guide, however, the diagram still needs an explicit visual annotation that the interaction is supplied by the Edge/robot owner.

**Recommendation:** Label the external segment `[Assumption / external system evidence required]` and replace it only with approved Edge/robot documentation.

### R6-34 — Moderate — Acknowledgement and report instructions are integration contracts, not operator actions

Exact acknowledgement values are evidence-supported, but a human operator does not normally choose them. Including them as numbered “operation” steps can confuse user responsibility.

**Recommendation:** Move exact payload/status behavior to the Edge integration guide. In the operator manual, explain how to observe acknowledgement/report state and when to escalate without inferring physical outcome.

### R6-35 — Moderate — Runtime-menu source remains correctly open but blocks final tablet instructions

The evidence includes a Cloud runtime-menu API while the deployed-system truth describes Edge as the tablet’s normal source. Report 6 correctly marks Edge/Cloud/both unresolved.

**Recommendation:** Treat this as a blocker for the final customer/tablet installation and workflow guide. Diagram normal and fallback paths only after the team approves the deployed contract.

## 6. Missing domains, credentials, ports, variables, and screenshots

### R6-36 — Positive finding — Missing values are clearly disclosed

The report explicitly identifies missing domains, callback addresses, TLS/certificates, ports, firewall/reverse-proxy rules, credentials and secret delivery, environment variables/config keys, client base URLs, versions, screenshots, and physical procedures. No live secret or fabricated value appears.

### R6-37 — Major — Global disclosure is not enough for completion control

Because missing values are scattered through prose, a reviewer cannot reliably prove that all placeholders were resolved for a particular deployment profile.

**Recommendation:** Add a single completion register with placeholder ID, section/step, value owner, environment, secret/non-secret classification, blocking status, due date, approved source, and resolution evidence. Final review should fail if unresolved blocking placeholders remain.

### R6-38 — Moderate — Credentials need lifecycle instructions, not only initial values

The report asks for issuance and secret sources but final installation also needs generation, secure handoff, activation, rotation, overlap/grace period, revocation, expiry, audit, backup/recovery, and compromise response for JWT/provider, mTLS, ECDSA, MQTT, object-storage, and database credentials.

**Recommendation:** Add a credential lifecycle matrix or reference an approved security runbook. Never put actual secrets in Report 6.

## 7. DOCX conversion readiness

### R6-39 — Positive finding — Top-level Markdown maps cleanly to the university DOCX

Headings, numbered installation steps, tables, warnings, expected results, and workflow subsections can be transferred without changing the top-level template structure.

### R6-40 — Major — Final UI completion would still require material layout work

The absence of per-workflow screen/screenshot slots, screen IDs, captions, and UI-build references means screenshots cannot simply be dropped into the current document. Several broad subsections will also become unwieldy once detailed screen steps are added.

**Recommendation:** Introduce the final repeatable workflow layout now: purpose, actors/roles, prerequisites, numbered steps, expected visible result, errors/recovery, screenshot figure(s), backend boundary, and FR reference. Split broad sections before DOCX conversion.

### R6-41 — Moderate — Mermaid diagrams require a rendering plan

Raw Mermaid may not render in the university Word template. Large diagrams and tables also need page-orientation and readability checks.

**Recommendation:** Render approved diagrams to high-resolution images, assign figure numbers/captions/alt descriptions, preserve editable sources separately, and verify cross-references and page breaks in DOCX/PDF.

### R6-42 — Moderate — Internal repository paths should not be the only final references

Workspace-relative paths are useful during drafting but may not exist for a report recipient.

**Recommendation:** In the final package, reference manifest item IDs, document versions, appendix numbers, or packaged relative paths that remain valid after archive extraction.

## 8. Recommended open questions

Add these to `open_questions.md` if an equivalent entry is not already present:

| Topic | Question |
|---|---|
| Release manifest ownership | Who approves the exact artifact list, versions, checksums, confidentiality, and supersession rules? |
| Reports 2 and 7 | What are their approved filenames, versions, owners, and package locations? |
| Deployment profile matrix | Which components/integrations/jobs are required, optional, or disabled in development, test, staging, and production? |
| Configuration contract | What are the exact non-secret configuration keys, formats, defaults, and validation behavior for the approved release? |
| Network contract | What domains, protocols, ports, TLS/authentication, and firewall directions are approved per environment? |
| Migration procedure | How and when do manual preflights run, and what is the approved failure/rollback/forward-fix process? |
| Backup/recovery gate | Which environments require tested restore/rollback evidence before deployment? |
| UI/manual ownership | Which approved client builds and screen inventory supply navigation, field names, messages, and screenshots? |
| Tablet runtime-menu path | Is the normal source Edge, Cloud, or both, and what is the supported fallback behavior? |
| Customer support channel | Is there a supported tablet/customer support-request workflow, or only staff contact and backend `RequiresStaffSupport` state? |
| Edge/robot guide ownership | Who supplies installation, credential, update/rollback, safety, calibration, and emergency procedures? |
| Credential lifecycle | Who owns issuance, secure handoff, rotation, revocation, expiry, recovery, and compromise response for every credential family? |
| Installation acceptance | Who signs installation verification, and what evidence/waivers are permitted for each deployment profile? |

## 9. Revision priority

1. Freeze a versioned release-package manifest, including explicit Report 2 and Report 7 entries.
2. Approve deployment profiles and produce configuration, network, credential, environment, migration, backup, and rollback matrices.
3. Convert the installation framework into a tested, versioned runbook with exact safe commands and evidence.
4. Separate human user instructions, operator recovery, and Edge/provider integration contracts.
5. Map human workflows to FRs and add screen-step/screenshot placeholders before DOCX conversion.
6. Resolve the tablet runtime-menu source and customer-support channel before finalizing customer instructions.
7. Render diagrams, validate links/cross-references, run a placeholder audit, and verify the final DOCX/PDF against the approved release build.

## Final disposition

**Suitable for team review as a cautious Report 6 framework; not ready as a final release package, installation runbook, or end-user manual.** The report’s uncertainty markers, secret handling, and physical-outcome boundaries are strong. Final readiness requires a versioned manifest, approved executable deployment procedures, complete configuration/network/credential data, separated user/operator/integration guidance, and client-owned screen instructions and screenshots.
