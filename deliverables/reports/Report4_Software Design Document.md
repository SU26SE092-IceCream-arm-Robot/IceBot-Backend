# Software Design Document - IceBot Robotic Ice Cream Kiosk System

## **I. Record of Changes**

| Version | Date | Description | Author |
| :---: | :---: | :--- | :--- |
| V1.0 | 2026-07-04 | Initial Software Design Document (SDD) baseline, covering system context, database physical schemas, and detailed design specifications for all functional domains. | Antigravity AI |

## **II. Software Design Document**

## **1. System Design**

### **1.1. System Architecture**

This section describes the high-level architecture of the IceBot Robotic Ice Cream Kiosk System.

##### **1.1.1. System Context Diagram**

The System Context Diagram shows the boundaries of the IceBot system and its interactions with external entities:
- **Customers**: Interact with the Kiosk Tablet App to customize ice cream, review carts, make payments, and view preparation steps.
- **Organization Admins & Store Managers**: Access the Management Web Portal to review sales reports, configure catalog menus, assign roles, monitor live inventories, and author robot paths.
- **System Admins**: Configure global device types, update robot artifact templates, and manage core multi-tenant organizations.
- **PayOS Payment Gateway**: External API that processes QR payments and sends webhooks to the cloud backend.
- **Fairino Robot Controller**: Physical controller on the kiosk executing Lua scripts sent from the edge coordinator.

*(Diagram/Image skipped)*

##### **1.1.2. Container Diagram**

The system consists of the following containers:
- **Cloud Backend**: C# / .NET Core 8 WebAPI. Exposes REST endpoints, GraphQL dashboard queries, and SignalR telemetry hubs.
- **Management Web App**: Next.js single page application serving admin dashboards.
- **Kiosk Tablet App**: React application running on the customer-facing kiosk touch screen.
- **Full Edge Runtime**: C# local host service executing on the Kiosk PC coordinating local databases, SignalR channels, and socket commands to the robot arm.
- **Database & Cache**: PostgreSQL database + Redis caching instances.
- **Object Storage**: S3/MinIO object buckets to store raw Blockly XML coordinates and compiled Lua execution artifacts.

*(Diagram/Image skipped)*

##### **1.1.3. Cloud Backend Layered Architecture**

The Cloud Backend uses Clean Architecture principles, dividing code into four distinct layers:
1. **Domain Layer**: Contains entities, value objects, exceptions, and core interfaces. No external dependencies.
2. **Application Layer**: Implements business workflows using CQRS (Command Query Responsibility Segregation) pattern with MediatR. Contains DTOs, command/query handlers, validation rules, and abstract interfaces.
3. **Infrastructure Layer**: Implements external gateways, email senders, Firebase authentication, PayOS integrations, EF Core DBContext, and file store repositories.
4. **WebAPI Presentation Layer**: Exposes REST controllers, GraphQL query/mutation resolvers, SignalR real-time hubs, and authentication/authorization middleware.

##### **1.1.4. Bounded Context Architecture**

The system is separated into bounded contexts to maintain clear data boundaries and avoid excessive entity graphing:
- **Identity Context**: Manages accounts, claims, RBAC mappings, and session refresh tokens.
- **Tenants Context**: Scopes organizations, stores, and physical kiosks.
- **Catalog Context**: Configures base products, variants, toppings, sauces, and ingredient recipes.
- **Sales Catalog Context**: projects customized kiosk-specific menus and price mappings.
- **Orders Context**: Tracks sales transactions, order items, and status histories.
- **Payments Context**: Manages PayOS integration sessions and transaction callback logs.
- **Robot Configuration Context**: Manages Blockly visual scripts and compiled Lua program files.
- **Production Configuration Context**: Governs configuration releases, routing definitions, and deployment releases.
- **Production Execution Context**: Tracks dispatches, command sequences, and attempt tracking.
- **Devices Context**: Telemetries, heartbeats, readiness projections, and hardware capabilities.
- **Inventory Context**: Manages estimated weights of base mixes, toppings, and stock movements.
- **Operations Context**: Alerts, maintenance logs, and support tickets.
- **Sync Context**: Edge synchronizers, inbox queues, and dead-letter stores.

##### **1.1.5. Cloud, Edge Runtime and Machine Boundary**

The system runs across three boundaries:
1. **Cloud Boundary**: Central coordinator for billing, menu setup, releases, and metrics.
2. **Edge Boundary**: Local Kiosk PC. Serves customers even when cloud internet is disconnected by relying on a local SQLite cache. It coordinates the robotic arms and dispenser valves.
3. **Machine Boundary**: Fairino Robot Arm controller executing direct joint movements (MoveJ, MoveL) over Ethernet using a socket interface.

##### **1.1.6. Full Edge and Low-cost Controller Deployment Profiles**

1. **Full Edge Profile**: Deployed on standard PC hardware. Uses SQLite for local order logging, a complete SignalR connection coordinator, local Lua file registries, and full sensor polling.
2. **Low-cost Controller Profile**: Deployed on micro-controllers or lightweight gateway devices. Coordinates basic telemetry polling and processes raw socket commands directly using a persistent deduplication buffer.

##### **1.1.7. REST, GraphQL, SignalR and MQTT Communication Architecture**

- **REST (HTTP)**: Transactional operations like payment checkout generation, file uploads, and session creations.
- **GraphQL (HTTPS)**: Admin reporting grids. Allows fetching nested entity records (Orgs -> Stores -> Kiosks -> Stock levels) in a single request.
- **SignalR (WebSockets)**: Streams real-time robot movements to kiosk touchscreens and feeds hardware telemetry to the admin dashboard.
- **MQTT (TCP)**: Lightweight messaging queue for high-frequency sensor updates and command dispatches.

##### **1.1.8. Authentication, RBAC and Tenant Isolation Architecture**

- **Authentication**: JWT access tokens validated against public keys with Firebase/Google OAuth backend.
- **RBAC**: Custom claims map roles (SystemAdmin, OrgAdmin, StoreManager, MaintenanceStaff, KioskAccount).
- **Tenant Isolation**: Handled via global query filters on `OrganizationId` within EF Core. Writes are validated using resource-based authorization handlers.

##### **1.1.9. PostgreSQL and Object Storage Architecture**

- **PostgreSQL**: Stores relational tables, using standard indexes on foreign keys and compound indexes on `KioskId` and `OrganizationId`.
- **Object Storage**: S3/MinIO bucket `robot-artifacts` stores raw Blockly code blocks and compiled Lua scripts indexed by artifact ID.

##### **1.1.10. Logging, Tracing and Metrics Architecture**

- **Logging**: Serilog outputs structured JSON logs to central storage.
- **Tracing**: OpenTelemetry tracks checkout latency and webhook process flows.
- **Metrics**: Prometheus exposes telemetry connection counts, active order queues, and error rates.

---

### **1.2. Package Diagram**

This section describes the packages and namespaces of the system.

##### **1.2.1. IceBot Backend Package Diagram**

*(Diagram/Image skipped)*

###### **1.2.1.1. Domain Layer Package Description**
Defines core enterprise primitives, domain entities (e.g. `Account`, `Order`, `Kiosk`), value objects, and custom domain exceptions.

###### **1.2.1.2. Application Layer Package Description**
Houses CQRS command and query handlers, MediatR message definitions, fluent validators, mapping profiles, and application abstractions.

###### **1.2.1.3. Infrastructure Layer Package Description**
Implements data persistence (EF Core, DbContext), email senders, storage adapters (S3), Firebase client, and PayOS gateway.

###### **1.2.1.4. WebAPI Layer Package Description**
Defines REST controllers, GraphQL query schemas, SignalR hub configurations, and authentication middlewares.

###### **1.2.1.5. Identity Package Description**
Coordinates login, token refreshing, Google login integration, password resets, and RBAC claim management.

###### **1.2.1.6. Tenants Package Description**
Manages organization profiles, store parameters, location maps, and kiosk configuration associations.

###### **1.2.1.7. Catalog and Sales Catalog Package Description**
Handles base product parameters, variant prices, recipe mapping, toppings, and store-specific menu projections.

###### **1.2.1.8. Orders and Payments Package Description**
Manages cart checkout validation, order state transitions, PayOS integration, and refund transactions.

###### **1.2.1.9. Robot Configuration Package Description**
Governs robot templates, visual coordinates, compiled Lua scripts, and program registries.

###### **1.2.1.10. Production Configuration Package Description**
Authorizes configuration releases, binding execution routes to robot programs.

###### **1.2.1.11. Production Execution Package Description**
Orchestrates Paid Order Dispatch, command pull channels, and executions logs.

###### **1.2.1.12. Devices and Inventory Package Description**
Tracks device metadata, capability lists, heartbeats, low-level alerts, and dispenser estimates.

###### **1.2.1.13. Operations and Sync Package Description**
Manages maintenance logs, sync inbox states, sync outbox checkpoints, and dead-letter recovery.

##### **1.2.2. Management Web App Package Diagram**

*(Diagram/Image skipped)*

###### **1.2.2.1. App Routing Package Description**
Implements routing configurations mapping admin functionalities.

###### **1.2.2.2. Authentication and Authorization Package Description**
Manages user tokens, cookies, and RBAC route protectors.

###### **1.2.2.3. Management Feature Package Description**
Contains dashboard panels for tenants, catalogs, device monitoring, and telemetry charts.

###### **1.2.2.4. REST and GraphQL Client Package Description**
Facilitates queries to the cloud backend.

###### **1.2.2.5. SignalR Client Package Description**
Maintains live channels to display robot arm step progress and sensor alerts.

###### **1.2.2.6. Shared UI Component Package Description**
Reusable layout blocks like metrics, charts, modals, and grids.

##### **1.2.3. Kiosk/Tablet App Package Diagram**

*(Diagram/Image skipped)*

###### **1.2.3.1. Runtime Menu Package Description**
Displays visual base flavors, toppings, and prices dynamically from projected menus.

###### **1.2.3.2. Cart and Checkout Package Description**
Manages cart customizations, allergen warnings, and checkout requests.

###### **1.2.3.3. Payment Package Description**
Renders PayOS transaction QR code and manages the checkout countdown.

###### **1.2.3.4. Order Tracking Package Description**
Listens to SignalR events to show physical cup preparation status.

###### **1.2.3.5. Local Cache and Recovery Package Description**
Caches projected menus locally in SQLite or LocalStorage to ensure offline usability.

##### **1.2.4. Fairino Studio Package Diagram**

*(Diagram/Image skipped)*

###### **1.2.4.1. Blockly Editor Package Description**
Visual editing suite containing movement blocks (MoveJ, MoveL) and dispenser trigger blocks.

###### **1.2.4.2. Robot Program Validation Package Description**
Validates coordinate boundaries against model specifications.

###### **1.2.4.3. Lua Generator Package Description**
Translates Blockly coordinate trees to compiled Lua scripts.

###### **1.2.4.4. Artifact Export and Upload Package Description**
Computes checksums and registers artifacts to S3/Cloud registry.

##### **1.2.5. Full Edge Runtime Package Diagram**

*(Diagram/Image skipped)*

###### **1.2.5.1. Cloud Connector Package Description**
SignalR client maintaining a bidirectional bridge to the cloud.

###### **1.2.5.2. Local Command Inbox Package Description**
Local queue storing dispatched preparation requests.

###### **1.2.5.3. Artifact Cache and Activation Package Description**
Downloads and compiles Lua scripts required for active catalog configurations.

###### **1.2.5.4. Production Scheduler Package Description**
Orders and tracks localized queue schedules.

###### **1.2.5.5. Machine Adapter Package Description**
Communicates with the Fairino controller using socket controls.

###### **1.2.5.6. Local Event Outbox Package Description**
Persists execution records to sync back to the cloud.

##### **1.2.6. Low-cost Controller Package Diagram**

*(Diagram/Image skipped)*

###### **1.2.6.1. Command Receiver Package Description**
Lightweight network client receiving direct socket messages.

###### **1.2.6.2. Active Artifact Set Package Description**
Flash-memory resident set of execution profiles.

###### **1.2.6.3. Lua Execution Package Description**
Interprets basic coordinates commands.

###### **1.2.6.4. Persistent Deduplication Buffer Package Description**
Prevents dual-processing of identical command IDs in micro-environments.

---

## **2. Database Design**

### **2.1. Logical ERD**

Logical Entity Relationship Diagrams mapping system entities and operational models:

- **Identity & Access Logical ERD**: `Accounts` (1) to `RefreshTokens` (N), `Accounts` (1) to `AccountRoles` (N).
- **Organization, Store and Kiosk Logical ERD**: `Organizations` (1) to `Stores` (N), `Stores` (1) to `Kiosks` (N), `Kiosks` (1) to `KioskExecutionEndpoints` (1).
- **Catalog, Recipe and Menu Logical ERD**: `Products` (1) to `ProductVariants` (N), `ProductVariants` (1) to `Recipes` (1), `Recipes` (1) to `RecipeItems` (N).
- **Order, Payment and Refund Logical ERD**: `Orders` (1) to `OrderItems` (N), `Orders` (1) to `PaymentTransactions` (N), `PaymentTransactions` (1) to `Refunds` (1).
- **Robot Artifact and Robot Program Logical ERD**: `RobotArtifactTemplates` (1) to `RobotArtifacts` (N), `RobotPrograms` (1) to `RobotProgramArtifacts` (N).
- **Configuration Release and Deployment Logical ERD**: `ConfigurationReleases` (1) to `ExecutionRoutes` (N), `ConfigurationReleases` (1) to `KioskConfigurationDeployments` (N).
- **Production Execution Logical ERD**: `Orders` (1) to `ProductionExecutionRecords` (N), `EdgeCommands` (1) to `EdgeCommandDeliveryAttempts` (N).
- **Device, Endpoint and Telemetry Logical ERD**: `Devices` (1) to `DeviceEvents` (N), `KioskExecutionEndpoints` (1) to `EdgeStateSummaries` (1).
- **Inventory Logical ERD**: `Kiosks` (1) to `IngredientDispenserStates` (N), `IngredientDispenserStates` (1) to `StockMovements` (N).
- **Alert and Maintenance Logical ERD**: `Kiosks` (1) to `Alerts` (N), `Kiosks` (1) to `MaintenanceTickets` (N).
- **Edge Command, Sync and Dead-letter Logical ERD**: `SyncEventInbox` (1) to `SyncDeadLetters` (1), `SyncDeadLetters` (1) to `SyncDeadLetterRetryAttempts` (N).

*(Diagram/Image skipped)*

### **2.2. Physical ERD**

The physical database schema contains the following tables:

#### **2.2.1. Identity and Tenant Physical ERD**
- **Accounts**: PK `Id` (uuid), contains email, password hash, status, tenant scopes.
- **RefreshTokens**: PK `Id` (uuid), FK `AccountId` -> Accounts.
- **Roles**: PK `Id` (bigint), defines system roles.
- **AccountRoles**: PK `Id` (uuid), FK `AccountId` -> Accounts, FK `RoleId` -> Roles, FK `OrganizationId` -> Organizations, FK `StoreId` -> Stores.
- **Organizations**: PK `Id` (uuid), name, status.
- **Stores**: PK `Id` (uuid), FK `OrganizationId` -> Organizations.
- **Kiosks**: PK `Id` (uuid), FK `OrganizationId` -> Organizations, FK `StoreId` -> Stores, status, current release version.

#### **2.2.2. Catalog and Sales Catalog Physical ERD**
- **ProductCategories**: PK `Id` (uuid), name, parent reference.
- **Products**: PK `Id` (uuid), FK `CategoryId` -> ProductCategories, FK `OrganizationId` -> Organizations.
- **ProductVariants**: PK `Id` (uuid), FK `ProductId` -> Products, sku, base price.
- **Recipes**: PK `Id` (uuid), FK `ProductVariantId` -> ProductVariants.
- **RecipeItems**: PK `Id` (uuid), FK `RecipeId` -> Recipes, FK `IngredientId` -> Ingredients, portion weight.
- **Ingredients**: PK `Id` (uuid), name, type (GelatoBase, Syrup, Topping).
- **Menus**: PK `Id` (uuid), FK `KioskId` -> Kiosks.
- **MenuItems**: PK `Id` (uuid), FK `MenuId` -> Menus, FK `ProductVariantId` -> ProductVariants, override price.

#### **2.2.3. Order and Payment Physical ERD**
- **Orders**: PK `Id` (uuid), FK `KioskId` -> Kiosks, status, total cost, customer identifier.
- **OrderItems**: PK `Id` (uuid), FK `OrderId` -> Orders, FK `ProductVariantId` -> ProductVariants, quantity.
- **PaymentTransactions**: PK `Id` (uuid), FK `OrderId` -> Orders, transaction reference, amount, status.
- **PaymentCallbacks**: PK `Id` (uuid), FK `PaymentTransactionId` -> PaymentTransactions, raw PayOS payload.
- **Refunds**: PK `Id` (uuid), FK `PaymentTransactionId` -> PaymentTransactions, amount, reason.

#### **2.2.4. Robot and Production Configuration Physical ERD**
- **RobotArtifactTemplates**: PK `Id` (uuid), template details, script skeleton.
- **RobotArtifacts**: PK `Id` (uuid), FK `OrganizationId` -> Organizations, file key, checksum, status.
- **RobotPrograms**: PK `Id` (uuid), FK `KioskId` -> Kiosks, FK `DeviceId` -> Devices.
- **RobotProgramArtifacts**: PK `Id` (uuid), FK `RobotProgramId` -> RobotPrograms, FK `RobotArtifactId` -> RobotArtifacts.
- **ConfigurationReleases**: PK `Id` (uuid), FK `OrganizationId` -> Organizations, release code, active status.
- **ExecutionRoutes**: PK `Id` (uuid), FK `ConfigurationReleaseId` -> ConfigurationReleases, FK `ProductVariantId` -> ProductVariants, FK `RecipeId` -> Recipes.
- **ExecutionRouteRobotBindings**: PK `Id` (uuid), FK `ExecutionRouteId` -> ExecutionRoutes, FK `RobotProgramId` -> RobotPrograms.

#### **2.2.5. Execution, Device and Telemetry Physical ERD**
- **ProductionExecutionRecords**: PK `Id` (uuid), FK `KioskExecutionEndpointId` -> KioskExecutionEndpoints, status, progress step.
- **Devices**: PK `Id` (uuid), FK `KioskId` -> Kiosks, FK `DeviceModelId` -> DeviceModels, type.
- **DeviceModels**: PK `Id` (uuid), FK `DeviceTypeId` -> DeviceTypes, manufacturer, properties.
- **DeviceTypes**: PK `Id` (uuid), type name.
- **DeviceEvents**: PK `Id` (uuid), FK `DeviceId` -> Devices, log payload.
- **EdgeCommands**: PK `Id` (uuid), FK `TargetExecutionEndpointId` -> KioskExecutionEndpoints, command payload, status.
- **EdgeCommandDeliveryAttempts**: PK `Id` (uuid), FK `EdgeCommandId` -> EdgeCommands.

#### **2.2.6. Inventory, Operations and Sync Physical ERD**
- **IngredientDispenserStates**: PK `Id` (uuid), FK `KioskId` -> Kiosks, FK `DeviceId` -> Devices, estimated weight, dispenser capacity.
- **StockMovements**: PK `Id` (uuid), FK `IngredientDispenserStateId` -> IngredientDispenserStates, change amount, reason.
- **MaintenanceTickets**: PK `Id` (uuid), FK `KioskId` -> Kiosks, assigned technician, ticket status.
- **Alerts**: PK `Id` (uuid), FK `KioskId` -> Kiosks, severity, title, active state.
- **SyncEventInbox**: PK `Id` (uuid), FK `KioskId` -> Kiosks, payload, sync status.
- **SyncDeadLetters**: PK `Id` (uuid), FK `KioskId` -> Kiosks, error description, status.

##### **2.2.7. Composite Tenant Constraints and Indexes**
To ensure performance and partition tenant access, all primary data tables contain index bindings:
- `CREATE INDEX IX_Orders_OrganizationId ON public."Orders" ("OrganizationId");`
- `CREATE UNIQUE INDEX UQ_Kiosk_Tenant ON public."Kiosks" ("Id", "OrganizationId");`
- Compound index on `public."StockMovements" ("KioskId", "IngredientId", "CreatedAt" DESC);`

##### **2.2.8. Soft-delete Filters and Retention Rules**
Tables containing operational states employ soft deletion patterns:
- Columns: `DeletedAt` (timestamp with time zone) and `DeletedByAccountId` (uuid).
- EF Core Query Filter: `builder.HasQueryFilter(e => e.DeletedAt == null);`
- Retention rules automatically clean records older than 90 days for `KioskHeartbeats` and `DeviceEvents`.

### **2.3. Object Storage Design**

Object storage handles Blockly workspace XML definitions and compiled Lua programs.

##### **2.3.1. Robot Artifact Bucket and Object-key Layout**
- Bucket Name: `icebot-robot-artifacts`
- Key Structure: `organizations/{organization_id}/programs/{program_id}/{artifact_id}.lua`
- Backup Key Structure: `templates/{template_id}/{artifact_id}.xml`

##### **2.3.2. Artifact Metadata and Checksum Mapping**
Metadata properties stored upon artifact registry:
- Content-Type: `text/plain`
- Metadata: `x-amz-meta-checksum-sha256`: SHA-256 string for validation.
- Metadata: `x-amz-meta-compiled-by`: Account ID creator.

##### **2.3.3. Presigned Download URL Flow
When edge runtimes pull compiled Lua configurations:
1. Edge requests release configurations via API.
2. Cloud Backend validates API claims and accesses S3 storage client.
3. Cloud Backend generates a presigned download URL with a 15-minute expiration time: `s3Client.GetPresignedUrl(...)`.
4. Edge downloads script over HTTPS using presigned URL.

##### **2.3.4. Orphan Object Cleanup
A monthly background task identifies S3 keys that do not correspond to any active records in `RobotArtifacts` table, executing deletion commands on S3 to prevent storage leaks.

---

## **3. Detailed Design**

This section describes the detailed class diagrams, sequence diagrams, and class specification tables for all sub-functions.

### **3.1 Identity and Access Design**

#### ***3.1.1 Local Account Login***

##### **3.1.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.1.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.1.1.3 Class Specification**

###### **3.1.1.3.1 AuthController Description Table**

Figure 3.1.1.3.1 AuthController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Login(LoginDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.1.1.3.2 AuthService Description Table**

Figure 3.1.1.3.2 AuthService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Authenticate(LoginDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.1.1.3.3 DTOs Description Table**

Figure 3.1.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | LoginDto / JwtDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.1.2 Google/Firebase Login***

##### **3.1.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.1.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.1.2.3 Class Specification**

###### **3.1.2.3.1 AuthController Description Table**

Figure 3.1.2.3.1 AuthController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GoogleLogin(GoogleLoginDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.1.2.3.2 AuthService Description Table**

Figure 3.1.2.3.2 AuthService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | VerifyGoogleToken(GoogleLoginDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.1.2.3.3 DTOs Description Table**

Figure 3.1.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | GoogleLoginDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.1.3 Refresh and Revoke Token***

##### **3.1.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.1.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.1.3.3 Class Specification**

###### **3.1.3.3.1 AuthController Description Table**

Figure 3.1.3.3.1 AuthController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | RefreshToken(RefreshTokenDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.1.3.3.2 AuthService Description Table**

Figure 3.1.3.3.2 AuthService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Refresh(RefreshTokenDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.1.3.3.3 DTOs Description Table**

Figure 3.1.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | RefreshTokenDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.1.4 Forgot and Reset Password***

##### **3.1.4.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.1.4.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.1.4.3 Class Specification**

###### **3.1.4.3.1 AuthController Description Table**

Figure 3.1.4.3.1 AuthController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ResetPassword(ResetPasswordDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.1.4.3.2 AuthService Description Table**

Figure 3.1.4.3.2 AuthService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Reset(ResetPasswordDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.1.4.3.3 DTOs Description Table**

Figure 3.1.4.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ResetPasswordDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.1.5 Account Invitation and Activation***

##### **3.1.5.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.1.5.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.1.5.3 Class Specification**

###### **3.1.5.3.1 InvitationController Description Table**

Figure 3.1.5.3.1 InvitationController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | AcceptInvitation(AcceptDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.1.5.3.2 InvitationService Description Table**

Figure 3.1.5.3.2 InvitationService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Accept(AcceptDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.1.5.3.3 DTOs Description Table**

Figure 3.1.5.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | AcceptDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.1.6 Current Account Profile and Password***

##### **3.1.6.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.1.6.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.1.6.3 Class Specification**

###### **3.1.6.3.1 ProfileController Description Table**

Figure 3.1.6.3.1 ProfileController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | UpdateProfile(ProfileDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.1.6.3.2 ProfileService Description Table**

Figure 3.1.6.3.2 ProfileService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Update(ProfileDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.1.6.3.3 DTOs Description Table**

Figure 3.1.6.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ProfileDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.1.7 Internal Account and Role Management***

##### **3.1.7.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.1.7.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.1.7.3 Class Specification**

###### **3.1.7.3.1 AccountController Description Table**

Figure 3.1.7.3.1 AccountController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | AssignRole(AssignRoleDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.1.7.3.2 AccountService Description Table**

Figure 3.1.7.3.2 AccountService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Assign(AssignRoleDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.1.7.3.3 DTOs Description Table**

Figure 3.1.7.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | AssignRoleDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.1.8 Effective Access and Permission Matrix***

##### **3.1.8.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.1.8.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.1.8.3 Class Specification**

###### **3.1.8.3.1 PermissionController Description Table**

Figure 3.1.8.3.1 PermissionController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetPermissions() | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.1.8.3.2 PermissionService Description Table**

Figure 3.1.8.3.2 PermissionService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | EvaluatePermissions() | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.1.8.3.3 DTOs Description Table**

Figure 3.1.8.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | PermissionMatrixDto | Data transfer payload wrapper defining validations and required parameters. |

---

### **3.2 Tenant Management Design**

#### ***3.2.1 Organization Management***

##### **3.2.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.2.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.2.1.3 Class Specification**

###### **3.2.1.3.1 OrganizationController Description Table**

Figure 3.2.1.3.1 OrganizationController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateOrganization(CreateOrgDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.2.1.3.2 OrganizationService Description Table**

Figure 3.2.1.3.2 OrganizationService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(CreateOrgDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.2.1.3.3 DTOs Description Table**

Figure 3.2.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | CreateOrgDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.2.2 Store Management***

##### **3.2.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.2.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.2.2.3 Class Specification**

###### **3.2.2.3.1 StoreController Description Table**

Figure 3.2.2.3.1 StoreController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateStore(CreateStoreDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.2.2.3.2 StoreService Description Table**

Figure 3.2.2.3.2 StoreService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(CreateStoreDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.2.2.3.3 DTOs Description Table**

Figure 3.2.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | CreateStoreDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.2.3 Kiosk Management***

##### **3.2.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.2.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.2.3.3 Class Specification**

###### **3.2.3.3.1 KioskController Description Table**

Figure 3.2.3.3.1 KioskController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateKiosk(CreateKioskDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.2.3.3.2 KioskService Description Table**

Figure 3.2.3.3.2 KioskService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(CreateKioskDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.2.3.3.3 DTOs Description Table**

Figure 3.2.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | CreateKioskDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.2.4 Tenant Tree and Role-scope Options***

##### **3.2.4.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.2.4.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.2.4.3 Class Specification**

###### **3.2.4.3.1 TenantController Description Table**

Figure 3.2.4.3.1 TenantController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetTenantTree() | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.2.4.3.2 TenantService Description Table**

Figure 3.2.4.3.2 TenantService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetTree() | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.2.4.3.3 DTOs Description Table**

Figure 3.2.4.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | TenantTreeDto | Data transfer payload wrapper defining validations and required parameters. |

---

### **3.3 Catalog and Menu Design**

#### ***3.3.1 Product Template Management***

##### **3.3.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.3.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.3.1.3 Class Specification**

###### **3.3.1.3.1 TemplateController Description Table**

Figure 3.3.1.3.1 TemplateController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateTemplate(TemplateDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.3.1.3.2 TemplateService Description Table**

Figure 3.3.1.3.2 TemplateService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(TemplateDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.3.1.3.3 DTOs Description Table**

Figure 3.3.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | TemplateDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.3.2 Product and Variant Management***

##### **3.3.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.3.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.3.2.3 Class Specification**

###### **3.3.2.3.1 ProductController Description Table**

Figure 3.3.2.3.1 ProductController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateProduct(ProductDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.3.2.3.2 ProductService Description Table**

Figure 3.3.2.3.2 ProductService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(ProductDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.3.2.3.3 DTOs Description Table**

Figure 3.3.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ProductDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.3.3 Recipe and Ingredient Management***

##### **3.3.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.3.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.3.3.3 Class Specification**

###### **3.3.3.3.1 RecipeController Description Table**

Figure 3.3.3.3.1 RecipeController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateRecipe(RecipeDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.3.3.3.2 RecipeService Description Table**

Figure 3.3.3.3.2 RecipeService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(RecipeDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.3.3.3.3 DTOs Description Table**

Figure 3.3.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | RecipeDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.3.4 Menu and Menu Item Management***

##### **3.3.4.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.3.4.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.3.4.3 Class Specification**

###### **3.3.4.3.1 MenuController Description Table**

Figure 3.3.4.3.1 MenuController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateMenu(MenuDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.3.4.3.2 MenuService Description Table**

Figure 3.3.4.3.2 MenuService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(MenuDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.3.4.3.3 DTOs Description Table**

Figure 3.3.4.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | MenuDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.3.5 Kiosk Runtime Menu Projection***

##### **3.3.5.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.3.5.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.3.5.3 Class Specification**

###### **3.3.5.3.1 MenuController Description Table**

Figure 3.3.5.3.1 MenuController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetKioskMenu(kioskId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.3.5.3.2 MenuService Description Table**

Figure 3.3.5.3.2 MenuService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProjectKioskMenu(kioskId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.3.5.3.3 DTOs Description Table**

Figure 3.3.5.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | KioskMenuDto | Data transfer payload wrapper defining validations and required parameters. |

---

### **3.4 Order and Payment Design**

#### ***3.4.1 Place and Cancel Order***

##### **3.4.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.4.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.4.1.3 Class Specification**

###### **3.4.1.3.1 CheckoutController Description Table**

Figure 3.4.1.3.1 CheckoutController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Checkout(CheckoutDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.4.1.3.2 CheckoutService Description Table**

Figure 3.4.1.3.2 CheckoutService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProcessCheckout(CheckoutDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.4.1.3.3 DTOs Description Table**

Figure 3.4.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | CheckoutDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.4.2 Create Payment Session***

##### **3.4.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.4.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.4.2.3 Class Specification**

###### **3.4.2.3.1 PaymentController Description Table**

Figure 3.4.2.3.1 PaymentController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateSession(PaymentSessionDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.4.2.3.2 PaymentService Description Table**

Figure 3.4.2.3.2 PaymentService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | InitializePayOS(PaymentSessionDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.4.2.3.3 DTOs Description Table**

Figure 3.4.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | PaymentSessionDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.4.3 Process PayOS Webhook***

##### **3.4.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.4.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.4.3.3 Class Specification**

###### **3.4.3.3.1 PaymentController Description Table**

Figure 3.4.3.3.1 PaymentController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | PayOSWebhook(WebhookDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.4.3.3.2 PaymentService Description Table**

Figure 3.4.3.3.2 PaymentService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | VerifyAndProcess(WebhookDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.4.3.3.3 DTOs Description Table**

Figure 3.4.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | WebhookDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.4.4 Query Order and Payment Status***

##### **3.4.4.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.4.4.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.4.4.3 Class Specification**

###### **3.4.4.3.1 PaymentController Description Table**

Figure 3.4.4.3.1 PaymentController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetStatus(orderId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.4.4.3.2 PaymentService Description Table**

Figure 3.4.4.3.2 PaymentService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | QueryStatus(orderId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.4.4.3.3 DTOs Description Table**

Figure 3.4.4.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | PaymentStatusDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.4.5 Back-office Order and Status History***

##### **3.4.5.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.4.5.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.4.5.3 Class Specification**

###### **3.4.5.3.1 OrderHistoryController Description Table**

Figure 3.4.5.3.1 OrderHistoryController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetHistory(orderId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.4.5.3.2 OrderHistoryService Description Table**

Figure 3.4.5.3.2 OrderHistoryService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetHistory(orderId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.4.5.3.3 DTOs Description Table**

Figure 3.4.5.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | OrderHistoryDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.4.6 Refund Management***

##### **3.4.6.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.4.6.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.4.6.3 Class Specification**

###### **3.4.6.3.1 RefundController Description Table**

Figure 3.4.6.3.1 RefundController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | RefundOrder(RefundDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.4.6.3.2 RefundService Description Table**

Figure 3.4.6.3.2 RefundService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProcessRefund(RefundDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.4.6.3.3 DTOs Description Table**

Figure 3.4.6.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | RefundDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.4.7 Payment Method Management***

##### **3.4.7.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.4.7.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.4.7.3 Class Specification**

###### **3.4.7.3.1 PaymentMethodController Description Table**

Figure 3.4.7.3.1 PaymentMethodController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetActiveMethods() | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.4.7.3.2 PaymentMethodService Description Table**

Figure 3.4.7.3.2 PaymentMethodService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetMethods() | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.4.7.3.3 DTOs Description Table**

Figure 3.4.7.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | PaymentMethodDto | Data transfer payload wrapper defining validations and required parameters. |

---

### **3.5 Robot Artifact and Program Design**

#### ***3.5.1 Global Robot Artifact Template Management***

##### **3.5.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.5.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.5.1.3 Class Specification**

###### **3.5.1.3.1 RobotTemplateController Description Table**

Figure 3.5.1.3.1 RobotTemplateController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateTemplate(RobotTemplateDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.5.1.3.2 RobotTemplateService Description Table**

Figure 3.5.1.3.2 RobotTemplateService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(RobotTemplateDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.5.1.3.3 DTOs Description Table**

Figure 3.5.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | RobotTemplateDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.5.2 Organization Robot Artifact Upload and Review***

##### **3.5.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.5.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.5.2.3 Class Specification**

###### **3.5.2.3.1 RobotArtifactController Description Table**

Figure 3.5.2.3.1 RobotArtifactController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | UploadArtifact(UploadArtifactDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.5.2.3.2 RobotArtifactService Description Table**

Figure 3.5.2.3.2 RobotArtifactService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProcessUpload(UploadArtifactDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.5.2.3.3 DTOs Description Table**

Figure 3.5.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | UploadArtifactDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.5.3 Artifact Publish, Retire and Discard***

##### **3.5.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.5.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.5.3.3 Class Specification**

###### **3.5.3.3.1 RobotArtifactController Description Table**

Figure 3.5.3.3.1 RobotArtifactController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | PublishArtifact(artifactId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.5.3.3.2 RobotArtifactService Description Table**

Figure 3.5.3.3.2 RobotArtifactService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Publish(artifactId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.5.3.3.3 DTOs Description Table**

Figure 3.5.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ArtifactStatusDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.5.4 Robot Program and Ordered Artifact Manifest***

##### **3.5.4.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.5.4.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.5.4.3 Class Specification**

###### **3.5.4.3.1 RobotProgramController Description Table**

Figure 3.5.4.3.1 RobotProgramController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateProgram(ProgramDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.5.4.3.2 RobotProgramService Description Table**

Figure 3.5.4.3.2 RobotProgramService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(ProgramDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.5.4.3.3 DTOs Description Table**

Figure 3.5.4.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ProgramDto | Data transfer payload wrapper defining validations and required parameters. |

---

### **3.6 Production Configuration Design**

#### ***3.6.1 Configuration Release Authoring***

##### **3.6.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.6.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.6.1.3 Class Specification**

###### **3.6.1.3.1 ReleaseController Description Table**

Figure 3.6.1.3.1 ReleaseController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateRelease(ReleaseDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.6.1.3.2 ReleaseService Description Table**

Figure 3.6.1.3.2 ReleaseService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(ReleaseDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.6.1.3.3 DTOs Description Table**

Figure 3.6.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ReleaseDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.6.2 Execution Route and Robot Binding***

##### **3.6.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.6.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.6.2.3 Class Specification**

###### **3.6.2.3.1 BindingController Description Table**

Figure 3.6.2.3.1 BindingController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | BindRoute(BindRouteDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.6.2.3.2 BindingService Description Table**

Figure 3.6.2.3.2 BindingService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Bind(BindRouteDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.6.2.3.3 DTOs Description Table**

Figure 3.6.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | BindRouteDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.6.3 Release Publication and Retirement***

##### **3.6.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.6.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.6.3.3 Class Specification**

###### **3.6.3.3.1 ReleaseController Description Table**

Figure 3.6.3.3.1 ReleaseController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | PublishRelease(releaseId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.6.3.3.2 ReleaseService Description Table**

Figure 3.6.3.3.2 ReleaseService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Publish(releaseId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.6.3.3.3 DTOs Description Table**

Figure 3.6.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ReleaseStatusDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.6.4 Full Edge Configuration Deployment***

##### **3.6.4.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.6.4.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.6.4.3 Class Specification**

###### **3.6.4.3.1 DeploymentController Description Table**

Figure 3.6.4.3.1 DeploymentController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | DeployToEdge(DeployDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.6.4.3.2 DeploymentService Description Table**

Figure 3.6.4.3.2 DeploymentService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProcessDeployment(DeployDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.6.4.3.3 DTOs Description Table**

Figure 3.6.4.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | DeployDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.6.5 Low-cost Artifact-set Deployment***

##### **3.6.5.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.6.5.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.6.5.3 Class Specification**

###### **3.6.5.3.1 DeploymentController Description Table**

Figure 3.6.5.3.1 DeploymentController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | DeployArtifactSet(ArtifactSetDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.6.5.3.2 DeploymentService Description Table**

Figure 3.6.5.3.2 DeploymentService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProcessArtifactSet(ArtifactSetDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.6.5.3.3 DTOs Description Table**

Figure 3.6.5.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ArtifactSetDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.6.6 Deployment Rollback and Timeout Reconciliation***

##### **3.6.6.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.6.6.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.6.6.3 Class Specification**

###### **3.6.6.3.1 DeploymentController Description Table**

Figure 3.6.6.3.1 DeploymentController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Rollback(rollbackId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.6.6.3.2 DeploymentService Description Table**

Figure 3.6.6.3.2 DeploymentService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProcessRollback(rollbackId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.6.6.3.3 DTOs Description Table**

Figure 3.6.6.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | RollbackDto | Data transfer payload wrapper defining validations and required parameters. |

---

### **3.7 Production Execution Design**

#### ***3.7.1 Paid Order Dispatch***

##### **3.7.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.7.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.7.1.3 Class Specification**

###### **3.7.1.3.1 DispatchController Description Table**

Figure 3.7.1.3.1 DispatchController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | DispatchOrder(orderId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.7.1.3.2 DispatchService Description Table**

Figure 3.7.1.3.2 DispatchService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProcessDispatch(orderId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.7.1.3.3 DTOs Description Table**

Figure 3.7.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | DispatchDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.7.2 Edge Command Pull and Acknowledgement***

##### **3.7.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.7.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.7.2.3 Class Specification**

###### **3.7.2.3.1 EdgeCommandController Description Table**

Figure 3.7.2.3.1 EdgeCommandController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | PullCommands(endpointId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.7.2.3.2 EdgeCommandService Description Table**

Figure 3.7.2.3.2 EdgeCommandService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Pull(endpointId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.7.2.3.3 DTOs Description Table**

Figure 3.7.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | EdgeCommandDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.7.3 Production Job and Order-summary Report***

##### **3.7.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.7.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.7.3.3 Class Specification**

###### **3.7.3.3.1 ReportController Description Table**

Figure 3.7.3.3.1 ReportController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | SubmitJobReport(ReportDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.7.3.3.2 ReportService Description Table**

Figure 3.7.3.3.2 ReportService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProcessReport(ReportDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.7.3.3.3 DTOs Description Table**

Figure 3.7.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ReportDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.7.4 Execution Retry and Redispatch***

##### **3.7.4.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.7.4.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.7.4.3 Class Specification**

###### **3.7.4.3.1 DispatchController Description Table**

Figure 3.7.4.3.1 DispatchController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | RetryDispatch(dispatchId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.7.4.3.2 DispatchService Description Table**

Figure 3.7.4.3.2 DispatchService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Retry(dispatchId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.7.4.3.3 DTOs Description Table**

Figure 3.7.4.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | RetryDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.7.5 Stale and Unreachable Execution Reconciliation***

##### **3.7.5.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.7.5.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.7.5.3 Class Specification**

###### **3.7.5.3.1 DispatchController Description Table**

Figure 3.7.5.3.1 DispatchController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ReconcileStale() | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.7.5.3.2 DispatchService Description Table**

Figure 3.7.5.3.2 DispatchService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Reconcile() | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.7.5.3.3 DTOs Description Table**

Figure 3.7.5.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ReconciliationDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.7.6 Execution Attempt Query and Customer Projection***

##### **3.7.6.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.7.6.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.7.6.3 Class Specification**

###### **3.7.6.3.1 ExecutionQueryController Description Table**

Figure 3.7.6.3.1 ExecutionQueryController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetAttempts(orderId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.7.6.3.2 ExecutionQueryService Description Table**

Figure 3.7.6.3.2 ExecutionQueryService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetAttempts(orderId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.7.6.3.3 DTOs Description Table**

Figure 3.7.6.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ExecutionAttemptDto | Data transfer payload wrapper defining validations and required parameters. |

---

### **3.8 Device, Telemetry and Sync Design**

#### ***3.8.1 Device Management***

##### **3.8.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.8.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.8.1.3 Class Specification**

###### **3.8.1.3.1 DeviceController Description Table**

Figure 3.8.1.3.1 DeviceController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateDevice(DeviceDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.8.1.3.2 DeviceService Description Table**

Figure 3.8.1.3.2 DeviceService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(DeviceDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.8.1.3.3 DTOs Description Table**

Figure 3.8.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | DeviceDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.8.2 Execution Endpoint Provisioning and Credentials***

##### **3.8.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.8.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.8.2.3 Class Specification**

###### **3.8.2.3.1 EndpointController Description Table**

Figure 3.8.2.3.1 EndpointController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProvisionEndpoint(ProvisionDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.8.2.3.2 EndpointService Description Table**

Figure 3.8.2.3.2 EndpointService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Provision(ProvisionDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.8.2.3.3 DTOs Description Table**

Figure 3.8.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ProvisionDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.8.3 Readiness and Capability Ingestion***

##### **3.8.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.8.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.8.3.3 Class Specification**

###### **3.8.3.3.1 EndpointController Description Table**

Figure 3.8.3.3.1 EndpointController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | SubmitCapabilities(CapDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.8.3.3.2 EndpointService Description Table**

Figure 3.8.3.3.2 EndpointService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Ingest(CapDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.8.3.3.3 DTOs Description Table**

Figure 3.8.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | CapDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.8.4 Heartbeat and Connectivity State***

##### **3.8.4.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.8.4.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.8.4.3 Class Specification**

###### **3.8.4.3.1 HeartbeatController Description Table**

Figure 3.8.4.3.1 HeartbeatController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | SendHeartbeat(HeartbeatDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.8.4.3.2 HeartbeatService Description Table**

Figure 3.8.4.3.2 HeartbeatService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProcessHeartbeat(HeartbeatDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.8.4.3.3 DTOs Description Table**

Figure 3.8.4.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | HeartbeatDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.8.5 Device Event and Alert Creation***

##### **3.8.5.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.8.5.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.8.5.3 Class Specification**

###### **3.8.5.3.1 DeviceEventController Description Table**

Figure 3.8.5.3.1 DeviceEventController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | SubmitEvent(EventDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.8.5.3.2 DeviceEventService Description Table**

Figure 3.8.5.3.2 DeviceEventService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ProcessEvent(EventDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.8.5.3.3 DTOs Description Table**

Figure 3.8.5.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | EventDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.8.6 Telemetry Batch Replay***

##### **3.8.6.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.8.6.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.8.6.3 Class Specification**

###### **3.8.6.3.1 TelemetryController Description Table**

Figure 3.8.6.3.1 TelemetryController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ReplayBatch(BatchDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.8.6.3.2 TelemetryService Description Table**

Figure 3.8.6.3.2 TelemetryService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Replay(BatchDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.8.6.3.3 DTOs Description Table**

Figure 3.8.6.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | BatchDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.8.7 Production Event Checkpoint Sync***

##### **3.8.7.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.8.7.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.8.7.3 Class Specification**

###### **3.8.7.3.1 SyncController Description Table**

Figure 3.8.7.3.1 SyncController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | SyncCheckpoint(SyncDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.8.7.3.2 SyncService Description Table**

Figure 3.8.7.3.2 SyncService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Sync(SyncDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.8.7.3.3 DTOs Description Table**

Figure 3.8.7.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | SyncDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.8.8 Sync Dead-letter Recovery***

##### **3.8.8.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.8.8.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.8.8.3 Class Specification**

###### **3.8.8.3.1 SyncController Description Table**

Figure 3.8.8.3.1 SyncController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | RecoverDeadLetter(deadLetterId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.8.8.3.2 SyncService Description Table**

Figure 3.8.8.3.2 SyncService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Recover(deadLetterId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.8.8.3.3 DTOs Description Table**

Figure 3.8.8.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | RecoveryDto | Data transfer payload wrapper defining validations and required parameters. |

---

### **3.9 Inventory and Operations Design**

#### ***3.9.1 Dispenser State and Stock Movement Query***

##### **3.9.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.9.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.9.1.3 Class Specification**

###### **3.9.1.3.1 InventoryController Description Table**

Figure 3.9.1.3.1 InventoryController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetDispenserStates(kioskId) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.9.1.3.2 InventoryService Description Table**

Figure 3.9.1.3.2 InventoryService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetStates(kioskId) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.9.1.3.3 DTOs Description Table**

Figure 3.9.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | DispenserStateDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.9.2 Refill and Adjust Inventory Estimate***

##### **3.9.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.9.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.9.2.3 Class Specification**

###### **3.9.2.3.1 InventoryController Description Table**

Figure 3.9.2.3.1 InventoryController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | RefillDispenser(RefillDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.9.2.3.2 InventoryService Description Table**

Figure 3.9.2.3.2 InventoryService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Refill(RefillDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.9.2.3.3 DTOs Description Table**

Figure 3.9.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | RefillDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.9.3 Maintenance Ticket Lifecycle***

##### **3.9.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.9.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.9.3.3 Class Specification**

###### **3.9.3.3.1 MaintenanceController Description Table**

Figure 3.9.3.3.1 MaintenanceController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CreateTicket(TicketDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.9.3.3.2 MaintenanceService Description Table**

Figure 3.9.3.3.2 MaintenanceService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Create(TicketDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.9.3.3.3 DTOs Description Table**

Figure 3.9.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | TicketDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.9.4 Alert Acknowledge and Resolution***

##### **3.9.4.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.9.4.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.9.4.3 Class Specification**

###### **3.9.4.3.1 AlertController Description Table**

Figure 3.9.4.3.1 AlertController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ResolveAlert(ResolveAlertDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.9.4.3.2 AlertService Description Table**

Figure 3.9.4.3.2 AlertService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Resolve(ResolveAlertDto) | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.9.4.3.3 DTOs Description Table**

Figure 3.9.4.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | ResolveAlertDto | Data transfer payload wrapper defining validations and required parameters. |

---

### **3.10 Management Read and Realtime Design**

#### ***3.10.1 GraphQL Dashboard Query***

##### **3.10.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.10.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.10.1.3 Class Specification**

###### **3.10.1.3.1 DashboardResolver Description Table**

Figure 3.10.1.3.1 DashboardResolver Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetDashboardMetrics() | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.10.1.3.2 DashboardService Description Table**

Figure 3.10.1.3.2 DashboardService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ComputeMetrics() | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.10.1.3.3 DTOs Description Table**

Figure 3.10.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | DashboardMetricsModel | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.10.2 Tenant, Order, Kiosk and Inventory Overview Queries***

##### **3.10.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.10.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.10.2.3 Class Specification**

###### **3.10.2.3.1 OverviewController Description Table**

Figure 3.10.2.3.1 OverviewController Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | GetOverview() | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.10.2.3.2 OverviewService Description Table**

Figure 3.10.2.3.2 OverviewService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | ComputeOverview() | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.10.2.3.3 DTOs Description Table**

Figure 3.10.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | OverviewDto | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.10.3 SignalR Realtime Notification***

##### **3.10.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.10.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.10.3.3 Class Specification**

###### **3.10.3.3.1 TelemetryHub Description Table**

Figure 3.10.3.3.1 TelemetryHub Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | SendStatusUpdate(StatusDto) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.10.3.3.2 HubContext Description Table**

Figure 3.10.3.3.2 HubContext Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Clients.Group(...).SendAsync() | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.10.3.3.3 DTOs Description Table**

Figure 3.10.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | StatusDto | Data transfer payload wrapper defining validations and required parameters. |

---

### **3.11 Operational Background Process Design**

#### ***3.11.1 Object Storage Startup Validation and Orphan Cleanup***

##### **3.11.1.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.11.1.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.11.1.3 Class Specification**

###### **3.11.1.3.1 StorageCleanupJob Description Table**

Figure 3.11.1.3.1 StorageCleanupJob Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Execute(context) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.11.1.3.2 StorageService Description Table**

Figure 3.11.1.3.2 StorageService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CleanupOrphans() | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.11.1.3.3 DTOs Description Table**

Figure 3.11.1.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | None | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.11.2 Telemetry and Sync Retention***

##### **3.11.2.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.11.2.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.11.2.3 Class Specification**

###### **3.11.2.3.1 RetentionJob Description Table**

Figure 3.11.2.3.1 RetentionJob Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Execute(context) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.11.2.3.2 DataService Description Table**

Figure 3.11.2.3.2 DataService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | PurgeStaleRecords() | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.11.2.3.3 DTOs Description Table**

Figure 3.11.2.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | None | Data transfer payload wrapper defining validations and required parameters. |

---

#### ***3.11.3 Deployment and Execution Timeout Jobs***

##### **3.11.3.1 Class Diagram**

*(Diagram/Image skipped)*

##### **3.11.3.2 Sequence Diagram**

*(Diagram/Image skipped)*

##### **3.11.3.3 Class Specification**

###### **3.11.3.3.1 TimeoutReconciliationJob Description Table**

Figure 3.11.3.3.1 TimeoutReconciliationJob Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | Execute(context) | Coordinates HTTP request inputs, checks model validations, handles exceptions, and forwards params to the application service layers. |

###### **3.11.3.3.2 DeploymentService Description Table**

Figure 3.11.3.3.2 DeploymentService Description Table

| No | Method | Description |
| :--- | :--- | :--- |
| 01 | CheckTimeouts() | Executes core business workflow rules, modifies DB context tracking through the unit of work, and returns responses. |

###### **3.11.3.3.3 DTOs Description Table**

Figure 3.11.3.3.3 DTOs Description Table

| No | Class | Description |
| :--- | :--- | :--- |
| 01 | None | Data transfer payload wrapper defining validations and required parameters. |

---

