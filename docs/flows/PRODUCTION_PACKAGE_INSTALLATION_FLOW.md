# Production Package Installation Flow

## Status

The code model and API surface are implemented. Migration
`AddProductionPackageComposition` contains the database delta, but each
environment must apply it before using this feature.

## Boundary

`ProductionPackages` hides the technical binding graph from the normal
franchise workflow. It does not replace Catalog, RobotConfiguration,
ProductionConfiguration, deployment, or Edge execution.

```text
Platform authoring
-> global Product template and published Recipe versions
-> published RobotArtifactTemplate
-> published RobotArtifactTechnicalContract
-> ProductionPackage Draft/version
-> immutable package manifest

Organization installation
-> copy Product/Recipe/options into organization scope
-> copy immutable Lua objects into organization storage
-> create organization RobotArtifacts
-> validate declared effects and fixed quantities
-> deterministically create one RobotProgram per route and its RunOrder
-> create ConfigurationRelease Draft and routes
-> preserve installation/materialization provenance
```

## Technical Contract

Lua remains an executable artifact. Backend does not parse Lua to infer its
physical behavior. A versioned `RobotArtifactTechnicalContract` declares:

- effects;
- fixed or parameterized quantity mode;
- ingredient/option codes;
- runtime target and machine model;
- capabilities;
- ordering constraints.

`Parameterized` quantity is rejected while the Edge/Fairino runtime contract
does not support it. `FixedInArtifact` effects must match Recipe/option quantity
and unit during composition.

An option-specific Lua file remains static. Its program membership carries
`RequiredOptionCode`; Edge executes that ordered artifact only when the order
line contains the matching selected option. This supports optional topping
artifacts without pretending that Lua accepts runtime quantity parameters.
Option codes must be unique across one packaged Product, and composition
matches option ingredient requirements using the exact option code in addition
to ingredient, quantity, and unit.

Product options explicitly declare their execution boundary:

- `CommercialOnly` affects customer choice or price and is excluded from robot
  composition.
- `ProductionAffecting` changes physical production and must resolve to an
  ingredient requirement or an option-specific artifact effect.

The backend rejects commercial-only options with physical effects and rejects
production-affecting options that have no deterministic production input. This
classification is part of package product snapshot schema V2.

The reader still accepts immutable V1 package snapshots. For V1 only, option
impact is resolved from both ingredient requirements and option codes declared
by the package's artifact technical contracts. New and replaced package
definitions are always written as V2; V1 snapshots are not rewritten.

Package V1 accepts exactly one required capability code per route because one
route currently materializes one RobotProgram binding. Multi-workcell package
routes require a later binding contract instead of silently choosing the first
capability.

One package version may include each source Product only once. Different
`SourceKey` values cannot alias the same `SourceProductId`; V1 rejects that
definition instead of duplicating snapshot identities. Legacy option-effect
resolution is scoped through the owning Product's routes and program
blueprints, so identical option codes in different Products do not affect each
other.

Each route binds one exact source tuple:

```text
ProductSourceKey
+ ProductVariantSourceKey
+ RecipeSourceKey
+ ProgramBlueprintCode
+ SupportedOptionCodes
```

Recipe codes may repeat across variants. `SupportedOptionCodes` contains only
production-affecting options executable by that route; commercial-only options
remain Catalog behavior and an empty list means the route supports no physical
option adjustment. Validation and installation use this route policy rather
than applying every Product option to every RobotProgram.

Fairino-Studio exports a sibling `.icebot.json` sidecar for each `.lua`. The
sidecar is authoring input and must still be reviewed and published as a
technical contract. It is not executable and is not a certification.
Re-importing the same organization, contract code, and version replaces an
existing Draft definition. Published and Retired versions remain immutable and
require a new contract version.

## API

Platform package authoring:

```text
GET   /api/v1/management/production-packages
GET   /api/v1/management/production-packages/{packageId}
POST  /api/v1/management/production-packages
PUT   /api/v1/management/production-packages/{packageId}
PATCH /api/v1/management/production-packages/{packageId}/retire
POST  /api/v1/management/production-packages/{packageId}/versions
GET   /api/v1/management/production-packages/{packageId}/versions/{versionId}/definition
PUT   /api/v1/management/production-packages/{packageId}/versions/{versionId}/definition
PATCH /api/v1/management/production-packages/{packageId}/versions/{versionId}/publish
PATCH /api/v1/management/production-packages/{packageId}/versions/{versionId}/retire
```

Organization package workflow:

```text
GET  /api/v1/management/organizations/{organizationId}/production-packages/catalog
POST /api/v1/management/organizations/{organizationId}/production-package-installations
GET  /api/v1/management/organizations/{organizationId}/production-package-installations
GET  /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}
GET  /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/workspace
POST /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/retry
POST /api/v1/management/organizations/{organizationId}/production-package-installations/{installationId}/fork
```

The workspace endpoint is the normal single-screen read model. It aggregates
materialized Products, Variants, Options, Recipes, Artifacts, ordered Programs,
the Draft/Published release, menu assignment, execution-endpoint readiness,
latest deployment state, separate commercial/technical blockers, and structured
required/optional/recovery action codes. FE invokes
the existing resource-specific command endpoints for those actions; workspace
does not bypass their authorization, validation, or audit boundaries.
Action context supplies the nested parent IDs required by those APIs. Deployment
actions also identify the compatible execution endpoint/profile and the complete
route/program selections required by a Low-cost deployment.
Unavailable options are optional offerings unless they leave an active required
group below `MinSelections`; that condition blocks commercial readiness and its
enable action becomes required. Failed installations keep
their selected-product snapshot and can be retried through the installation
retry endpoint without reconstructing the original request in FE.

Technical-contract authoring:

```text
GET/POST /api/v1/management/robot-artifact-technical-contracts
GET/PUT/DELETE /api/v1/management/robot-artifact-technical-contracts/{id}
POST     /api/v1/management/robot-artifact-technical-contracts/{id}/validation-preview
PATCH    /api/v1/management/robot-artifact-technical-contracts/{id}/publish|retire
GET/POST /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts
GET/PUT/DELETE /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/{id}
POST     /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/import-sidecars
POST     /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/{id}/validation-preview
PATCH    /api/v1/management/organizations/{organizationId}/robot-artifact-technical-contracts/{id}/publish|retire
PUT      /api/v1/management/robot-artifact-templates/{templateId}/technical-contract
PUT      /api/v1/management/organizations/{organizationId}/robot-artifacts/{artifactId}/technical-contract
```

Artifact and template publication requires the assigned contract to remain
Published and checksum/target compatible. Publication also verifies the Lua
object size and SHA-256 before changing lifecycle state.

Installation uses the `Idempotency-Key` header. A retry with the same package,
scope, and manifest returns the existing installation. Reusing the key for a
different payload returns conflict.

Definition replacement, package publication, installation preview, and actual
installation run the same deterministic validation. An invalid Recipe source,
effect, fixed quantity, option identity, ordering graph, target, or capability
cannot be published as an installable package version.

Package upgrade/diff and recompose-after-authoring are not part of this V1
surface. Upgrade must create new Draft resources and a new release; it must not
mutate an installed or active release.

## Publication And Deployment

Release publication builds one immutable production definition per route. Its
checksum covers Recipe quantities, supported options, option ingredient
requirements, program order, artifact checksums, technical-contract checksums,
capabilities, and package provenance.

Deployment now requires a validation preview:

```text
POST /api/v1/management/kiosks/{kioskId}/configuration-deployments/validation-preview
```

The deploy request echoes the returned validation checksum and sets
`acknowledgeRemainingRisk`. This is organization self-acknowledgement, not
third-party certification. Objective failures such as missing effects, wrong
machine target, checksum mismatch, or invalid order cannot be bypassed.

## Frontend Boundary

Normal package installation does not ask FE for artifact IDs, RunOrder,
parameters JSON, route priority, capability codes, program IDs, manifests,
checksums, storage keys, or schema versions. Existing technical APIs remain for
advanced self-authoring workflows.
