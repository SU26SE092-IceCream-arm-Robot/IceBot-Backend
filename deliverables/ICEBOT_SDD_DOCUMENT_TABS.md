# IceBot Software Design Document - Document Tabs

## I. Record of Changes

## II. Software Design Document

### 1. System Design

#### 1.1. System Architecture

##### 1.1.1. System Context Diagram

##### 1.1.2. Container Diagram

##### 1.1.3. Cloud Backend Layered Architecture

##### 1.1.4. Bounded Context Architecture

##### 1.1.5. Cloud, Edge Runtime and Machine Boundary

##### 1.1.6. Full Edge and Low-cost Controller Deployment Profiles

##### 1.1.7. REST, GraphQL, SignalR and MQTT Communication Architecture

##### 1.1.8. Authentication, RBAC and Tenant Isolation Architecture

##### 1.1.9. PostgreSQL and Object Storage Architecture

##### 1.1.10. Logging, Tracing and Metrics Architecture

#### 1.2. Package Diagram

##### 1.2.1. IceBot Backend Package Diagram

###### 1.2.1.1. Domain Layer Package Description

###### 1.2.1.2. Application Layer Package Description

###### 1.2.1.3. Infrastructure Layer Package Description

###### 1.2.1.4. WebAPI Layer Package Description

###### 1.2.1.5. Identity Package Description

###### 1.2.1.6. Tenants Package Description

###### 1.2.1.7. Catalog and Sales Catalog Package Description

###### 1.2.1.8. Orders and Payments Package Description

###### 1.2.1.9. Robot Configuration Package Description

###### 1.2.1.10. Production Configuration Package Description

###### 1.2.1.11. Production Execution Package Description

###### 1.2.1.12. Devices and Inventory Package Description

###### 1.2.1.13. Operations and Sync Package Description

##### 1.2.2. Management Web App Package Diagram

###### 1.2.2.1. App Routing Package Description

###### 1.2.2.2. Authentication and Authorization Package Description

###### 1.2.2.3. Management Feature Package Description

###### 1.2.2.4. REST and GraphQL Client Package Description

###### 1.2.2.5. SignalR Client Package Description

###### 1.2.2.6. Shared UI Component Package Description

##### 1.2.3. Kiosk/Tablet App Package Diagram

###### 1.2.3.1. Runtime Menu Package Description

###### 1.2.3.2. Cart and Checkout Package Description

###### 1.2.3.3. Payment Package Description

###### 1.2.3.4. Order Tracking Package Description

###### 1.2.3.5. Local Cache and Recovery Package Description

##### 1.2.4. Fairino Studio Package Diagram

###### 1.2.4.1. Blockly Editor Package Description

###### 1.2.4.2. Robot Program Validation Package Description

###### 1.2.4.3. Lua Generator Package Description

###### 1.2.4.4. Artifact Export and Upload Package Description

##### 1.2.5. Full Edge Runtime Package Diagram

###### 1.2.5.1. Cloud Connector Package Description

###### 1.2.5.2. Local Command Inbox Package Description

###### 1.2.5.3. Artifact Cache and Activation Package Description

###### 1.2.5.4. Production Scheduler Package Description

###### 1.2.5.5. Machine Adapter Package Description

###### 1.2.5.6. Local Event Outbox Package Description

##### 1.2.6. Low-cost Controller Package Diagram

###### 1.2.6.1. Command Receiver Package Description

###### 1.2.6.2. Active Artifact Set Package Description

###### 1.2.6.3. Lua Execution Package Description

###### 1.2.6.4. Persistent Deduplication Buffer Package Description

### 2. Database Design

#### 2.1. Logical ERD

##### 2.1.1. Identity and Access Logical ERD

##### 2.1.2. Organization, Store and Kiosk Logical ERD

##### 2.1.3. Catalog, Recipe and Menu Logical ERD

##### 2.1.4. Order, Payment and Refund Logical ERD

##### 2.1.5. Robot Artifact and Robot Program Logical ERD

##### 2.1.6. Configuration Release and Deployment Logical ERD

##### 2.1.7. Production Execution Logical ERD

##### 2.1.8. Device, Endpoint and Telemetry Logical ERD

##### 2.1.9. Inventory Logical ERD

##### 2.1.10. Alert and Maintenance Logical ERD

##### 2.1.11. Edge Command, Sync and Dead-letter Logical ERD

#### 2.2. Physical ERD

##### 2.2.1. Identity and Tenant Physical ERD

##### 2.2.2. Catalog and Sales Catalog Physical ERD

##### 2.2.3. Order and Payment Physical ERD

##### 2.2.4. Robot and Production Configuration Physical ERD

##### 2.2.5. Execution, Device and Telemetry Physical ERD

##### 2.2.6. Inventory, Operations and Sync Physical ERD

##### 2.2.7. Composite Tenant Constraints and Indexes

##### 2.2.8. Soft-delete Filters and Retention Rules

#### 2.3. Object Storage Design

##### 2.3.1. Robot Artifact Bucket and Object-key Layout

##### 2.3.2. Artifact Metadata and Checksum Mapping

##### 2.3.3. Presigned Download URL Flow

##### 2.3.4. Orphan Object Cleanup

### 3. Detailed Design

#### 3.1. Identity and Access Design

##### 3.1.1. Local Account Login

###### 3.1.1.1. Class Diagram

###### 3.1.1.2. Sequence Diagram

###### 3.1.1.3. Class Specification

##### 3.1.2. Google/Firebase Login

###### 3.1.2.1. Class Diagram

###### 3.1.2.2. Sequence Diagram

###### 3.1.2.3. Class Specification

##### 3.1.3. Refresh and Revoke Token

###### 3.1.3.1. Class Diagram

###### 3.1.3.2. Sequence Diagram

###### 3.1.3.3. Class Specification

##### 3.1.4. Forgot and Reset Password

###### 3.1.4.1. Class Diagram

###### 3.1.4.2. Sequence Diagram

###### 3.1.4.3. Class Specification

##### 3.1.5. Account Invitation and Activation

###### 3.1.5.1. Class Diagram

###### 3.1.5.2. Sequence Diagram

###### 3.1.5.3. Class Specification

##### 3.1.6. Current Account Profile and Password

###### 3.1.6.1. Class Diagram

###### 3.1.6.2. Sequence Diagram

###### 3.1.6.3. Class Specification

##### 3.1.7. Internal Account and Role Management

###### 3.1.7.1. Class Diagram

###### 3.1.7.2. Sequence Diagram

###### 3.1.7.3. Class Specification

##### 3.1.8. Effective Access and Permission Matrix

###### 3.1.8.1. Class Diagram

###### 3.1.8.2. Sequence Diagram

###### 3.1.8.3. Class Specification

#### 3.2. Tenant Management Design

##### 3.2.1. Organization Management

###### 3.2.1.1. Class Diagram

###### 3.2.1.2. Sequence Diagram

###### 3.2.1.3. Class Specification

##### 3.2.2. Store Management

###### 3.2.2.1. Class Diagram

###### 3.2.2.2. Sequence Diagram

###### 3.2.2.3. Class Specification

##### 3.2.3. Kiosk Management

###### 3.2.3.1. Class Diagram

###### 3.2.3.2. Sequence Diagram

###### 3.2.3.3. Class Specification

##### 3.2.4. Tenant Tree and Role-scope Options

###### 3.2.4.1. Class Diagram

###### 3.2.4.2. Sequence Diagram

###### 3.2.4.3. Class Specification

#### 3.3. Catalog and Menu Design

##### 3.3.1. Product Template Management

###### 3.3.1.1. Class Diagram

###### 3.3.1.2. Sequence Diagram

###### 3.3.1.3. Class Specification

##### 3.3.2. Product and Variant Management

###### 3.3.2.1. Class Diagram

###### 3.3.2.2. Sequence Diagram

###### 3.3.2.3. Class Specification

##### 3.3.3. Recipe and Ingredient Management

###### 3.3.3.1. Class Diagram

###### 3.3.3.2. Sequence Diagram

###### 3.3.3.3. Class Specification

##### 3.3.4. Menu and Menu Item Management

###### 3.3.4.1. Class Diagram

###### 3.3.4.2. Sequence Diagram

###### 3.3.4.3. Class Specification

##### 3.3.5. Kiosk Runtime Menu Projection

###### 3.3.5.1. Class Diagram

###### 3.3.5.2. Sequence Diagram

###### 3.3.5.3. Class Specification

#### 3.4. Order and Payment Design

##### 3.4.1. Place and Cancel Order

###### 3.4.1.1. Class Diagram

###### 3.4.1.2. Sequence Diagram

###### 3.4.1.3. Class Specification

##### 3.4.2. Create Payment Session

###### 3.4.2.1. Class Diagram

###### 3.4.2.2. Sequence Diagram

###### 3.4.2.3. Class Specification

##### 3.4.3. Process PayOS Webhook

###### 3.4.3.1. Class Diagram

###### 3.4.3.2. Sequence Diagram

###### 3.4.3.3. Class Specification

##### 3.4.4. Query Order and Payment Status

###### 3.4.4.1. Class Diagram

###### 3.4.4.2. Sequence Diagram

###### 3.4.4.3. Class Specification

##### 3.4.5. Back-office Order and Status History

###### 3.4.5.1. Class Diagram

###### 3.4.5.2. Sequence Diagram

###### 3.4.5.3. Class Specification

##### 3.4.6. Refund Management

###### 3.4.6.1. Class Diagram

###### 3.4.6.2. Sequence Diagram

###### 3.4.6.3. Class Specification

##### 3.4.7. Payment Method Management

###### 3.4.7.1. Class Diagram

###### 3.4.7.2. Sequence Diagram

###### 3.4.7.3. Class Specification

#### 3.5. Robot Artifact and Program Design

##### 3.5.1. Global Robot Artifact Template Management

###### 3.5.1.1. Class Diagram

###### 3.5.1.2. Sequence Diagram

###### 3.5.1.3. Class Specification

##### 3.5.2. Organization Robot Artifact Upload and Review

###### 3.5.2.1. Class Diagram

###### 3.5.2.2. Sequence Diagram

###### 3.5.2.3. Class Specification

##### 3.5.3. Artifact Publish, Retire and Discard

###### 3.5.3.1. Class Diagram

###### 3.5.3.2. Sequence Diagram

###### 3.5.3.3. Class Specification

##### 3.5.4. Robot Program and Ordered Artifact Manifest

###### 3.5.4.1. Class Diagram

###### 3.5.4.2. Sequence Diagram

###### 3.5.4.3. Class Specification

#### 3.6. Production Configuration Design

##### 3.6.1. Configuration Release Authoring

###### 3.6.1.1. Class Diagram

###### 3.6.1.2. Sequence Diagram

###### 3.6.1.3. Class Specification

##### 3.6.2. Execution Route and Robot Binding

###### 3.6.2.1. Class Diagram

###### 3.6.2.2. Sequence Diagram

###### 3.6.2.3. Class Specification

##### 3.6.3. Release Publication and Retirement

###### 3.6.3.1. Class Diagram

###### 3.6.3.2. Sequence Diagram

###### 3.6.3.3. Class Specification

##### 3.6.4. Full Edge Configuration Deployment

###### 3.6.4.1. Class Diagram

###### 3.6.4.2. Sequence Diagram

###### 3.6.4.3. Class Specification

##### 3.6.5. Low-cost Artifact-set Deployment

###### 3.6.5.1. Class Diagram

###### 3.6.5.2. Sequence Diagram

###### 3.6.5.3. Class Specification

##### 3.6.6. Deployment Rollback and Timeout Reconciliation

###### 3.6.6.1. Class Diagram

###### 3.6.6.2. Sequence Diagram

###### 3.6.6.3. Class Specification

#### 3.7. Production Execution Design

##### 3.7.1. Paid Order Dispatch

###### 3.7.1.1. Class Diagram

###### 3.7.1.2. Sequence Diagram

###### 3.7.1.3. Class Specification

##### 3.7.2. Edge Command Pull and Acknowledgement

###### 3.7.2.1. Class Diagram

###### 3.7.2.2. Sequence Diagram

###### 3.7.2.3. Class Specification

##### 3.7.3. Production Job and Order-summary Report

###### 3.7.3.1. Class Diagram

###### 3.7.3.2. Sequence Diagram

###### 3.7.3.3. Class Specification

##### 3.7.4. Execution Retry and Redispatch

###### 3.7.4.1. Class Diagram

###### 3.7.4.2. Sequence Diagram

###### 3.7.4.3. Class Specification

##### 3.7.5. Stale and Unreachable Execution Reconciliation

###### 3.7.5.1. Class Diagram

###### 3.7.5.2. Sequence Diagram

###### 3.7.5.3. Class Specification

##### 3.7.6. Execution Attempt Query and Customer Projection

###### 3.7.6.1. Class Diagram

###### 3.7.6.2. Sequence Diagram

###### 3.7.6.3. Class Specification

#### 3.8. Device, Telemetry and Sync Design

##### 3.8.1. Device Management

###### 3.8.1.1. Class Diagram

###### 3.8.1.2. Sequence Diagram

###### 3.8.1.3. Class Specification

##### 3.8.2. Execution Endpoint Provisioning and Credentials

###### 3.8.2.1. Class Diagram

###### 3.8.2.2. Sequence Diagram

###### 3.8.2.3. Class Specification

##### 3.8.3. Readiness and Capability Ingestion

###### 3.8.3.1. Class Diagram

###### 3.8.3.2. Sequence Diagram

###### 3.8.3.3. Class Specification

##### 3.8.4. Heartbeat and Connectivity State

###### 3.8.4.1. Class Diagram

###### 3.8.4.2. Sequence Diagram

###### 3.8.4.3. Class Specification

##### 3.8.5. Device Event and Alert Creation

###### 3.8.5.1. Class Diagram

###### 3.8.5.2. Sequence Diagram

###### 3.8.5.3. Class Specification

##### 3.8.6. Telemetry Batch Replay

###### 3.8.6.1. Class Diagram

###### 3.8.6.2. Sequence Diagram

###### 3.8.6.3. Class Specification

##### 3.8.7. Production Event Checkpoint Sync

###### 3.8.7.1. Class Diagram

###### 3.8.7.2. Sequence Diagram

###### 3.8.7.3. Class Specification

##### 3.8.8. Sync Dead-letter Recovery

###### 3.8.8.1. Class Diagram

###### 3.8.8.2. Sequence Diagram

###### 3.8.8.3. Class Specification

#### 3.9. Inventory and Operations Design

##### 3.9.1. Dispenser State and Stock Movement Query

###### 3.9.1.1. Class Diagram

###### 3.9.1.2. Sequence Diagram

###### 3.9.1.3. Class Specification

##### 3.9.2. Refill and Adjust Inventory Estimate

###### 3.9.2.1. Class Diagram

###### 3.9.2.2. Sequence Diagram

###### 3.9.2.3. Class Specification

##### 3.9.3. Maintenance Ticket Lifecycle

###### 3.9.3.1. Class Diagram

###### 3.9.3.2. Sequence Diagram

###### 3.9.3.3. Class Specification

##### 3.9.4. Alert Acknowledge and Resolution

###### 3.9.4.1. Class Diagram

###### 3.9.4.2. Sequence Diagram

###### 3.9.4.3. Class Specification

#### 3.10. Management Read and Realtime Design

##### 3.10.1. GraphQL Dashboard Query

###### 3.10.1.1. Class Diagram

###### 3.10.1.2. Sequence Diagram

###### 3.10.1.3. Class Specification

##### 3.10.2. Tenant, Order, Kiosk and Inventory Overview Queries

###### 3.10.2.1. Class Diagram

###### 3.10.2.2. Sequence Diagram

###### 3.10.2.3. Class Specification

##### 3.10.3. SignalR Realtime Notification

###### 3.10.3.1. Class Diagram

###### 3.10.3.2. Sequence Diagram

###### 3.10.3.3. Class Specification

#### 3.11. Operational Background Process Design

##### 3.11.1. Object Storage Startup Validation and Orphan Cleanup

###### 3.11.1.1. Class Diagram

###### 3.11.1.2. Sequence Diagram

###### 3.11.1.3. Class Specification

##### 3.11.2. Telemetry and Sync Retention

###### 3.11.2.1. Class Diagram

###### 3.11.2.2. Sequence Diagram

###### 3.11.2.3. Class Specification

##### 3.11.3. Deployment and Execution Timeout Jobs

###### 3.11.3.1. Class Diagram

###### 3.11.3.2. Sequence Diagram

###### 3.11.3.3. Class Specification
