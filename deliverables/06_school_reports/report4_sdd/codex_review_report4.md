# Codex Review — Report 4 Software Design Document

## Review scope and method

This file contains review comments only. The reviewed Report 4 and all baseline deliverables were left unchanged.

The review compares `report4_sdd.md` with the university structure recorded in `template_structure_notes.md`, the UML deliverables, the conceptual/logical/physical database designs, the SRS and traceability matrix, the repository-evidence inventories, Report 3, and the team open-question register. “Supported” below means that the claim is demonstrated by those baselines; it does not mean that the proposed design has been approved by the team.

Severity labels:

- **Critical** — likely to make the design materially false or unverifiable.
- **Major** — important template, coverage, or evidence gap that should be resolved before submission.
- **Moderate** — wording, precision, or presentation issue that can mislead a reviewer.
- **Minor** — editorial or traceability improvement.

## Overall assessment

Report 4 has the correct top-level university headings and is strongest where it separates Cloud command/report evidence from physical robot outcomes. It is not yet a complete Software Design Document under the referenced template. The architecture and database sections are useful summaries, but the package design is too aggregated, the database catalogue is representative rather than complete, and only the order/payment and robot/Edge areas contain sequence-level design. Catalog, inventory, production configuration, identity, operations, and synchronization are grouped into broad narrative sections without the feature-level class specifications and sequence diagrams expected by the template.

The report should remain a draft until the major comments below are addressed or explicitly accepted as documented limitations.

## 1. University template structure

### R4-01 — Major — Required detailed-design structure is only partially implemented

**Affected area:** Section 3, especially 3.3–3.5.

The template calls for a detailed-design subsection per feature/function, supported by class diagrams or class specifications and one or more sequence diagrams. Report 4 provides two real flow designs—order/payment and robot/Edge—but collapses the remaining system into three large component tables. Those tables do not constitute detailed feature design and do not provide operation signatures, collaboration rules, failure paths, or sequence behavior.

**Recommendation:** Split Section 3 by a defensible set of major workflows from the SRS/functional inventory. At minimum add detailed coverage for authentication/authorization, catalog/runtime-menu publication and retrieval, inventory/stock movement, production configuration publication/deployment, artifact/package lifecycle, payment recovery/refund, operational incidents, and sync ingestion/retry/dead-letter handling. If a flow is deliberately out of scope, list it as an explicit Report 4 coverage limitation rather than implying complete design coverage.

### R4-02 — Major — Package-diagram requirement is met only at project-group level

**Affected area:** Section 1.2.

The template expects package diagrams per subsystem and a package/namespace description table. The report presents one high-level diagram of project layers and grouped functional labels. Several labels combine multiple real contexts (for example, `Catalog / SalesCatalog / Inventory`) or describe application capabilities rather than actual packages/namespaces. This is useful architecture orientation but is not sufficient package design.

**Recommendation:** Either add package diagrams for the principal subsystems/bounded contexts and name the actual project/namespace ownership, or relabel the existing diagram as a logical component/context diagram. Add a table distinguishing physical project, namespace/package, bounded context, responsibility, and allowed dependencies.

### R4-03 — Major — Database-description table is incomplete for the template

**Affected area:** Section 2.3.

The report explicitly provides representative entity groups rather than a complete table catalogue. The university template expects table descriptions including primary and foreign keys. The report’s general statement that `Id` is the primary key “unless noted” does not enumerate exceptions such as composite/join keys and does not provide a complete FK catalogue.

**Recommendation:** Add a complete table appendix or a clearly referenced complete physical catalogue. For each mapped table state PK, material FKs, nullability relevant to cardinality, and important unique/check/index constraints. Do not use the 98 `DbSet` count as a table count until the model/database reconciliation question is closed.

### R4-04 — Moderate — Submission metadata and change record remain placeholders

**Affected area:** Cover metadata and Section I.

The official project name, code, group, location/date, and change history are not submission-ready. The initial change row does not identify an accountable author/reviewer.

**Recommendation:** Treat these as finalization blockers in the review checklist. Do not invent the values; obtain them from the team/university submission owner.

### R4-05 — Moderate — No explicit mapping from Report 3 requirements to detailed designs

The report cites selected FRs, but does not show which SRS requirements are designed by each class/sequence section or which requirements lack Report 4 coverage. This makes completeness difficult to review against Report 3 and the RTM.

**Recommendation:** Add a compact “design coverage” table mapping each detailed design/diagram to FR/NFR identifiers and explicitly list uncovered requirements. Avoid claiming that a package “owns” every cited FR when it merely participates in the workflow.

## 2. Architecture and bounded-context accuracy

### R4-06 — Moderate — The dependency diagram mixes compile-time dependencies and runtime integrations

**Affected area:** Section 1.1 architecture diagram.

`WebAPI → Infrastructure → Application → Domain` is supported as the project dependency direction, but the same diagram also uses arrows for runtime calls to PayOS, Firebase/Google/FCM, MQTT, PostgreSQL, and MinIO. The arrow semantics are therefore ambiguous. In particular, an inbound PayOS webhook enters through WebAPI; drawing PayOS only against Infrastructure obscures the inbound adapter and may imply that the provider calls the infrastructure project directly.

**Recommendation:** Separate the container/runtime diagram from the code-dependency diagram, or add a legend defining each arrow type. Show inbound provider callbacks through the WebAPI boundary and outbound provider adapters in Infrastructure.

### R4-07 — Moderate — Scheduling ownership is imprecise

**Affected area:** Application and Infrastructure component descriptions.

The report attributes scheduled workflows to Application. The evidence supports hosted/background services in Infrastructure that invoke application commands/handlers. Application may own workflow logic, but it does not own the process scheduler/host.

**Recommendation:** State separately that Infrastructure owns hosting, timers, leases/advisory-lock coordination, and external I/O, while Application owns the invoked use-case logic.

### R4-08 — Major — Functional packages are presented as if they align cleanly with bounded contexts

**Affected area:** Section 1.2 package table.

`Dashboard` and `EdgeIntegration` are application/integration capabilities, not equivalent domain bounded contexts. `ProductionExecution` owns execution projections, but Report 4’s mapping associates it broadly with robot command, MQTT, realtime, GraphQL, and background-job requirements. Similarly, the `Sync` row spans requirements whose implementation ownership lies across IoT REST controllers, EdgeIntegration MQTT, realtime delivery, queries, and hosted jobs.

**Recommendation:** Distinguish domain context ownership from participating application/infrastructure packages. Use “owns,” “orchestrates,” “adapts,” and “reads” deliberately. Recheck the FR mappings against `functional_inventory.md` instead of assigning broad ranges to one package.

### R4-09 — Moderate — Identity and notification providers are over-grouped

**Affected area:** Architecture component list/diagram.

Firebase/Google identity verification and FCM push delivery are separate responsibilities and adapters, even if supplied by related vendors. Grouping them as one component weakens the trust-boundary and failure-mode design.

**Recommendation:** Show external identity providers/token verification separately from push notification delivery, including which direction data flows and which application capability consumes each adapter.

### R4-10 — Moderate — Status vocabulary is inconsistent with the evidence package

Report 4 uses variants such as `[Needs Team Review]`, `[Needs Team/UI Review]`, and `[Needs Review]`, while the evidence/SRS package relies on categories such as `[Inferred]`, `[Assumption]`, `[Unclear]`, and open questions. The new labels have no legend and blur “not evidenced” with “awaiting approval.”

**Recommendation:** Add a status legend and normalize labels. Use `[Inferred]` for a conclusion supported indirectly, `[Assumption]` for a proposed design premise, `[Unclear]` where repository evidence is insufficient, and `[Open Question]` for a decision that must be answered.

## 3. UML and detailed-flow consistency

### R4-11 — Major — The Report 4 class diagram states stronger minimum cardinalities than the evidence

**Affected area:** Section 3.1 class diagram.

Associations such as Order-to-PaymentTransaction, PaymentTransaction-to-PaymentCallback, and PaymentTransaction-to-Refund are written as `many`, which can be read as one-or-more. An order can exist before payment transactions; a payment transaction can have no callback; and most payments have no refund. Parent-side collections should therefore normally be zero-to-many unless a specific invariant establishes a positive minimum. The same precision issue applies to OrderItem collections if the diagram is intended as a persistence model rather than an aggregate-creation invariant.

**Recommendation:** Use explicit `0..*`, `1..*`, `0..1`, and `1` multiplicities, and state whether the diagram models database optionality, aggregate invariants, or runtime workflow state. Align each relation with `class_diagram.md`, `erd.md`, and the mapped FK nullability.

### R4-12 — Moderate — The class diagram is a selective persistence projection, not a full class specification

**Affected area:** Sections 3.1 and 3.2.

The explanations say the classes/attributes are established by the baseline, but the diagrams omit much of the broader model in `class_diagram.md` and provide few or no methods, invariants, constructors, interfaces, or dependencies. Some types are simplified (for example a generic/string-looking command or execution status) without explaining whether this is presentation shorthand or the actual domain type.

**Recommendation:** Label each diagram as a workflow-specific subset. Add class-specification tables for the principal types with exact baseline type names, responsibilities, key operations, invariants, and collaborators. Mark deliberately simplified attribute types as such.

### R4-13 — Major — Runtime-menu source conflicts with, or fails to reconcile, the order-flow baseline

**Affected area:** Section 3.1 order sequence.

Report 4 depicts the tablet requesting the runtime menu from Cloud WebAPI. The repository truth/order-flow material describes an Edge-local participation in the tablet ordering flow, while the repository also exposes Cloud runtime-menu APIs. Report 4 does not reconcile whether the tablet’s normal source is Edge, Cloud, or a fallback path. This is a material deployed-system boundary question, not merely a diagram detail.

**Recommendation:** Mark the source as `[Unclear]` and add/retain an open question until the team defines the deployed tablet/Edge contract. If both paths exist, diagram the normal and fallback paths separately and cite the evidence for each.

### R4-14 — Major — Payment-to-command sequencing implies stronger atomicity and immediacy than proven

**Affected area:** Section 3.1 order sequence and explanation.

The straight-line sequence from payment webhook to paid/ready state to persisted `ExecuteOrder` command visually implies that all steps occur immediately and perhaps in one transaction. Elsewhere the report acknowledges that exact transaction boundaries are unresolved and that reconciliation handles missed dispatch. Those statements are inconsistent.

**Recommendation:** Separate the confirmed payment state transition from post-commit dispatch and reconciliation. Use `opt`/`alt` fragments for immediate dispatch, failure, and later repair. Mark the transaction boundary `[Unclear]` until verified. Do not imply guaranteed repair: retries can exhaust and require manual recovery.

### R4-15 — Moderate — Order design contains only a happy path

The SRS/evidence includes materially different states and recovery behavior, but the sequence omits duplicate/late/invalid callbacks, payment failure or expiry, cancellation, session recovery, dispatch failure, Edge rejection/busy response, command timeout, incidents, refund handling, and terminal/manual recovery.

**Recommendation:** Add failure-oriented sequence variants or a state/exception table. At least cover idempotent callback handling, partial failure after payment commit, retry exhaustion, and how operators observe/recover the order.

### R4-16 — Moderate — “Inventory revalidation” in checkout/runtime-menu wording needs evidence correction

**Affected area:** Sections 3.1 and 3.3.

The evidence distinguishes catalog/menu sellability from stock availability; runtime-menu sellability does not simply consult inventory. Wording that Cloud “revalidates inventory” before order creation, or that runtime-menu evaluation includes stock, risks inventing a stronger stock gate than implemented.

**Recommendation:** Name the exact validated conditions supported by the order handler (tenant/store/kiosk/menu/product/price/configuration as applicable). If stock is not queried, remove “inventory” or mark the intended gate `[Assumption]`.

### R4-17 — Moderate — Robot acknowledgement statuses need exact enum/contract traceability

**Affected area:** Section 3.2 robot sequence.

The sequence lists statuses such as Received, Accepted, Rejected, Busy, and Failed. The report should not present a normalized lifecycle as the exact API contract unless those exact values and transitions are present in the baseline contract. The current text also compresses command acknowledgement, execution reporting, and incident evidence into one conceptual response.

**Recommendation:** Cite the exact acknowledgement/report DTO or baseline enum, distinguish acknowledgement from later execution evidence, and label any normalized state model `[Inferred]`.

### R4-18 — Moderate — “Artifact access data” in the pull response is not established

**Affected area:** Section 3.2 sequence.

The report says the Cloud returns a pending command together with artifact access data. The evidence establishes command pull and artifact/package capabilities, but does not by itself establish that artifact access data is embedded in this particular pull response.

**Recommendation:** Replace this with the exact documented response fields, or mark it `[Unclear]` and describe artifact retrieval as a separate interaction until the contract is verified.

### R4-19 — Positive finding / retain — Physical robot outcomes are appropriately bounded

Report 4 correctly states that Cloud acknowledgements and reports are evidence received from Edge, not independently verified physical outcomes. It also treats execution timeout as an observation/operational gap rather than proof that the robot stopped or failed. Retain this language.

The diagrammed Edge-to-robot “run program” interaction is nevertheless outside the backend repository evidence. It should be visually annotated `[Assumption — physical/Edge implementation outside repository]`, especially if the document is read as an as-built design rather than a target design.

### R4-20 — Moderate — Idempotency is described too generically

**Affected area:** Robot/Edge and Sync explanations.

“Validate provenance and apply idempotently” is directionally supported, but identifiers differ by ingestion path. A generic `(SourceEventId, SequenceNumber)` rule should not be implied for every event/command/report without evidence.

**Recommendation:** Specify idempotency keys and uniqueness behavior per endpoint/message family. Where the key is not established, use `[Unclear]` and link it to the corresponding open question.

## 4. Database-design consistency

### R4-21 — Moderate — Subject-area count is inconsistent

**Affected area:** Section 2 abstraction summary.

Report 4 refers to fourteen subject areas, while the conceptual design’s numbered inventory contains thirteen. This is a straightforward factual mismatch unless an additional area has been added and named.

**Recommendation:** Recount and name the subject areas rather than relying on an unsupported total.

### R4-22 — Major — ERD overstates mandatory Kiosk ownership of Menu

**Affected area:** Section 2 ERD.

The ERD uses a mandatory Kiosk-to-Menu relationship, but the database inventory records nullable `Menu.KioskId` and supports broader organization/store/menu scope. The diagram therefore overstates both parent requirement and scope semantics.

**Recommendation:** Change the optionality to match the nullable FK and explain the alternative scope ownership. Do not infer exclusivity or mandatory ownership solely from the navigation name.

### R4-23 — Moderate — ProductionIncident cardinality should be stated as physical fact plus business question

**Affected area:** ERD/class diagram and database open questions.

The mapped model/index evidence permits multiple production incidents for an order item (and does not provide a unique OrderItem constraint). Therefore “zero-to-many physically” is stronger than a mere tentative guess. Whether the intended business rule is one incident per item, one per unit, or many remains open.

**Recommendation:** State the current physical cardinality as confirmed from the mapped schema, then separately mark the intended business maximum `[Open Question]`. Do not silently inherit a zero-or-one relation from an older ERD.

### R4-24 — Moderate — Soft-delete wording conflates global filters and filtered unique indexes

**Affected area:** Section 2.5.

“Most soft-deletable entities receive global query filters and soft-delete-aware unique indexes” can be read as though both mechanisms apply uniformly. The inventory supports convention-based filters and a limited set of six partial unique indexes, not a filtered unique index on every soft-deletable entity.

**Recommendation:** Separate the claims: identify the scope/known exceptions of global filters, enumerate the six confirmed partial unique indexes, and state that other uniqueness constraints may still block key reuse after soft deletion. Preserve the open question about complete convention coverage.

### R4-25 — Moderate — Key and FK summary hides important exceptions

**Affected area:** Sections 2.3–2.4.

The “PK is `Id` unless noted” convention is too broad for a physical design summary containing join/composite-key entities such as `AccountStores`. The report also does not consistently state FK nullability, which is necessary to support its ERD optionalities.

**Recommendation:** Add an exception table for composite/nonstandard keys and include nullability for every relationship shown in the ERD. Reconcile mapped entities without `DbSet` exposure before reporting a physical table count.

### R4-26 — Minor — Enum-backed partial-index predicates remain appropriately open but should not look final

The report correctly flags numeric enum predicates as needing verification. Ensure the physical DDL/index table retains `[Unclear]` or `[Open Question]` on those numeric values until enum-to-database mappings are verified; do not present the values as approved constants elsewhere in the report.

## 5. Breadth, duplication, and unsupported design detail

### R4-27 — Major — Three “detailed design” sections are too broad and too shallow

**Affected area:** Sections 3.3–3.5.

Each section combines several independently reviewable workflows and bounded contexts. For example, catalog, inventory, runtime menus, configuration publication, deployment, and execution consumption have different owners and failure modes. Tenant identity, role/scope authorization, and lockout similarly require different contracts. Operations, sync, realtime, jobs, logs, and advisory locks are not one feature.

**Recommendation:** Decompose by workflow, not by a broad list of nouns. Each subsection should identify actors, entry point, owning context, participating adapters, state changes, transaction boundary, idempotency/concurrency behavior, failures/retries, and linked requirements.

### R4-28 — Major — Important flows are missing

At minimum, the following evidence-backed areas lack adequate detailed design or an explicit exclusion:

- login/token verification, lockout, and role-plus-scope authorization;
- menu/catalog publication and runtime-menu retrieval;
- stock movement/reservation/adjustment and concurrency behavior;
- production configuration draft/publish/deploy gating;
- robot artifact authoring/import and package install/upgrade lifecycle;
- payment-session creation/recovery, callbacks, cancellation, refund, and reconciliation;
- Edge event ingestion, sequence/idempotency validation, retry, dead-letter, and manual recovery;
- alerts, maintenance, incidents, operational logs, and operator recovery;
- realtime/GraphQL read projections and their consistency boundary.

**Recommendation:** Prioritize flows by SRS criticality and submission scope, then record remaining omissions as open coverage questions.

### R4-29 — Moderate — Baseline summaries are duplicated without enough design decisions

The architecture and database sections repeat useful material from the truth map and database-design deliverables, but the detailed-design portion often stops at restating capabilities. An SDD should add design-specific decisions: interfaces, collaborations, lifecycle/state transitions, error paths, consistency, and deployment boundaries.

**Recommendation:** Keep concise baseline summaries and use references for repeated inventories. Spend the recovered space on behavior and decisions that are unique to Report 4.

### R4-30 — Moderate — Frontend navigation behavior is outside current evidence

**Affected area:** Section 3.4.

Statements about hiding unauthorized navigation are sensible UI guidance, but frontend implementation is outside this backend repository and no approved frontend design is established by the supplied evidence. A “Needs UI Review” marker does not make it an as-built claim.

**Recommendation:** Mark it `[Assumption — frontend design]` or move it to an open design question. Retain the supported security requirement that the server enforces authorization independently of UI visibility.

### R4-31 — Moderate — Publication/deployment/install language can imply stronger end-to-end guarantees

**Affected area:** Section 3.3.

The backend evidence supports publication/deployment records, commands, reports, and readiness gates. It does not prove that a physical package was installed, that a program was successfully executed, or that execution-driven inventory consumption is complete end to end. The report labels some of this uncertainty, but adjacent prose can still read as a finished physical workflow.

**Recommendation:** Apply the same observation-boundary language used in Section 3.2 to deployment/install and consumption outcomes. Use “records/reports,” “requests,” or “observes” rather than “ensures” or “completes,” and preserve the relevant open questions.

### R4-32 — Minor — Draft completion notice should state evidence limitations, not only editing tasks

The closing notice is useful but should identify the substantive blockers: incomplete deployed-system boundary, incomplete feature-level design coverage, unresolved table/schema reconciliation, cardinality/delete behavior, and unverified physical/Edge contracts.

**Recommendation:** Convert it into a short submission-readiness checklist linked to `open_questions.md` and the design-coverage table.

## 6. Required uncertainty and open-question updates

The following claims should be relabeled or explicitly carried into the open-question register if they are not already present:

| Topic | Recommended status | Question to resolve |
|---|---|---|
| Tablet runtime-menu source | `[Unclear]` | Does the deployed tablet read from Edge, Cloud, or both, and what is the fallback contract? |
| Payment commit versus command creation | `[Unclear]` | Which state changes share a transaction, and which are post-commit/reconciled? |
| Artifact access in command pull | `[Unclear]` | What exact payload/reference and retrieval flow does Edge receive? |
| Robot program invocation and physical result | `[Assumption]` | Which Edge/robot contract exists outside this repository, and what evidence returns to Cloud? |
| Acknowledgement lifecycle | `[Unclear]` | What are the exact DTO fields, enum values, valid transitions, and terminal meanings? |
| ProductionIncident business maximum | `[Open Question]` | Is the intended rule many per item, one per item, or one per production unit? |
| Menu scope/ownership | `[Open Question]` | How do organization/store/kiosk scope and nullable `KioskId` interact? |
| Soft-delete key reuse | `[Open Question]` | Which unique business keys are intentionally reusable after deletion? |
| `DbSet`/table reconciliation | `[Open Question]` | What is the authoritative mapped-table count, including entities without a `DbSet`? |
| Inventory participation in sellability/checkout | `[Unclear]` | Which handlers actually query inventory, and which gates are configuration-only? |
| Package/version/install lifecycle | `[Open Question]` | What are the approved uniqueness, upgrade, rollback, and observed-install semantics? |
| Execution-driven consumption | `[Inferred]` | Is the end-to-end consumption workflow implemented and verified, or only partially represented? |
| Frontend package/UI behavior | `[Assumption]` | Is frontend design in Report 4 scope, and where is its approved evidence? |
| Detailed-design coverage boundary | `[Open Question]` | Which SRS features must have class and sequence designs for university acceptance? |

## 7. Suggested revision priority

1. Resolve or visibly mark the runtime-menu boundary, payment/dispatch transaction boundary, and physical/Edge assumptions.
2. Replace the grouped package view with accurate package/namespace and bounded-context ownership views.
3. Correct ERD optionalities/cardinalities and complete the physical key/FK catalogue.
4. Add feature-level design coverage and failure sequences for the highest-priority SRS workflows.
5. Normalize uncertainty labels and link every unresolved design issue to `open_questions.md`.
6. Finish submission metadata, change history, coverage mapping, and rendering/readability checks.

## Final review disposition

**Not ready for final university submission.** The document is a credible architecture-and-design draft, and its treatment of Cloud/Edge evidence is appropriately cautious. The remaining blockers are incomplete template-level detailed design, ambiguous package ownership, several unsupported diagram semantics, incomplete database catalogue/cardinality precision, and unresolved workflow boundaries. These should be corrected or explicitly accepted and labeled before the report is presented as the final SDD.
