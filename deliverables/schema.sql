--
-- PostgreSQL database dump
--

\restrict ZyoqaYjPylAxkHCQ7VCQQbQ8wEvcKF3nicGJ4xRHfU26piDBOlckBNqeu2r05Pv

-- Dumped from database version 17.10 (Debian 17.10-1.pgdg13+1)
-- Dumped by pg_dump version 17.10 (Debian 17.10-1.pgdg13+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: AccountDevices; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."AccountDevices" (
    "Id" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "DeviceName" character varying(500) NOT NULL,
    "Platform" character varying(500) NOT NULL,
    "AppVersion" character varying(500),
    "DeviceTokenHash" character varying(500),
    "PushToken" character varying(500),
    "IsTrusted" boolean NOT NULL,
    "LastSeenAt" timestamp with time zone,
    "RevokedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."AccountDevices" OWNER TO postgres;

--
-- Name: AccountInvitations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."AccountInvitations" (
    "Id" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "TokenHash" character varying(128) NOT NULL,
    "InvitedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "EmailSentAt" timestamp with time zone,
    "AcceptedAt" timestamp with time zone,
    "RevokedAt" timestamp with time zone,
    "InvitedByAccountId" uuid,
    "AcceptedByIp" character varying(500),
    "AcceptedByUserAgent" character varying(500),
    "Purpose" character varying(50) NOT NULL
);


ALTER TABLE public."AccountInvitations" OWNER TO postgres;

--
-- Name: AccountRoles; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."AccountRoles" (
    "Id" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "RoleId" bigint NOT NULL,
    "OrganizationId" uuid,
    "StoreId" uuid,
    "KioskId" uuid,
    "IsActive" boolean NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL,
    "AssignedByAccountId" uuid
);


ALTER TABLE public."AccountRoles" OWNER TO postgres;

--
-- Name: AccountStores; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."AccountStores" (
    "AccountId" uuid NOT NULL,
    "StoreId" uuid NOT NULL
);


ALTER TABLE public."AccountStores" OWNER TO postgres;

--
-- Name: Accounts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Accounts" (
    "Id" uuid NOT NULL,
    "UserName" character varying(500) NOT NULL,
    "FullName" character varying(500),
    "Email" character varying(500) NOT NULL,
    "EmailConfirmed" boolean NOT NULL,
    "EmailConfirmedAt" timestamp with time zone,
    "PasswordHash" character varying(512),
    "ImageUrl" character varying(500),
    "PhoneNumber" character varying(500),
    "PhoneNumberConfirmed" boolean NOT NULL,
    "PhoneNumberConfirmedAt" timestamp with time zone,
    "Address" character varying(500),
    "Gender" character varying(500) NOT NULL,
    "Status" integer NOT NULL,
    "LocalLoginEnabled" boolean NOT NULL,
    "GoogleLoginEnabled" boolean NOT NULL,
    "GoogleSubjectId" character varying(500),
    "GoogleEmail" character varying(500),
    "LastLoginAt" timestamp with time zone,
    "LockedUntil" timestamp with time zone,
    "FailedLoginCount" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Accounts" OWNER TO postgres;

--
-- Name: Alerts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Alerts" (
    "Id" uuid NOT NULL,
    "KioskId" uuid NOT NULL,
    "DeviceId" uuid,
    "AcknowledgedByAccountId" uuid,
    "AlertCode" character varying(500) NOT NULL,
    "Severity" integer NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Message" character varying(500),
    "Status" integer NOT NULL,
    "SourceType" character varying(500),
    "SourceId" uuid,
    "RaisedAt" timestamp with time zone NOT NULL,
    "AcknowledgedAt" timestamp with time zone,
    "ResolvedAt" timestamp with time zone,
    "ResolutionNotes" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid,
    "OriginNodeId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "SyncedAt" timestamp with time zone
);


ALTER TABLE public."Alerts" OWNER TO postgres;

--
-- Name: ConfigurationReleases; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ConfigurationReleases" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "ReleaseNumber" bigint NOT NULL,
    "Status" integer NOT NULL,
    "ReleaseManifestSchemaVersion" integer NOT NULL,
    "ManifestJson" jsonb,
    "ReleaseChecksum" character varying(500),
    "PublishedAt" timestamp with time zone,
    "PublishedByAccountId" uuid,
    "RetiredAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."ConfigurationReleases" OWNER TO postgres;

--
-- Name: ControllerArtifactSetDeployments; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ControllerArtifactSetDeployments" (
    "Id" uuid NOT NULL,
    "KioskId" uuid NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "ControllerId" uuid NOT NULL,
    "SourceConfigurationReleaseId" uuid NOT NULL,
    "ReleaseChecksum" character varying(500) NOT NULL,
    "ActiveSetVersion" bigint NOT NULL,
    "ActiveSetChecksum" character varying(500) NOT NULL,
    "MaxArtifactCount" integer NOT NULL,
    "MaxArtifactStorageBytes" bigint NOT NULL,
    "RequestedArtifactCount" integer NOT NULL,
    "RequestedArtifactStorageBytes" bigint NOT NULL,
    "Status" integer NOT NULL,
    "RequestedByAccountId" uuid,
    "RequestedAt" timestamp with time zone NOT NULL,
    "ControllerReportedAt" timestamp with time zone,
    "CloudReceivedAt" timestamp with time zone,
    "LastControllerReportId" uuid,
    "FailureCode" character varying(500),
    "FailureReason" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "IdempotencyKey" character varying(200) NOT NULL,
    "OrganizationId" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


ALTER TABLE public."ControllerArtifactSetDeployments" OWNER TO postgres;

--
-- Name: ControllerArtifactSetItems; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ControllerArtifactSetItems" (
    "Id" uuid NOT NULL,
    "ControllerArtifactSetDeploymentId" uuid NOT NULL,
    "ExecutionRouteId" uuid NOT NULL,
    "RobotProgramId" uuid NOT NULL,
    "RobotProgramManifestChecksum" character varying(500) NOT NULL,
    "RobotArtifactId" uuid NOT NULL,
    "ArtifactChecksum" character varying(500) NOT NULL,
    "StorageKey" character varying(500) NOT NULL,
    "RuntimeTargetCode" character varying(500) NOT NULL,
    "MachineModelCode" character varying(500) NOT NULL,
    "DeviceId" uuid,
    "ContentLengthBytes" bigint NOT NULL,
    "RunOrder" integer NOT NULL,
    "ParametersSchemaVersion" integer NOT NULL,
    "ParametersJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."ControllerArtifactSetItems" OWNER TO postgres;

--
-- Name: DeviceEvents; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."DeviceEvents" (
    "Id" uuid NOT NULL,
    "DeviceId" uuid NOT NULL,
    "KioskId" uuid,
    "EventId" uuid NOT NULL,
    "CorrelationId" uuid,
    "CausationId" uuid,
    "EventType" character varying(500) NOT NULL,
    "Severity" integer NOT NULL,
    "Message" character varying(500),
    "PayloadJson" jsonb,
    "OccurredAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "OriginNodeId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "SyncedAt" timestamp with time zone
);


ALTER TABLE public."DeviceEvents" OWNER TO postgres;

--
-- Name: DeviceModels; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."DeviceModels" (
    "Id" uuid NOT NULL,
    "DeviceTypeId" bigint NOT NULL,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "Manufacturer" character varying(500),
    "ModelNumber" character varying(500),
    "FirmwareFamily" character varying(500),
    "CapabilitiesSchemaVersion" integer NOT NULL,
    "CapabilitiesJson" jsonb,
    "MetadataJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."DeviceModels" OWNER TO postgres;

--
-- Name: DeviceTypes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."DeviceTypes" (
    "Id" bigint NOT NULL,
    "Category" character varying(500) NOT NULL,
    "RequiresKioskAssignment" boolean NOT NULL,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."DeviceTypes" OWNER TO postgres;

--
-- Name: DeviceTypes_Id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."DeviceTypes" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."DeviceTypes_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Devices; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Devices" (
    "Id" uuid NOT NULL,
    "DeviceTypeId" bigint NOT NULL,
    "DeviceModelId" uuid,
    "KioskId" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "SerialNumber" character varying(500),
    "Status" integer NOT NULL,
    "PositionLabel" character varying(500),
    "FirmwareVersion" character varying(500),
    "InstalledAt" timestamp with time zone,
    "LastSeenAt" timestamp with time zone,
    "MetadataJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Devices" OWNER TO postgres;

--
-- Name: EdgeCommandDeliveryAttempts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."EdgeCommandDeliveryAttempts" (
    "Id" uuid NOT NULL,
    "EdgeCommandId" uuid NOT NULL,
    "DeliveryAttemptNo" integer NOT NULL,
    "SentAt" timestamp with time zone NOT NULL,
    "Outcome" integer NOT NULL,
    "ResponseCode" character varying(500),
    "ResponseMessage" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."EdgeCommandDeliveryAttempts" OWNER TO postgres;

--
-- Name: EdgeCommands; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."EdgeCommands" (
    "Id" uuid NOT NULL,
    "CommandType" integer NOT NULL,
    "DispatchAttemptNo" integer,
    "OrderId" uuid,
    "KioskId" uuid NOT NULL,
    "TargetExecutionEndpointId" uuid NOT NULL,
    "PayloadJson" jsonb NOT NULL,
    "CommandExpiryAt" timestamp with time zone,
    "Status" integer NOT NULL,
    "RejectionCode" character varying(500),
    "RejectionMessage" character varying(500),
    "DeliveredAt" timestamp with time zone,
    "RespondedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeploymentId" uuid,
    "DeploymentKind" integer
);


ALTER TABLE public."EdgeCommands" OWNER TO postgres;

--
-- Name: EdgeStateSummaries; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."EdgeStateSummaries" (
    "Id" uuid NOT NULL,
    "KioskId" uuid NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "SourceExecutorId" uuid NOT NULL,
    "SummaryKind" character varying(100) NOT NULL,
    "StateRevision" bigint NOT NULL,
    "SummarySchemaVersion" integer NOT NULL,
    "EdgeCreatedAt" timestamp with time zone NOT NULL,
    "CloudReceivedAt" timestamp with time zone NOT NULL,
    "PayloadJson" jsonb NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."EdgeStateSummaries" OWNER TO postgres;

--
-- Name: ExecutionEndpointCapabilityProjections; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ExecutionEndpointCapabilityProjections" (
    "Id" uuid NOT NULL,
    "ExecutionEndpointReadinessProjectionId" uuid NOT NULL,
    "CapabilityCode" character varying(100) NOT NULL,
    "WorkcellCode" character varying(100),
    "IsAvailable" boolean NOT NULL,
    "UnavailableReason" character varying(500)
);


ALTER TABLE public."ExecutionEndpointCapabilityProjections" OWNER TO postgres;

--
-- Name: ExecutionEndpointCredentialBindings; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ExecutionEndpointCredentialBindings" (
    "Id" uuid NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "AuthenticationMode" integer NOT NULL,
    "CredentialReference" character varying(500) NOT NULL,
    "Status" integer NOT NULL,
    "ProvisionedAt" timestamp with time zone NOT NULL,
    "ActivatedAt" timestamp with time zone,
    "RevokedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "PublicKeyPem" character varying(500)
);


ALTER TABLE public."ExecutionEndpointCredentialBindings" OWNER TO postgres;

--
-- Name: ExecutionEndpointMqttCredentials; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ExecutionEndpointMqttCredentials" (
    "Id" uuid NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "Username" character varying(100) NOT NULL,
    "BrokerProvider" character varying(100) NOT NULL,
    "CredentialVersion" integer NOT NULL,
    "Status" integer NOT NULL,
    "ActivatedAt" timestamp with time zone,
    "RevokedAt" timestamp with time zone,
    "LastError" character varying(1000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."ExecutionEndpointMqttCredentials" OWNER TO postgres;

--
-- Name: ExecutionEndpointReadinessProjections; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ExecutionEndpointReadinessProjections" (
    "Id" uuid NOT NULL,
    "KioskId" uuid NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "SourceExecutorId" uuid NOT NULL,
    "StateRevision" bigint NOT NULL,
    "Readiness" integer NOT NULL,
    "Activity" integer NOT NULL,
    "Safety" integer NOT NULL,
    "CurrentCommandId" uuid,
    "PhysicalOutputState" integer NOT NULL,
    "FaultCode" character varying(100),
    "ExecutorReportedAt" timestamp with time zone NOT NULL,
    "CloudReceivedAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."ExecutionEndpointReadinessProjections" OWNER TO postgres;

--
-- Name: ExecutionEndpointRequestNonces; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ExecutionEndpointRequestNonces" (
    "Id" uuid NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "Nonce" uuid NOT NULL,
    "RequestTimestamp" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."ExecutionEndpointRequestNonces" OWNER TO postgres;

--
-- Name: ExecutionEndpointSupportedRobotTargets; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ExecutionEndpointSupportedRobotTargets" (
    "Id" uuid NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "RuntimeTargetCode" character varying(500) NOT NULL,
    "MachineModelCode" character varying(500) NOT NULL,
    "DeviceId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "KioskId" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


ALTER TABLE public."ExecutionEndpointSupportedRobotTargets" OWNER TO postgres;

--
-- Name: ExecutionRouteRobotBindings; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ExecutionRouteRobotBindings" (
    "Id" uuid NOT NULL,
    "ExecutionRouteId" uuid NOT NULL,
    "BindingOrder" integer NOT NULL,
    "RequiredWorkcellCapabilityCode" character varying(500) NOT NULL,
    "RobotProgramId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."ExecutionRouteRobotBindings" OWNER TO postgres;

--
-- Name: ExecutionRoutes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ExecutionRoutes" (
    "Id" uuid NOT NULL,
    "ConfigurationReleaseId" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "RouteCode" character varying(500) NOT NULL,
    "Priority" integer NOT NULL,
    "RequiredCapabilitiesJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."ExecutionRoutes" OWNER TO postgres;

--
-- Name: IngredientDispenserStates; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."IngredientDispenserStates" (
    "Id" uuid NOT NULL,
    "DeviceId" uuid NOT NULL,
    "KioskId" uuid,
    "IngredientId" uuid NOT NULL,
    "ContainerCode" character varying(500) NOT NULL,
    "CurrentLevelStatus" integer NOT NULL,
    "EstimatedQuantity" numeric(18,4),
    "CapacityQuantity" numeric(18,4),
    "Unit" character varying(500) NOT NULL,
    "LevelToQuantityProfileSchemaVersion" integer NOT NULL,
    "LevelToQuantityProfileJson" jsonb,
    "LastMeasuredAt" timestamp with time zone NOT NULL,
    "LastRefilledAt" timestamp with time zone,
    "ExpiresAt" timestamp with time zone,
    "SensorPayloadJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid,
    "OriginNodeId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "SyncedAt" timestamp with time zone
);


ALTER TABLE public."IngredientDispenserStates" OWNER TO postgres;

--
-- Name: Ingredients; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Ingredients" (
    "Id" uuid NOT NULL,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "IngredientType" character varying(500) NOT NULL,
    "Unit" character varying(500) NOT NULL,
    "Description" character varying(500),
    "StorageRequirement" character varying(500),
    "IsPerishable" boolean NOT NULL,
    "IsAllergen" boolean NOT NULL,
    "ShelfLifeDays" integer,
    "MetadataJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Ingredients" OWNER TO postgres;

--
-- Name: KioskConfigurationDeployments; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."KioskConfigurationDeployments" (
    "Id" uuid NOT NULL,
    "KioskId" uuid NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "EdgeRuntimeId" uuid NOT NULL,
    "ConfigurationReleaseId" uuid NOT NULL,
    "ReleaseChecksum" character varying(500) NOT NULL,
    "AttemptNo" integer NOT NULL,
    "Status" integer NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "RequestedByAccountId" uuid,
    "EdgeReportedAt" timestamp with time zone,
    "CloudReceivedAt" timestamp with time zone,
    "LastEdgeDeploymentEventId" uuid,
    "FailureCode" character varying(500),
    "FailureReason" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid,
    "IdempotencyKey" character varying(200) NOT NULL,
    "OrganizationId" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


ALTER TABLE public."KioskConfigurationDeployments" OWNER TO postgres;

--
-- Name: KioskExecutionEndpoints; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."KioskExecutionEndpoints" (
    "Id" uuid NOT NULL,
    "KioskId" uuid NOT NULL,
    "EndpointCode" character varying(500) NOT NULL,
    "ExecutionProfile" integer NOT NULL,
    "AuthenticationMode" integer NOT NULL,
    "CredentialBindingId" uuid,
    "Status" integer NOT NULL,
    "FullEdgeRuntimeId" uuid,
    "ControllerId" uuid,
    "ProvisionedAt" timestamp with time zone,
    "ActiveConfigurationDeploymentId" uuid,
    "ActiveConfigurationReleaseId" uuid,
    "ActiveConfigurationReleaseChecksum" character varying(500),
    "LastEdgeActivationEventId" uuid,
    "ActiveConfigurationEdgeReportedAt" timestamp with time zone,
    "ActiveConfigurationCloudReceivedAt" timestamp with time zone,
    "ActiveArtifactSetDeploymentId" uuid,
    "ActiveArtifactSetReleaseId" uuid,
    "ActiveArtifactSetReleaseChecksum" character varying(500),
    "ActiveArtifactSetVersion" bigint,
    "ActiveArtifactSetChecksum" character varying(500),
    "LastControllerActivationReportId" uuid,
    "ActiveArtifactSetControllerReportedAt" timestamp with time zone,
    "ActiveArtifactSetCloudReceivedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid,
    CONSTRAINT "CK_KioskExecutionEndpoints_ProfileIdentity" CHECK ((((("ExecutionProfile" = 1) AND ("ControllerId" IS NULL)) OR (("ExecutionProfile" = 2) AND ("FullEdgeRuntimeId" IS NULL))) AND (("Status" <> 2) OR ((("ExecutionProfile" = 1) AND ("FullEdgeRuntimeId" IS NOT NULL)) OR (("ExecutionProfile" = 2) AND ("ControllerId" IS NOT NULL))))))
);


ALTER TABLE public."KioskExecutionEndpoints" OWNER TO postgres;

--
-- Name: KioskHeartbeats; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."KioskHeartbeats" (
    "Id" uuid NOT NULL,
    "KioskId" uuid NOT NULL,
    "NodeId" uuid NOT NULL,
    "HeartbeatSequence" bigint,
    "ReportedAt" timestamp with time zone NOT NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "Status" integer NOT NULL,
    "RobotStatus" character varying(500),
    "NetworkStatus" character varying(500),
    "AppVersion" character varying(500),
    "FirmwareVersion" character varying(500),
    "CpuUsagePercent" numeric(18,4),
    "MemoryUsagePercent" numeric(18,4),
    "DiskUsagePercent" numeric(18,4),
    "PendingSyncEventCount" integer NOT NULL,
    "PayloadJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "OriginNodeId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "SyncedAt" timestamp with time zone
);


ALTER TABLE public."KioskHeartbeats" OWNER TO postgres;

--
-- Name: Kiosks; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Kiosks" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "StoreId" uuid NOT NULL,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "KioskType" character varying(500) NOT NULL,
    "Status" integer NOT NULL,
    "SerialNumber" character varying(500),
    "TimeZone" character varying(500) NOT NULL,
    "Address" character varying(500),
    "Latitude" numeric(18,4),
    "Longitude" numeric(18,4),
    "InstalledAt" timestamp with time zone,
    "LastOnlineAt" timestamp with time zone,
    "SupportsOfflineMode" boolean NOT NULL,
    "ConfigurationVersion" bigint NOT NULL,
    "SettingsSchemaVersion" integer NOT NULL,
    "SettingsJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Kiosks" OWNER TO postgres;

--
-- Name: MaintenanceTickets; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."MaintenanceTickets" (
    "Id" uuid NOT NULL,
    "KioskId" uuid NOT NULL,
    "DeviceId" uuid,
    "AssignedToAccountId" uuid,
    "TicketNumber" character varying(500) NOT NULL,
    "IssueCode" character varying(500) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Description" character varying(500),
    "Priority" integer NOT NULL,
    "Status" integer NOT NULL,
    "ReportedAt" timestamp with time zone NOT NULL,
    "DueAt" timestamp with time zone,
    "ResolvedAt" timestamp with time zone,
    "ClosedAt" timestamp with time zone,
    "ResolutionNotes" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid,
    "OriginNodeId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "SyncedAt" timestamp with time zone,
    "AssignedAt" timestamp with time zone,
    "CancelReason" character varying(500),
    "CancelledAt" timestamp with time zone,
    "DeviceEventId" uuid,
    "OrderId" uuid,
    "OrganizationId" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "StartedAt" timestamp with time zone,
    "StoreId" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


ALTER TABLE public."MaintenanceTickets" OWNER TO postgres;

--
-- Name: MenuItems; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."MenuItems" (
    "Id" uuid NOT NULL,
    "MenuId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "RecipeId" uuid,
    "Code" character varying(500) NOT NULL,
    "DisplayName" character varying(500) NOT NULL,
    "Description" character varying(500),
    "Status" integer NOT NULL,
    "Price" numeric(18,4) NOT NULL,
    "DiscountAmount" numeric(18,4) NOT NULL,
    "Currency" character varying(500) NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "PreparationTimeSeconds" integer,
    "ImageUrl" character varying(500),
    "EffectiveFrom" timestamp with time zone,
    "EffectiveTo" timestamp with time zone,
    "MetadataSchemaVersion" integer NOT NULL,
    "MetadataJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."MenuItems" OWNER TO postgres;

--
-- Name: Menus; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Menus" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid,
    "StoreId" uuid,
    "KioskId" uuid,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "Description" character varying(500),
    "Status" integer NOT NULL,
    "ScopeType" integer NOT NULL,
    "Currency" character varying(500) NOT NULL,
    "EffectiveFrom" timestamp with time zone,
    "EffectiveTo" timestamp with time zone,
    "DisplayOrder" integer NOT NULL,
    "MetadataSchemaVersion" integer NOT NULL,
    "MetadataJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Menus" OWNER TO postgres;

--
-- Name: OperationLogs; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."OperationLogs" (
    "Id" uuid NOT NULL,
    "AccountId" uuid,
    "KioskId" uuid,
    "DeviceId" uuid,
    "OrderId" uuid,
    "SourceEventId" uuid,
    "CorrelationId" uuid,
    "CausationId" uuid,
    "Action" character varying(500) NOT NULL,
    "Category" character varying(500) NOT NULL,
    "Severity" integer NOT NULL,
    "Message" character varying(500),
    "IpAddress" character varying(500),
    "UserAgent" character varying(500),
    "PayloadJson" jsonb,
    "OccurredAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "OriginNodeId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "SyncedAt" timestamp with time zone
);


ALTER TABLE public."OperationLogs" OWNER TO postgres;

--
-- Name: OptionGroups; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."OptionGroups" (
    "Id" bigint NOT NULL,
    "SelectionType" integer NOT NULL,
    "MinSelections" integer NOT NULL,
    "MaxSelections" integer NOT NULL,
    "IsRequired" boolean NOT NULL,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."OptionGroups" OWNER TO postgres;

--
-- Name: OptionGroups_Id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."OptionGroups" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."OptionGroups_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: OrderExecutionRecords; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."OrderExecutionRecords" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "SourceCommandId" uuid NOT NULL,
    "DispatchAttemptNo" integer NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "ExecutionProfile" integer NOT NULL,
    "SourceConfigurationReleaseId" uuid NOT NULL,
    "ReleaseChecksum" character varying(500) NOT NULL,
    "Status" integer NOT NULL,
    "ObservationStatus" integer NOT NULL,
    "CustomerExecutionStatus" integer NOT NULL,
    "SourceExecutorId" uuid NOT NULL,
    "LastAppliedSourceEventId" uuid NOT NULL,
    "LastAppliedSequenceNumber" bigint NOT NULL,
    "LastEdgeCreatedAt" timestamp with time zone NOT NULL,
    "LastExecutorReportedAt" timestamp with time zone NOT NULL,
    "CloudReceivedAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."OrderExecutionRecords" OWNER TO postgres;

--
-- Name: OrderItemProductOptions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."OrderItemProductOptions" (
    "OrderItemId" uuid NOT NULL,
    "ProductOptionId" uuid NOT NULL
);


ALTER TABLE public."OrderItemProductOptions" OWNER TO postgres;

--
-- Name: OrderItems; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."OrderItems" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "MenuItemId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "RecipeId" uuid,
    "ClientLineId" character varying(500),
    "MenuItemCodeSnapshot" character varying(500) NOT NULL,
    "MenuItemNameSnapshot" character varying(500) NOT NULL,
    "ProductCodeSnapshot" character varying(500) NOT NULL,
    "ProductNameSnapshot" character varying(500) NOT NULL,
    "ProductVariantCodeSnapshot" character varying(500) NOT NULL,
    "ProductVariantNameSnapshot" character varying(500) NOT NULL,
    "RecipeVersionSnapshot" integer,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,4) NOT NULL,
    "DiscountAmount" numeric(18,4) NOT NULL,
    "TotalAmount" numeric(18,4) NOT NULL,
    "Status" integer NOT NULL,
    "OptionsSchemaVersion" integer NOT NULL,
    "OptionsJson" jsonb,
    "RecipeSnapshotSchemaVersion" integer NOT NULL,
    "RecipeSnapshotJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."OrderItems" OWNER TO postgres;

--
-- Name: OrderStatusHistories; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."OrderStatusHistories" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "ChangedByAccountId" uuid,
    "FromStatus" integer,
    "ToStatus" integer NOT NULL,
    "Reason" character varying(500),
    "ChangedAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."OrderStatusHistories" OWNER TO postgres;

--
-- Name: Orders; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Orders" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid,
    "KioskId" uuid NOT NULL,
    "StoreId" uuid,
    "OrderNumber" character varying(500) NOT NULL,
    "IdempotencyKey" character varying(500),
    "ClientOrderId" character varying(500),
    "CorrelationId" uuid,
    "RuntimeSnapshotId" uuid,
    "RuntimeSnapshotGeneratedAt" timestamp with time zone,
    "Channel" integer NOT NULL,
    "ExternalChannel" character varying(500),
    "Status" integer NOT NULL,
    "PaymentStatus" integer NOT NULL,
    "Currency" character varying(500) NOT NULL,
    "SubtotalAmount" numeric(18,4) NOT NULL,
    "DiscountAmount" numeric(18,4) NOT NULL,
    "TaxAmount" numeric(18,4) NOT NULL,
    "TotalAmount" numeric(18,4) NOT NULL,
    "PaidAmount" numeric(18,4) NOT NULL,
    "CustomerName" character varying(500),
    "CustomerPhoneNumber" character varying(500),
    "PlacedAt" timestamp with time zone NOT NULL,
    "PaidAt" timestamp with time zone,
    "CompletedAt" timestamp with time zone,
    "CancelledAt" timestamp with time zone,
    "Notes" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Orders" OWNER TO postgres;

--
-- Name: Organizations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Organizations" (
    "Id" uuid NOT NULL,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "LegalName" character varying(500),
    "TaxCode" character varying(500),
    "Email" character varying(500),
    "PhoneNumber" character varying(500),
    "Address" character varying(500),
    "Status" integer NOT NULL,
    "MetadataJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Organizations" OWNER TO postgres;

--
-- Name: PasswordResetRequests; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."PasswordResetRequests" (
    "Id" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "TokenHash" character varying(128) NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "UsedAt" timestamp with time zone,
    "RequestedByIp" character varying(500),
    "RequestedByUserAgent" character varying(500),
    "UsedByIp" character varying(500),
    "UsedByUserAgent" character varying(500)
);


ALTER TABLE public."PasswordResetRequests" OWNER TO postgres;

--
-- Name: PaymentCallbacks; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."PaymentCallbacks" (
    "Id" uuid NOT NULL,
    "PaymentTransactionId" uuid NOT NULL,
    "Provider" character varying(500) NOT NULL,
    "EventType" character varying(500) NOT NULL,
    "ProviderEventId" character varying(500),
    "PayloadJson" jsonb NOT NULL,
    "Signature" character varying(500),
    "ProcessingStatus" integer NOT NULL,
    "ProcessingAttempts" integer NOT NULL,
    "MaxProcessingAttempts" integer NOT NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "LastAttemptAt" timestamp with time zone,
    "NextRetryAt" timestamp with time zone,
    "ProcessedAt" timestamp with time zone,
    "LastError" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."PaymentCallbacks" OWNER TO postgres;

--
-- Name: PaymentMethods; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."PaymentMethods" (
    "Id" bigint NOT NULL,
    "Provider" character varying(500) NOT NULL,
    "MethodType" character varying(500) NOT NULL,
    "IsOnline" boolean NOT NULL,
    "ConfigSchemaVersion" integer NOT NULL,
    "ConfigJson" jsonb,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."PaymentMethods" OWNER TO postgres;

--
-- Name: PaymentMethods_Id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."PaymentMethods" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PaymentMethods_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: PaymentTransactions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."PaymentTransactions" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "PaymentMethodId" bigint NOT NULL,
    "TransactionNumber" character varying(500) NOT NULL,
    "IdempotencyKey" character varying(500),
    "CorrelationId" uuid,
    "Provider" character varying(500) NOT NULL,
    "ProviderTransactionId" character varying(500),
    "PaymentIntentId" character varying(500),
    "ProviderOrderCode" character varying(100),
    "ProviderPaymentLinkId" character varying(200),
    "CheckoutUrl" character varying(2048),
    "QrCodePayload" character varying(2048),
    "ExpiresAt" timestamp with time zone,
    "Amount" numeric(18,4) NOT NULL,
    "PaidAmount" numeric(18,4),
    "Currency" character varying(500) NOT NULL,
    "ProviderStatus" character varying(100),
    "ProviderPaidAt" timestamp with time zone,
    "Status" integer NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "AuthorizedAt" timestamp with time zone,
    "PaidAt" timestamp with time zone,
    "FailedAt" timestamp with time zone,
    "CancelledAt" timestamp with time zone,
    "RetryCount" integer NOT NULL,
    "MaxRetries" integer NOT NULL,
    "NextRetryAt" timestamp with time zone,
    "LastAttemptAt" timestamp with time zone,
    "LastErrorCode" character varying(500),
    "LastErrorMessage" character varying(500),
    "FailureCode" character varying(500),
    "FailureMessage" character varying(500),
    "RawRequestJson" jsonb,
    "RawResponseJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."PaymentTransactions" OWNER TO postgres;

--
-- Name: ProductCategories; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ProductCategories" (
    "Id" bigint NOT NULL,
    "ParentCategoryId" bigint,
    "ProductType" character varying(500) NOT NULL,
    "ImageUrl" character varying(500),
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."ProductCategories" OWNER TO postgres;

--
-- Name: ProductCategories_Id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."ProductCategories" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."ProductCategories_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: ProductOptions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ProductOptions" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid,
    "OptionGroupId" bigint NOT NULL,
    "TemplateProductOptionId" uuid,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "Description" character varying(500),
    "PriceDelta" numeric(18,4) NOT NULL,
    "Currency" character varying(500) NOT NULL,
    "IsDefault" boolean NOT NULL,
    "IsAvailable" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "MetadataJson" jsonb,
    "ScopeType" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."ProductOptions" OWNER TO postgres;

--
-- Name: ProductProductOptions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ProductProductOptions" (
    "ProductId" uuid NOT NULL,
    "ProductOptionId" uuid NOT NULL
);


ALTER TABLE public."ProductProductOptions" OWNER TO postgres;

--
-- Name: ProductVariants; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ProductVariants" (
    "Id" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "DisplayName" character varying(500),
    "Description" character varying(500),
    "VariantType" character varying(500) NOT NULL,
    "SizeCode" character varying(500),
    "BasePrice" numeric(18,4) NOT NULL,
    "Currency" character varying(500) NOT NULL,
    "IsAvailable" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "PreparationTimeSeconds" integer,
    "ImageUrl" character varying(500),
    "MetadataJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid,
    "FulfillmentType" integer DEFAULT 2 NOT NULL
);


ALTER TABLE public."ProductVariants" OWNER TO postgres;

--
-- Name: ProductionEventCheckpoints; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ProductionEventCheckpoints" (
    "Id" uuid NOT NULL,
    "KioskId" uuid NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "SourceExecutorId" uuid NOT NULL,
    "LastContiguousSequenceNumber" bigint NOT NULL,
    "LastContiguousEventId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."ProductionEventCheckpoints" OWNER TO postgres;

--
-- Name: ProductionExecutionRecords; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ProductionExecutionRecords" (
    "Id" uuid NOT NULL,
    "SourceCommandId" uuid NOT NULL,
    "KioskExecutionEndpointId" uuid NOT NULL,
    "ExecutionProfile" integer NOT NULL,
    "SourceProductionJobId" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "WorkcellId" uuid,
    "ControllerId" uuid,
    "ExecutionPlanChecksum" character varying(500),
    "ActiveSetVersion" bigint,
    "ActiveSetChecksum" character varying(500),
    "Status" integer NOT NULL,
    "PhysicalOutputState" integer NOT NULL,
    "ErrorCode" character varying(500),
    "ErrorMessage" character varying(500),
    "SourceExecutorId" uuid NOT NULL,
    "LastAppliedSourceEventId" uuid NOT NULL,
    "LastAppliedSequenceNumber" bigint NOT NULL,
    "LastEdgeCreatedAt" timestamp with time zone NOT NULL,
    "LastExecutorReportedAt" timestamp with time zone NOT NULL,
    "CloudReceivedAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."ProductionExecutionRecords" OWNER TO postgres;

--
-- Name: Products; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Products" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid,
    "StoreId" uuid,
    "KioskId" uuid,
    "TemplateProductId" uuid,
    "CategoryId" bigint,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "DisplayName" character varying(500),
    "Description" character varying(500),
    "ProductType" character varying(500) NOT NULL,
    "BasePrice" numeric(18,4) NOT NULL,
    "Currency" character varying(500) NOT NULL,
    "IsAvailable" boolean NOT NULL,
    "PreparationTimeSeconds" integer,
    "ImageUrl" character varying(500),
    "MetadataJson" jsonb,
    "ScopeType" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Products" OWNER TO postgres;

--
-- Name: RecipeItems; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RecipeItems" (
    "Id" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "IngredientId" uuid NOT NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "Unit" character varying(500) NOT NULL,
    "StepOrder" integer NOT NULL,
    "IsOptional" boolean NOT NULL,
    "Notes" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."RecipeItems" OWNER TO postgres;

--
-- Name: Recipes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Recipes" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid,
    "StoreId" uuid,
    "KioskId" uuid,
    "ProductVariantId" uuid NOT NULL,
    "TemplateRecipeId" uuid,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "Version" integer NOT NULL,
    "Status" integer NOT NULL,
    "IsDefault" boolean NOT NULL,
    "YieldQuantity" numeric(18,4) NOT NULL,
    "Unit" character varying(500) NOT NULL,
    "EstimatedDurationSeconds" integer,
    "EffectiveFrom" timestamp with time zone,
    "EffectiveTo" timestamp with time zone,
    "InstructionsSchemaVersion" integer NOT NULL,
    "InstructionsJson" jsonb,
    "ScopeType" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Recipes" OWNER TO postgres;

--
-- Name: RefreshTokens; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RefreshTokens" (
    "Id" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "AccountDeviceId" uuid,
    "ReplacedByTokenId" uuid,
    "TokenHash" character varying(500) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "RevokedAt" timestamp with time zone,
    "CreatedByIp" character varying(500),
    "RevokedByIp" character varying(500),
    "CreatedByUserAgent" character varying(500),
    "RevokedByUserAgent" character varying(500),
    "RevokeReason" character varying(500),
    "ReuseDetectedAt" timestamp with time zone,
    "IsUsed" boolean NOT NULL
);


ALTER TABLE public."RefreshTokens" OWNER TO postgres;

--
-- Name: Refunds; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Refunds" (
    "Id" uuid NOT NULL,
    "PaymentTransactionId" uuid NOT NULL,
    "RequestedByAccountId" uuid,
    "RefundNumber" character varying(500) NOT NULL,
    "IdempotencyKey" character varying(500),
    "CorrelationId" uuid,
    "ProviderRefundId" character varying(500),
    "Amount" numeric(18,4) NOT NULL,
    "Currency" character varying(500) NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "Status" integer NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "ProcessedAt" timestamp with time zone,
    "RejectedAt" timestamp with time zone,
    "RetryCount" integer NOT NULL,
    "MaxRetries" integer NOT NULL,
    "NextRetryAt" timestamp with time zone,
    "LastAttemptAt" timestamp with time zone,
    "LastErrorCode" character varying(500),
    "LastErrorMessage" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Refunds" OWNER TO postgres;

--
-- Name: RobotArtifactTemplates; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RobotArtifactTemplates" (
    "Id" uuid NOT NULL,
    "TemplateCode" character varying(500) NOT NULL,
    "TemplateName" character varying(500) NOT NULL,
    "StorageKey" character varying(500) NOT NULL,
    "FileName" character varying(500) NOT NULL,
    "Checksum" character varying(500) NOT NULL,
    "RuntimeTargetCode" character varying(500) NOT NULL,
    "MachineModelCode" character varying(500) NOT NULL,
    "ContentLengthBytes" bigint NOT NULL,
    "Status" integer NOT NULL,
    "ExportedAt" timestamp with time zone NOT NULL,
    "Description" character varying(500),
    "MetadataJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."RobotArtifactTemplates" OWNER TO postgres;

--
-- Name: RobotArtifacts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RobotArtifacts" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "ArtifactCode" character varying(500) NOT NULL,
    "ArtifactName" character varying(500) NOT NULL,
    "StorageKey" character varying(500) NOT NULL,
    "FileName" character varying(500) NOT NULL,
    "Checksum" character varying(500) NOT NULL,
    "RuntimeTargetCode" character varying(500) NOT NULL,
    "MachineModelCode" character varying(500) NOT NULL,
    "ContentLengthBytes" bigint NOT NULL,
    "Status" integer NOT NULL,
    "ExportedAt" timestamp with time zone NOT NULL,
    "Description" character varying(500),
    "MetadataJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid,
    "OriginNodeId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "SyncedAt" timestamp with time zone,
    "SourceRobotArtifactTemplateId" uuid
);


ALTER TABLE public."RobotArtifacts" OWNER TO postgres;

--
-- Name: RobotProgramArtifacts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RobotProgramArtifacts" (
    "Id" uuid NOT NULL,
    "RobotProgramId" uuid NOT NULL,
    "RobotArtifactId" uuid NOT NULL,
    "RunOrder" integer NOT NULL,
    "ParametersSchemaVersion" integer NOT NULL,
    "ParametersJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid,
    "OriginNodeId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "SyncedAt" timestamp with time zone
);


ALTER TABLE public."RobotProgramArtifacts" OWNER TO postgres;

--
-- Name: RobotPrograms; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RobotPrograms" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid,
    "StoreId" uuid,
    "KioskId" uuid,
    "DeviceId" uuid,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "ScopeType" integer NOT NULL,
    "Status" integer NOT NULL,
    "ProgramManifestChecksum" character varying(500),
    "Description" character varying(500),
    "ProgramManifestSchemaVersion" integer NOT NULL,
    "ProgramManifestJson" jsonb,
    "PublishedAt" timestamp with time zone,
    "RetiredAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid,
    "OriginNodeId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "SyncedAt" timestamp with time zone
);


ALTER TABLE public."RobotPrograms" OWNER TO postgres;

--
-- Name: Roles; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Roles" (
    "Id" bigint NOT NULL,
    "IsSystemRole" boolean NOT NULL,
    "Priority" integer NOT NULL,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."Roles" OWNER TO postgres;

--
-- Name: Roles_Id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Roles" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Roles_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: StockMovements; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."StockMovements" (
    "Id" uuid NOT NULL,
    "IngredientDispenserStateId" uuid NOT NULL,
    "OrganizationId" uuid,
    "KioskId" uuid,
    "StoreId" uuid,
    "DeviceId" uuid,
    "IngredientId" uuid,
    "SourceEventId" uuid,
    "CorrelationId" uuid,
    "CausationId" uuid,
    "MovementType" character varying(500) NOT NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "BalanceAfter" numeric(18,4),
    "IsEstimated" boolean NOT NULL,
    "Unit" character varying(500) NOT NULL,
    "ReasonCode" character varying(500),
    "ReferenceType" character varying(500),
    "ReferenceId" uuid,
    "Notes" character varying(500),
    "OccurredAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "OriginNodeId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "SyncedAt" timestamp with time zone
);


ALTER TABLE public."StockMovements" OWNER TO postgres;

--
-- Name: Stores; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Stores" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "Code" character varying(500) NOT NULL,
    "Name" character varying(500) NOT NULL,
    "StoreType" character varying(500) NOT NULL,
    "Status" integer NOT NULL,
    "Address" character varying(500),
    "City" character varying(500),
    "Province" character varying(500),
    "Country" character varying(500),
    "TimeZone" character varying(500) NOT NULL,
    "Latitude" numeric(18,4),
    "Longitude" numeric(18,4),
    "PhoneNumber" character varying(500),
    "Email" character varying(500),
    "OpeningHoursSchemaVersion" integer NOT NULL,
    "OpeningHoursJson" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedByAccountId" uuid
);


ALTER TABLE public."Stores" OWNER TO postgres;

--
-- Name: SyncDeadLetterRetryAttempts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."SyncDeadLetterRetryAttempts" (
    "Id" uuid NOT NULL,
    "SyncDeadLetterId" uuid NOT NULL,
    "AttemptNumber" integer NOT NULL,
    "RequestedByAccountId" uuid NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    "CompletedAt" timestamp with time zone,
    "Succeeded" boolean,
    "ResultMessage" character varying(1000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedByAccountId" uuid,
    "UpdatedByAccountId" uuid
);


ALTER TABLE public."SyncDeadLetterRetryAttempts" OWNER TO postgres;

--
-- Name: SyncDeadLetters; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."SyncDeadLetters" (
    "Id" uuid NOT NULL,
    "SyncEventInboxId" uuid,
    "EventId" uuid,
    "KioskId" uuid,
    "SourceNodeId" uuid,
    "CorrelationId" uuid,
    "CausationId" uuid,
    "ResolvedByAccountId" uuid,
    "EventType" character varying(500) NOT NULL,
    "AggregateType" character varying(500),
    "AggregateId" uuid,
    "PayloadJson" jsonb NOT NULL,
    "ErrorMessage" character varying(500) NOT NULL,
    "ErrorDetails" character varying(500),
    "Status" integer NOT NULL,
    "ProcessingAttempts" integer NOT NULL,
    "FailedAt" timestamp with time zone NOT NULL,
    "ResolvedAt" timestamp with time zone,
    "ResolutionNotes" character varying(500)
);


ALTER TABLE public."SyncDeadLetters" OWNER TO postgres;

--
-- Name: SyncEventInbox; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."SyncEventInbox" (
    "Id" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "KioskId" uuid,
    "SourceNodeId" uuid NOT NULL,
    "CorrelationId" uuid,
    "CausationId" uuid,
    "EventType" character varying(500) NOT NULL,
    "AggregateType" character varying(500),
    "AggregateId" uuid,
    "PayloadJson" jsonb NOT NULL,
    "HeadersJson" jsonb,
    "Status" integer NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "ProcessedAt" timestamp with time zone,
    "LastAttemptAt" timestamp with time zone,
    "NextRetryAt" timestamp with time zone,
    "LockId" uuid,
    "LockedUntil" timestamp with time zone,
    "ProcessingAttempts" integer NOT NULL,
    "MaxProcessingAttempts" integer NOT NULL,
    "LastError" character varying(500),
    "SequenceNumber" bigint
);


ALTER TABLE public."SyncEventInbox" OWNER TO postgres;

--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


ALTER TABLE public."__EFMigrationsHistory" OWNER TO postgres;

--
-- Name: ConfigurationReleases AK_ConfigurationReleases_Id_OrganizationId; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ConfigurationReleases"
    ADD CONSTRAINT "AK_ConfigurationReleases_Id_OrganizationId" UNIQUE ("Id", "OrganizationId");


--
-- Name: Devices AK_Devices_Id_KioskId; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Devices"
    ADD CONSTRAINT "AK_Devices_Id_KioskId" UNIQUE ("Id", "KioskId");


--
-- Name: EdgeCommands AK_EdgeCommands_Id_TargetExecutionEndpointId; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EdgeCommands"
    ADD CONSTRAINT "AK_EdgeCommands_Id_TargetExecutionEndpointId" UNIQUE ("Id", "TargetExecutionEndpointId");


--
-- Name: KioskExecutionEndpoints AK_KioskExecutionEndpoints_Id_KioskId; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskExecutionEndpoints"
    ADD CONSTRAINT "AK_KioskExecutionEndpoints_Id_KioskId" UNIQUE ("Id", "KioskId");


--
-- Name: Kiosks AK_Kiosks_Id_OrganizationId; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Kiosks"
    ADD CONSTRAINT "AK_Kiosks_Id_OrganizationId" UNIQUE ("Id", "OrganizationId");


--
-- Name: AccountDevices PK_AccountDevices; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountDevices"
    ADD CONSTRAINT "PK_AccountDevices" PRIMARY KEY ("Id");


--
-- Name: AccountInvitations PK_AccountInvitations; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountInvitations"
    ADD CONSTRAINT "PK_AccountInvitations" PRIMARY KEY ("Id");


--
-- Name: AccountRoles PK_AccountRoles; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountRoles"
    ADD CONSTRAINT "PK_AccountRoles" PRIMARY KEY ("Id");


--
-- Name: AccountStores PK_AccountStores; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountStores"
    ADD CONSTRAINT "PK_AccountStores" PRIMARY KEY ("AccountId", "StoreId");


--
-- Name: Accounts PK_Accounts; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Accounts"
    ADD CONSTRAINT "PK_Accounts" PRIMARY KEY ("Id");


--
-- Name: Alerts PK_Alerts; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Alerts"
    ADD CONSTRAINT "PK_Alerts" PRIMARY KEY ("Id");


--
-- Name: ConfigurationReleases PK_ConfigurationReleases; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ConfigurationReleases"
    ADD CONSTRAINT "PK_ConfigurationReleases" PRIMARY KEY ("Id");


--
-- Name: ControllerArtifactSetDeployments PK_ControllerArtifactSetDeployments; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ControllerArtifactSetDeployments"
    ADD CONSTRAINT "PK_ControllerArtifactSetDeployments" PRIMARY KEY ("Id");


--
-- Name: ControllerArtifactSetItems PK_ControllerArtifactSetItems; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ControllerArtifactSetItems"
    ADD CONSTRAINT "PK_ControllerArtifactSetItems" PRIMARY KEY ("Id");


--
-- Name: DeviceEvents PK_DeviceEvents; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DeviceEvents"
    ADD CONSTRAINT "PK_DeviceEvents" PRIMARY KEY ("Id");


--
-- Name: DeviceModels PK_DeviceModels; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DeviceModels"
    ADD CONSTRAINT "PK_DeviceModels" PRIMARY KEY ("Id");


--
-- Name: DeviceTypes PK_DeviceTypes; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DeviceTypes"
    ADD CONSTRAINT "PK_DeviceTypes" PRIMARY KEY ("Id");


--
-- Name: Devices PK_Devices; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Devices"
    ADD CONSTRAINT "PK_Devices" PRIMARY KEY ("Id");


--
-- Name: EdgeCommandDeliveryAttempts PK_EdgeCommandDeliveryAttempts; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EdgeCommandDeliveryAttempts"
    ADD CONSTRAINT "PK_EdgeCommandDeliveryAttempts" PRIMARY KEY ("Id");


--
-- Name: EdgeCommands PK_EdgeCommands; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EdgeCommands"
    ADD CONSTRAINT "PK_EdgeCommands" PRIMARY KEY ("Id");


--
-- Name: EdgeStateSummaries PK_EdgeStateSummaries; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EdgeStateSummaries"
    ADD CONSTRAINT "PK_EdgeStateSummaries" PRIMARY KEY ("Id");


--
-- Name: ExecutionEndpointCapabilityProjections PK_ExecutionEndpointCapabilityProjections; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointCapabilityProjections"
    ADD CONSTRAINT "PK_ExecutionEndpointCapabilityProjections" PRIMARY KEY ("Id");


--
-- Name: ExecutionEndpointCredentialBindings PK_ExecutionEndpointCredentialBindings; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointCredentialBindings"
    ADD CONSTRAINT "PK_ExecutionEndpointCredentialBindings" PRIMARY KEY ("Id");


--
-- Name: ExecutionEndpointMqttCredentials PK_ExecutionEndpointMqttCredentials; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointMqttCredentials"
    ADD CONSTRAINT "PK_ExecutionEndpointMqttCredentials" PRIMARY KEY ("Id");


--
-- Name: ExecutionEndpointReadinessProjections PK_ExecutionEndpointReadinessProjections; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointReadinessProjections"
    ADD CONSTRAINT "PK_ExecutionEndpointReadinessProjections" PRIMARY KEY ("Id");


--
-- Name: ExecutionEndpointRequestNonces PK_ExecutionEndpointRequestNonces; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointRequestNonces"
    ADD CONSTRAINT "PK_ExecutionEndpointRequestNonces" PRIMARY KEY ("Id");


--
-- Name: ExecutionEndpointSupportedRobotTargets PK_ExecutionEndpointSupportedRobotTargets; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointSupportedRobotTargets"
    ADD CONSTRAINT "PK_ExecutionEndpointSupportedRobotTargets" PRIMARY KEY ("Id");


--
-- Name: ExecutionRouteRobotBindings PK_ExecutionRouteRobotBindings; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionRouteRobotBindings"
    ADD CONSTRAINT "PK_ExecutionRouteRobotBindings" PRIMARY KEY ("Id");


--
-- Name: ExecutionRoutes PK_ExecutionRoutes; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionRoutes"
    ADD CONSTRAINT "PK_ExecutionRoutes" PRIMARY KEY ("Id");


--
-- Name: IngredientDispenserStates PK_IngredientDispenserStates; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."IngredientDispenserStates"
    ADD CONSTRAINT "PK_IngredientDispenserStates" PRIMARY KEY ("Id");


--
-- Name: Ingredients PK_Ingredients; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Ingredients"
    ADD CONSTRAINT "PK_Ingredients" PRIMARY KEY ("Id");


--
-- Name: KioskConfigurationDeployments PK_KioskConfigurationDeployments; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskConfigurationDeployments"
    ADD CONSTRAINT "PK_KioskConfigurationDeployments" PRIMARY KEY ("Id");


--
-- Name: KioskExecutionEndpoints PK_KioskExecutionEndpoints; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskExecutionEndpoints"
    ADD CONSTRAINT "PK_KioskExecutionEndpoints" PRIMARY KEY ("Id");


--
-- Name: KioskHeartbeats PK_KioskHeartbeats; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskHeartbeats"
    ADD CONSTRAINT "PK_KioskHeartbeats" PRIMARY KEY ("Id");


--
-- Name: Kiosks PK_Kiosks; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Kiosks"
    ADD CONSTRAINT "PK_Kiosks" PRIMARY KEY ("Id");


--
-- Name: MaintenanceTickets PK_MaintenanceTickets; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MaintenanceTickets"
    ADD CONSTRAINT "PK_MaintenanceTickets" PRIMARY KEY ("Id");


--
-- Name: MenuItems PK_MenuItems; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MenuItems"
    ADD CONSTRAINT "PK_MenuItems" PRIMARY KEY ("Id");


--
-- Name: Menus PK_Menus; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Menus"
    ADD CONSTRAINT "PK_Menus" PRIMARY KEY ("Id");


--
-- Name: OperationLogs PK_OperationLogs; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OperationLogs"
    ADD CONSTRAINT "PK_OperationLogs" PRIMARY KEY ("Id");


--
-- Name: OptionGroups PK_OptionGroups; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OptionGroups"
    ADD CONSTRAINT "PK_OptionGroups" PRIMARY KEY ("Id");


--
-- Name: OrderExecutionRecords PK_OrderExecutionRecords; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderExecutionRecords"
    ADD CONSTRAINT "PK_OrderExecutionRecords" PRIMARY KEY ("Id");


--
-- Name: OrderItemProductOptions PK_OrderItemProductOptions; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderItemProductOptions"
    ADD CONSTRAINT "PK_OrderItemProductOptions" PRIMARY KEY ("OrderItemId", "ProductOptionId");


--
-- Name: OrderItems PK_OrderItems; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderItems"
    ADD CONSTRAINT "PK_OrderItems" PRIMARY KEY ("Id");


--
-- Name: OrderStatusHistories PK_OrderStatusHistories; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderStatusHistories"
    ADD CONSTRAINT "PK_OrderStatusHistories" PRIMARY KEY ("Id");


--
-- Name: Orders PK_Orders; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Orders"
    ADD CONSTRAINT "PK_Orders" PRIMARY KEY ("Id");


--
-- Name: Organizations PK_Organizations; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Organizations"
    ADD CONSTRAINT "PK_Organizations" PRIMARY KEY ("Id");


--
-- Name: PasswordResetRequests PK_PasswordResetRequests; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PasswordResetRequests"
    ADD CONSTRAINT "PK_PasswordResetRequests" PRIMARY KEY ("Id");


--
-- Name: PaymentCallbacks PK_PaymentCallbacks; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PaymentCallbacks"
    ADD CONSTRAINT "PK_PaymentCallbacks" PRIMARY KEY ("Id");


--
-- Name: PaymentMethods PK_PaymentMethods; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PaymentMethods"
    ADD CONSTRAINT "PK_PaymentMethods" PRIMARY KEY ("Id");


--
-- Name: PaymentTransactions PK_PaymentTransactions; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PaymentTransactions"
    ADD CONSTRAINT "PK_PaymentTransactions" PRIMARY KEY ("Id");


--
-- Name: ProductCategories PK_ProductCategories; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductCategories"
    ADD CONSTRAINT "PK_ProductCategories" PRIMARY KEY ("Id");


--
-- Name: ProductOptions PK_ProductOptions; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductOptions"
    ADD CONSTRAINT "PK_ProductOptions" PRIMARY KEY ("Id");


--
-- Name: ProductProductOptions PK_ProductProductOptions; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductProductOptions"
    ADD CONSTRAINT "PK_ProductProductOptions" PRIMARY KEY ("ProductId", "ProductOptionId");


--
-- Name: ProductVariants PK_ProductVariants; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductVariants"
    ADD CONSTRAINT "PK_ProductVariants" PRIMARY KEY ("Id");


--
-- Name: ProductionEventCheckpoints PK_ProductionEventCheckpoints; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductionEventCheckpoints"
    ADD CONSTRAINT "PK_ProductionEventCheckpoints" PRIMARY KEY ("Id");


--
-- Name: ProductionExecutionRecords PK_ProductionExecutionRecords; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductionExecutionRecords"
    ADD CONSTRAINT "PK_ProductionExecutionRecords" PRIMARY KEY ("Id");


--
-- Name: Products PK_Products; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Products"
    ADD CONSTRAINT "PK_Products" PRIMARY KEY ("Id");


--
-- Name: RecipeItems PK_RecipeItems; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RecipeItems"
    ADD CONSTRAINT "PK_RecipeItems" PRIMARY KEY ("Id");


--
-- Name: Recipes PK_Recipes; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Recipes"
    ADD CONSTRAINT "PK_Recipes" PRIMARY KEY ("Id");


--
-- Name: RefreshTokens PK_RefreshTokens; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id");


--
-- Name: Refunds PK_Refunds; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Refunds"
    ADD CONSTRAINT "PK_Refunds" PRIMARY KEY ("Id");


--
-- Name: RobotArtifactTemplates PK_RobotArtifactTemplates; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotArtifactTemplates"
    ADD CONSTRAINT "PK_RobotArtifactTemplates" PRIMARY KEY ("Id");


--
-- Name: RobotArtifacts PK_RobotArtifacts; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotArtifacts"
    ADD CONSTRAINT "PK_RobotArtifacts" PRIMARY KEY ("Id");


--
-- Name: RobotProgramArtifacts PK_RobotProgramArtifacts; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotProgramArtifacts"
    ADD CONSTRAINT "PK_RobotProgramArtifacts" PRIMARY KEY ("Id");


--
-- Name: RobotPrograms PK_RobotPrograms; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotPrograms"
    ADD CONSTRAINT "PK_RobotPrograms" PRIMARY KEY ("Id");


--
-- Name: Roles PK_Roles; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Roles"
    ADD CONSTRAINT "PK_Roles" PRIMARY KEY ("Id");


--
-- Name: StockMovements PK_StockMovements; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "PK_StockMovements" PRIMARY KEY ("Id");


--
-- Name: Stores PK_Stores; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Stores"
    ADD CONSTRAINT "PK_Stores" PRIMARY KEY ("Id");


--
-- Name: SyncDeadLetterRetryAttempts PK_SyncDeadLetterRetryAttempts; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyncDeadLetterRetryAttempts"
    ADD CONSTRAINT "PK_SyncDeadLetterRetryAttempts" PRIMARY KEY ("Id");


--
-- Name: SyncDeadLetters PK_SyncDeadLetters; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyncDeadLetters"
    ADD CONSTRAINT "PK_SyncDeadLetters" PRIMARY KEY ("Id");


--
-- Name: SyncEventInbox PK_SyncEventInbox; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyncEventInbox"
    ADD CONSTRAINT "PK_SyncEventInbox" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: IX_AccountDevices_AccountId_DeviceTokenHash; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_AccountDevices_AccountId_DeviceTokenHash" ON public."AccountDevices" USING btree ("AccountId", "DeviceTokenHash");


--
-- Name: IX_AccountInvitations_AccountId_InvitedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_AccountInvitations_AccountId_InvitedAt" ON public."AccountInvitations" USING btree ("AccountId", "InvitedAt");


--
-- Name: IX_AccountInvitations_TokenHash; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_AccountInvitations_TokenHash" ON public."AccountInvitations" USING btree ("TokenHash");


--
-- Name: IX_AccountRoles_AccountId_RoleId_OrganizationId_StoreId_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_AccountRoles_AccountId_RoleId_OrganizationId_StoreId_KioskId" ON public."AccountRoles" USING btree ("AccountId", "RoleId", "OrganizationId", "StoreId", "KioskId");


--
-- Name: IX_AccountRoles_AssignedByAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_AccountRoles_AssignedByAccountId" ON public."AccountRoles" USING btree ("AssignedByAccountId");


--
-- Name: IX_AccountRoles_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_AccountRoles_KioskId" ON public."AccountRoles" USING btree ("KioskId");


--
-- Name: IX_AccountRoles_OrganizationId_StoreId_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_AccountRoles_OrganizationId_StoreId_KioskId" ON public."AccountRoles" USING btree ("OrganizationId", "StoreId", "KioskId");


--
-- Name: IX_AccountRoles_RoleId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_AccountRoles_RoleId" ON public."AccountRoles" USING btree ("RoleId");


--
-- Name: IX_AccountRoles_StoreId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_AccountRoles_StoreId" ON public."AccountRoles" USING btree ("StoreId");


--
-- Name: IX_AccountStores_StoreId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_AccountStores_StoreId" ON public."AccountStores" USING btree ("StoreId");


--
-- Name: IX_Accounts_Email; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Accounts_Email" ON public."Accounts" USING btree ("Email") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_Accounts_GoogleEmail; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Accounts_GoogleEmail" ON public."Accounts" USING btree ("GoogleEmail") WHERE ("GoogleEmail" IS NOT NULL);


--
-- Name: IX_Accounts_GoogleSubjectId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Accounts_GoogleSubjectId" ON public."Accounts" USING btree ("GoogleSubjectId") WHERE (("GoogleSubjectId" IS NOT NULL) AND ("DeletedAt" IS NULL));


--
-- Name: IX_Accounts_UserName; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Accounts_UserName" ON public."Accounts" USING btree ("UserName") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_Alerts_AcknowledgedByAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Alerts_AcknowledgedByAccountId" ON public."Alerts" USING btree ("AcknowledgedByAccountId");


--
-- Name: IX_Alerts_DeviceId_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Alerts_DeviceId_KioskId" ON public."Alerts" USING btree ("DeviceId", "KioskId");


--
-- Name: IX_Alerts_KioskId_Status_RaisedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Alerts_KioskId_Status_RaisedAt" ON public."Alerts" USING btree ("KioskId", "Status", "RaisedAt");


--
-- Name: IX_Alerts_OriginNodeId_Version; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Alerts_OriginNodeId_Version" ON public."Alerts" USING btree ("OriginNodeId", "Version");


--
-- Name: IX_ConfigurationReleases_Id_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ConfigurationReleases_Id_OrganizationId" ON public."ConfigurationReleases" USING btree ("Id", "OrganizationId");


--
-- Name: IX_ConfigurationReleases_OrganizationId_ReleaseNumber; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ConfigurationReleases_OrganizationId_ReleaseNumber" ON public."ConfigurationReleases" USING btree ("OrganizationId", "ReleaseNumber") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_ConfigurationReleases_ReleaseChecksum; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ConfigurationReleases_ReleaseChecksum" ON public."ConfigurationReleases" USING btree ("ReleaseChecksum") WHERE ("ReleaseChecksum" IS NOT NULL);


--
-- Name: IX_ConfigurationReleases_Status_PublishedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ConfigurationReleases_Status_PublishedAt" ON public."ConfigurationReleases" USING btree ("Status", "PublishedAt");


--
-- Name: IX_ControllerArtifactSetDeployments_ControllerId_ActiveSetVers~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ControllerArtifactSetDeployments_ControllerId_ActiveSetVers~" ON public."ControllerArtifactSetDeployments" USING btree ("ControllerId", "ActiveSetVersion");


--
-- Name: IX_ControllerArtifactSetDeployments_ControllerId_LastControlle~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ControllerArtifactSetDeployments_ControllerId_LastControlle~" ON public."ControllerArtifactSetDeployments" USING btree ("ControllerId", "LastControllerReportId") WHERE ("LastControllerReportId" IS NOT NULL);


--
-- Name: IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_I~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_I~" ON public."ControllerArtifactSetDeployments" USING btree ("KioskExecutionEndpointId", "IdempotencyKey");


--
-- Name: IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_K~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_K~" ON public."ControllerArtifactSetDeployments" USING btree ("KioskExecutionEndpointId", "KioskId");


--
-- Name: IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_S~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_S~" ON public."ControllerArtifactSetDeployments" USING btree ("KioskExecutionEndpointId", "Status", "RequestedAt");


--
-- Name: IX_ControllerArtifactSetDeployments_KioskId_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ControllerArtifactSetDeployments_KioskId_OrganizationId" ON public."ControllerArtifactSetDeployments" USING btree ("KioskId", "OrganizationId");


--
-- Name: IX_ControllerArtifactSetDeployments_SourceConfigurationRelease~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ControllerArtifactSetDeployments_SourceConfigurationRelease~" ON public."ControllerArtifactSetDeployments" USING btree ("SourceConfigurationReleaseId", "OrganizationId");


--
-- Name: IX_ControllerArtifactSetItems_ControllerArtifactSetDeploymentI~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ControllerArtifactSetItems_ControllerArtifactSetDeploymentI~" ON public."ControllerArtifactSetItems" USING btree ("ControllerArtifactSetDeploymentId", "ExecutionRouteId", "RobotProgramId", "RunOrder", "RobotArtifactId");


--
-- Name: IX_DeviceEvents_DeviceId_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_DeviceEvents_DeviceId_KioskId" ON public."DeviceEvents" USING btree ("DeviceId", "KioskId");


--
-- Name: IX_DeviceEvents_DeviceId_OccurredAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_DeviceEvents_DeviceId_OccurredAt" ON public."DeviceEvents" USING btree ("DeviceId", "OccurredAt");


--
-- Name: IX_DeviceEvents_EventId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_DeviceEvents_EventId" ON public."DeviceEvents" USING btree ("EventId");


--
-- Name: IX_DeviceEvents_KioskId_OccurredAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_DeviceEvents_KioskId_OccurredAt" ON public."DeviceEvents" USING btree ("KioskId", "OccurredAt");


--
-- Name: IX_DeviceEvents_OriginNodeId_Version; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_DeviceEvents_OriginNodeId_Version" ON public."DeviceEvents" USING btree ("OriginNodeId", "Version");


--
-- Name: IX_DeviceModels_DeviceTypeId_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_DeviceModels_DeviceTypeId_Code" ON public."DeviceModels" USING btree ("DeviceTypeId", "Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_DeviceTypes_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_DeviceTypes_Code" ON public."DeviceTypes" USING btree ("Code");


--
-- Name: IX_Devices_DeviceModelId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Devices_DeviceModelId" ON public."Devices" USING btree ("DeviceModelId");


--
-- Name: IX_Devices_DeviceTypeId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Devices_DeviceTypeId" ON public."Devices" USING btree ("DeviceTypeId");


--
-- Name: IX_Devices_Id_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Devices_Id_KioskId" ON public."Devices" USING btree ("Id", "KioskId");


--
-- Name: IX_Devices_KioskId_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Devices_KioskId_Code" ON public."Devices" USING btree ("KioskId", "Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_Devices_SerialNumber; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Devices_SerialNumber" ON public."Devices" USING btree ("SerialNumber") WHERE (("SerialNumber" IS NOT NULL) AND ("DeletedAt" IS NULL));


--
-- Name: IX_EdgeCommandDeliveryAttempts_EdgeCommandId_DeliveryAttemptNo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_EdgeCommandDeliveryAttempts_EdgeCommandId_DeliveryAttemptNo" ON public."EdgeCommandDeliveryAttempts" USING btree ("EdgeCommandId", "DeliveryAttemptNo");


--
-- Name: IX_EdgeCommands_DeploymentId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_EdgeCommands_DeploymentId" ON public."EdgeCommands" USING btree ("DeploymentId") WHERE ("DeploymentId" IS NOT NULL);


--
-- Name: IX_EdgeCommands_OrderId_DispatchAttemptNo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_EdgeCommands_OrderId_DispatchAttemptNo" ON public."EdgeCommands" USING btree ("OrderId", "DispatchAttemptNo") WHERE (("OrderId" IS NOT NULL) AND ("DispatchAttemptNo" IS NOT NULL));


--
-- Name: IX_EdgeCommands_TargetExecutionEndpointId_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EdgeCommands_TargetExecutionEndpointId_KioskId" ON public."EdgeCommands" USING btree ("TargetExecutionEndpointId", "KioskId");


--
-- Name: IX_EdgeCommands_TargetExecutionEndpointId_Status_CreatedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EdgeCommands_TargetExecutionEndpointId_Status_CreatedAt" ON public."EdgeCommands" USING btree ("TargetExecutionEndpointId", "Status", "CreatedAt");


--
-- Name: IX_EdgeStateSummaries_KioskExecutionEndpointId_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EdgeStateSummaries_KioskExecutionEndpointId_KioskId" ON public."EdgeStateSummaries" USING btree ("KioskExecutionEndpointId", "KioskId");


--
-- Name: IX_EdgeStateSummaries_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EdgeStateSummaries_KioskId" ON public."EdgeStateSummaries" USING btree ("KioskId");


--
-- Name: IX_EdgeStateSummaries_SourceExecutorId_SummaryKind; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_EdgeStateSummaries_SourceExecutorId_SummaryKind" ON public."EdgeStateSummaries" USING btree ("SourceExecutorId", "SummaryKind");


--
-- Name: IX_ExecutionEndpointCapabilityProjections_ExecutionEndpointRea~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionEndpointCapabilityProjections_ExecutionEndpointRea~" ON public."ExecutionEndpointCapabilityProjections" USING btree ("ExecutionEndpointReadinessProjectionId", "CapabilityCode");


--
-- Name: IX_ExecutionEndpointCredentialBindings_CredentialReference; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionEndpointCredentialBindings_CredentialReference" ON public."ExecutionEndpointCredentialBindings" USING btree ("CredentialReference");


--
-- Name: IX_ExecutionEndpointCredentialBindings_KioskExecutionEndpointI~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ExecutionEndpointCredentialBindings_KioskExecutionEndpointI~" ON public."ExecutionEndpointCredentialBindings" USING btree ("KioskExecutionEndpointId", "Status");


--
-- Name: IX_ExecutionEndpointMqttCredentials_KioskExecutionEndpointId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionEndpointMqttCredentials_KioskExecutionEndpointId" ON public."ExecutionEndpointMqttCredentials" USING btree ("KioskExecutionEndpointId");


--
-- Name: IX_ExecutionEndpointMqttCredentials_Username; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionEndpointMqttCredentials_Username" ON public."ExecutionEndpointMqttCredentials" USING btree ("Username");


--
-- Name: IX_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~" ON public."ExecutionEndpointReadinessProjections" USING btree ("KioskExecutionEndpointId");


--
-- Name: IX_ExecutionEndpointReadinessProjections_KioskExecutionEndpoi~1; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionEndpointReadinessProjections_KioskExecutionEndpoi~1" ON public."ExecutionEndpointReadinessProjections" USING btree ("KioskExecutionEndpointId", "KioskId");


--
-- Name: IX_ExecutionEndpointReadinessProjections_KioskId_Readiness_Act~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ExecutionEndpointReadinessProjections_KioskId_Readiness_Act~" ON public."ExecutionEndpointReadinessProjections" USING btree ("KioskId", "Readiness", "Activity");


--
-- Name: IX_ExecutionEndpointRequestNonces_ExpiresAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ExecutionEndpointRequestNonces_ExpiresAt" ON public."ExecutionEndpointRequestNonces" USING btree ("ExpiresAt");


--
-- Name: IX_ExecutionEndpointRequestNonces_KioskExecutionEndpointId_Non~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionEndpointRequestNonces_KioskExecutionEndpointId_Non~" ON public."ExecutionEndpointRequestNonces" USING btree ("KioskExecutionEndpointId", "Nonce");


--
-- Name: IX_ExecutionEndpointSupportedRobotTargets_DeviceId_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ExecutionEndpointSupportedRobotTargets_DeviceId_KioskId" ON public."ExecutionEndpointSupportedRobotTargets" USING btree ("DeviceId", "KioskId");


--
-- Name: IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~" ON public."ExecutionEndpointSupportedRobotTargets" USING btree ("KioskExecutionEndpointId", "KioskId");


--
-- Name: IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~1; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~1" ON public."ExecutionEndpointSupportedRobotTargets" USING btree ("KioskExecutionEndpointId", "RuntimeTargetCode", "MachineModelCode") WHERE ("DeviceId" IS NULL);


--
-- Name: IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~2; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~2" ON public."ExecutionEndpointSupportedRobotTargets" USING btree ("KioskExecutionEndpointId", "RuntimeTargetCode", "MachineModelCode", "DeviceId") WHERE ("DeviceId" IS NOT NULL);


--
-- Name: IX_ExecutionRouteRobotBindings_ExecutionRouteId_BindingOrder; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionRouteRobotBindings_ExecutionRouteId_BindingOrder" ON public."ExecutionRouteRobotBindings" USING btree ("ExecutionRouteId", "BindingOrder") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_ExecutionRouteRobotBindings_RobotProgramId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ExecutionRouteRobotBindings_RobotProgramId" ON public."ExecutionRouteRobotBindings" USING btree ("RobotProgramId");


--
-- Name: IX_ExecutionRoutes_ConfigurationReleaseId_ProductVariantId_Rec~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ExecutionRoutes_ConfigurationReleaseId_ProductVariantId_Rec~" ON public."ExecutionRoutes" USING btree ("ConfigurationReleaseId", "ProductVariantId", "RecipeId", "Priority");


--
-- Name: IX_ExecutionRoutes_ConfigurationReleaseId_RouteCode; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ExecutionRoutes_ConfigurationReleaseId_RouteCode" ON public."ExecutionRoutes" USING btree ("ConfigurationReleaseId", "RouteCode") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_ExecutionRoutes_ProductVariantId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ExecutionRoutes_ProductVariantId" ON public."ExecutionRoutes" USING btree ("ProductVariantId");


--
-- Name: IX_ExecutionRoutes_RecipeId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ExecutionRoutes_RecipeId" ON public."ExecutionRoutes" USING btree ("RecipeId");


--
-- Name: IX_IngredientDispenserStates_DeviceId_ContainerCode; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_IngredientDispenserStates_DeviceId_ContainerCode" ON public."IngredientDispenserStates" USING btree ("DeviceId", "ContainerCode") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_IngredientDispenserStates_IngredientId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_IngredientDispenserStates_IngredientId" ON public."IngredientDispenserStates" USING btree ("IngredientId");


--
-- Name: IX_IngredientDispenserStates_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_IngredientDispenserStates_KioskId" ON public."IngredientDispenserStates" USING btree ("KioskId");


--
-- Name: IX_IngredientDispenserStates_OriginNodeId_Version; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_IngredientDispenserStates_OriginNodeId_Version" ON public."IngredientDispenserStates" USING btree ("OriginNodeId", "Version");


--
-- Name: IX_Ingredients_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Ingredients_Code" ON public."Ingredients" USING btree ("Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_KioskConfigurationDeployments_ConfigurationReleaseId_Organi~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_KioskConfigurationDeployments_ConfigurationReleaseId_Organi~" ON public."KioskConfigurationDeployments" USING btree ("ConfigurationReleaseId", "OrganizationId");


--
-- Name: IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Idem~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Idem~" ON public."KioskConfigurationDeployments" USING btree ("KioskExecutionEndpointId", "IdempotencyKey");


--
-- Name: IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Kios~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Kios~" ON public."KioskConfigurationDeployments" USING btree ("KioskExecutionEndpointId", "KioskId");


--
-- Name: IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Stat~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Stat~" ON public."KioskConfigurationDeployments" USING btree ("KioskExecutionEndpointId", "Status");


--
-- Name: IX_KioskConfigurationDeployments_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskConfigurationDeployments_KioskId" ON public."KioskConfigurationDeployments" USING btree ("KioskId") WHERE ("Status" = ANY (ARRAY[1, 2]));


--
-- Name: IX_KioskConfigurationDeployments_KioskId_ConfigurationReleaseI~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskConfigurationDeployments_KioskId_ConfigurationReleaseI~" ON public."KioskConfigurationDeployments" USING btree ("KioskId", "ConfigurationReleaseId", "AttemptNo");


--
-- Name: IX_KioskConfigurationDeployments_KioskId_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_KioskConfigurationDeployments_KioskId_OrganizationId" ON public."KioskConfigurationDeployments" USING btree ("KioskId", "OrganizationId");


--
-- Name: IX_KioskConfigurationDeployments_KioskId_Status_RequestedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_KioskConfigurationDeployments_KioskId_Status_RequestedAt" ON public."KioskConfigurationDeployments" USING btree ("KioskId", "Status", "RequestedAt");


--
-- Name: IX_KioskConfigurationDeployments_RequestedByAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_KioskConfigurationDeployments_RequestedByAccountId" ON public."KioskConfigurationDeployments" USING btree ("RequestedByAccountId");


--
-- Name: IX_KioskExecutionEndpoints_ControllerId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskExecutionEndpoints_ControllerId" ON public."KioskExecutionEndpoints" USING btree ("ControllerId") WHERE ("ControllerId" IS NOT NULL);


--
-- Name: IX_KioskExecutionEndpoints_CredentialBindingId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskExecutionEndpoints_CredentialBindingId" ON public."KioskExecutionEndpoints" USING btree ("CredentialBindingId") WHERE ("CredentialBindingId" IS NOT NULL);


--
-- Name: IX_KioskExecutionEndpoints_FullEdgeRuntimeId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskExecutionEndpoints_FullEdgeRuntimeId" ON public."KioskExecutionEndpoints" USING btree ("FullEdgeRuntimeId") WHERE ("FullEdgeRuntimeId" IS NOT NULL);


--
-- Name: IX_KioskExecutionEndpoints_Id_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskExecutionEndpoints_Id_KioskId" ON public."KioskExecutionEndpoints" USING btree ("Id", "KioskId");


--
-- Name: IX_KioskExecutionEndpoints_KioskId_EndpointCode; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskExecutionEndpoints_KioskId_EndpointCode" ON public."KioskExecutionEndpoints" USING btree ("KioskId", "EndpointCode") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_KioskExecutionEndpoints_LastControllerActivationReportId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskExecutionEndpoints_LastControllerActivationReportId" ON public."KioskExecutionEndpoints" USING btree ("LastControllerActivationReportId") WHERE ("LastControllerActivationReportId" IS NOT NULL);


--
-- Name: IX_KioskExecutionEndpoints_LastEdgeActivationEventId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskExecutionEndpoints_LastEdgeActivationEventId" ON public."KioskExecutionEndpoints" USING btree ("LastEdgeActivationEventId") WHERE ("LastEdgeActivationEventId" IS NOT NULL);


--
-- Name: IX_KioskHeartbeats_KioskId_NodeId_HeartbeatSequence; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_KioskHeartbeats_KioskId_NodeId_HeartbeatSequence" ON public."KioskHeartbeats" USING btree ("KioskId", "NodeId", "HeartbeatSequence") WHERE ("HeartbeatSequence" IS NOT NULL);


--
-- Name: IX_KioskHeartbeats_KioskId_ReportedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_KioskHeartbeats_KioskId_ReportedAt" ON public."KioskHeartbeats" USING btree ("KioskId", "ReportedAt");


--
-- Name: IX_KioskHeartbeats_OriginNodeId_Version; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_KioskHeartbeats_OriginNodeId_Version" ON public."KioskHeartbeats" USING btree ("OriginNodeId", "Version");


--
-- Name: IX_Kiosks_Id_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Kiosks_Id_OrganizationId" ON public."Kiosks" USING btree ("Id", "OrganizationId");


--
-- Name: IX_Kiosks_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Kiosks_OrganizationId" ON public."Kiosks" USING btree ("OrganizationId");


--
-- Name: IX_Kiosks_OrganizationId_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Kiosks_OrganizationId_Code" ON public."Kiosks" USING btree ("OrganizationId", "Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_Kiosks_SerialNumber; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Kiosks_SerialNumber" ON public."Kiosks" USING btree ("SerialNumber") WHERE (("SerialNumber" IS NOT NULL) AND ("DeletedAt" IS NULL));


--
-- Name: IX_Kiosks_StoreId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Kiosks_StoreId" ON public."Kiosks" USING btree ("StoreId");


--
-- Name: IX_MaintenanceTickets_AssignedToAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MaintenanceTickets_AssignedToAccountId" ON public."MaintenanceTickets" USING btree ("AssignedToAccountId");


--
-- Name: IX_MaintenanceTickets_CreatedByAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MaintenanceTickets_CreatedByAccountId" ON public."MaintenanceTickets" USING btree ("CreatedByAccountId");


--
-- Name: IX_MaintenanceTickets_DeviceEventId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MaintenanceTickets_DeviceEventId" ON public."MaintenanceTickets" USING btree ("DeviceEventId");


--
-- Name: IX_MaintenanceTickets_DeviceId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MaintenanceTickets_DeviceId" ON public."MaintenanceTickets" USING btree ("DeviceId");


--
-- Name: IX_MaintenanceTickets_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MaintenanceTickets_KioskId" ON public."MaintenanceTickets" USING btree ("KioskId");


--
-- Name: IX_MaintenanceTickets_OrderId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MaintenanceTickets_OrderId" ON public."MaintenanceTickets" USING btree ("OrderId");


--
-- Name: IX_MaintenanceTickets_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MaintenanceTickets_OrganizationId" ON public."MaintenanceTickets" USING btree ("OrganizationId");


--
-- Name: IX_MaintenanceTickets_OrganizationId_StoreId_KioskId_Status_Re~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MaintenanceTickets_OrganizationId_StoreId_KioskId_Status_Re~" ON public."MaintenanceTickets" USING btree ("OrganizationId", "StoreId", "KioskId", "Status", "ReportedAt");


--
-- Name: IX_MaintenanceTickets_OriginNodeId_Version; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MaintenanceTickets_OriginNodeId_Version" ON public."MaintenanceTickets" USING btree ("OriginNodeId", "Version");


--
-- Name: IX_MaintenanceTickets_StoreId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MaintenanceTickets_StoreId" ON public."MaintenanceTickets" USING btree ("StoreId");


--
-- Name: IX_MaintenanceTickets_TicketNumber; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_MaintenanceTickets_TicketNumber" ON public."MaintenanceTickets" USING btree ("TicketNumber") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_MenuItems_MenuId_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_MenuItems_MenuId_Code" ON public."MenuItems" USING btree ("MenuId", "Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_MenuItems_MenuId_Status_DisplayOrder; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MenuItems_MenuId_Status_DisplayOrder" ON public."MenuItems" USING btree ("MenuId", "Status", "DisplayOrder");


--
-- Name: IX_MenuItems_ProductId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MenuItems_ProductId" ON public."MenuItems" USING btree ("ProductId");


--
-- Name: IX_MenuItems_ProductVariantId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MenuItems_ProductVariantId" ON public."MenuItems" USING btree ("ProductVariantId");


--
-- Name: IX_MenuItems_RecipeId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_MenuItems_RecipeId" ON public."MenuItems" USING btree ("RecipeId");


--
-- Name: IX_Menus_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Menus_KioskId" ON public."Menus" USING btree ("KioskId");


--
-- Name: IX_Menus_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Menus_OrganizationId" ON public."Menus" USING btree ("OrganizationId");


--
-- Name: IX_Menus_OrganizationId_StoreId_KioskId_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Menus_OrganizationId_StoreId_KioskId_Code" ON public."Menus" USING btree ("OrganizationId", "StoreId", "KioskId", "Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_Menus_OrganizationId_StoreId_KioskId_Status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Menus_OrganizationId_StoreId_KioskId_Status" ON public."Menus" USING btree ("OrganizationId", "StoreId", "KioskId", "Status");


--
-- Name: IX_Menus_StoreId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Menus_StoreId" ON public."Menus" USING btree ("StoreId");


--
-- Name: IX_OperationLogs_AccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OperationLogs_AccountId" ON public."OperationLogs" USING btree ("AccountId");


--
-- Name: IX_OperationLogs_DeviceId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OperationLogs_DeviceId" ON public."OperationLogs" USING btree ("DeviceId");


--
-- Name: IX_OperationLogs_KioskId_OccurredAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OperationLogs_KioskId_OccurredAt" ON public."OperationLogs" USING btree ("KioskId", "OccurredAt");


--
-- Name: IX_OperationLogs_OrderId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OperationLogs_OrderId" ON public."OperationLogs" USING btree ("OrderId");


--
-- Name: IX_OperationLogs_OriginNodeId_Version; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OperationLogs_OriginNodeId_Version" ON public."OperationLogs" USING btree ("OriginNodeId", "Version");


--
-- Name: IX_OptionGroups_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_OptionGroups_Code" ON public."OptionGroups" USING btree ("Code");


--
-- Name: IX_OrderExecutionRecords_KioskExecutionEndpointId_SourceExecut~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_OrderExecutionRecords_KioskExecutionEndpointId_SourceExecut~" ON public."OrderExecutionRecords" USING btree ("KioskExecutionEndpointId", "SourceExecutorId", "LastAppliedSourceEventId");


--
-- Name: IX_OrderExecutionRecords_KioskExecutionEndpointId_Status_LastE~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderExecutionRecords_KioskExecutionEndpointId_Status_LastE~" ON public."OrderExecutionRecords" USING btree ("KioskExecutionEndpointId", "Status", "LastExecutorReportedAt");


--
-- Name: IX_OrderExecutionRecords_OrderId_CloudReceivedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderExecutionRecords_OrderId_CloudReceivedAt" ON public."OrderExecutionRecords" USING btree ("OrderId", "CloudReceivedAt");


--
-- Name: IX_OrderExecutionRecords_SourceCommandId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_OrderExecutionRecords_SourceCommandId" ON public."OrderExecutionRecords" USING btree ("SourceCommandId");


--
-- Name: IX_OrderExecutionRecords_SourceCommandId_KioskExecutionEndpoin~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderExecutionRecords_SourceCommandId_KioskExecutionEndpoin~" ON public."OrderExecutionRecords" USING btree ("SourceCommandId", "KioskExecutionEndpointId");


--
-- Name: IX_OrderExecutionRecords_SourceConfigurationReleaseId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderExecutionRecords_SourceConfigurationReleaseId" ON public."OrderExecutionRecords" USING btree ("SourceConfigurationReleaseId");


--
-- Name: IX_OrderItemProductOptions_ProductOptionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderItemProductOptions_ProductOptionId" ON public."OrderItemProductOptions" USING btree ("ProductOptionId");


--
-- Name: IX_OrderItems_MenuItemId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderItems_MenuItemId" ON public."OrderItems" USING btree ("MenuItemId");


--
-- Name: IX_OrderItems_OrderId_ClientLineId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_OrderItems_OrderId_ClientLineId" ON public."OrderItems" USING btree ("OrderId", "ClientLineId") WHERE (("ClientLineId" IS NOT NULL) AND ("DeletedAt" IS NULL));


--
-- Name: IX_OrderItems_ProductId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderItems_ProductId" ON public."OrderItems" USING btree ("ProductId");


--
-- Name: IX_OrderItems_ProductVariantId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderItems_ProductVariantId" ON public."OrderItems" USING btree ("ProductVariantId");


--
-- Name: IX_OrderItems_RecipeId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderItems_RecipeId" ON public."OrderItems" USING btree ("RecipeId");


--
-- Name: IX_OrderStatusHistories_ChangedByAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderStatusHistories_ChangedByAccountId" ON public."OrderStatusHistories" USING btree ("ChangedByAccountId");


--
-- Name: IX_OrderStatusHistories_OrderId_ChangedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_OrderStatusHistories_OrderId_ChangedAt" ON public."OrderStatusHistories" USING btree ("OrderId", "ChangedAt");


--
-- Name: IX_Orders_IdempotencyKey; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Orders_IdempotencyKey" ON public."Orders" USING btree ("IdempotencyKey") WHERE ("IdempotencyKey" IS NOT NULL);


--
-- Name: IX_Orders_KioskId_ClientOrderId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Orders_KioskId_ClientOrderId" ON public."Orders" USING btree ("KioskId", "ClientOrderId") WHERE ("ClientOrderId" IS NOT NULL);


--
-- Name: IX_Orders_OrderNumber; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Orders_OrderNumber" ON public."Orders" USING btree ("OrderNumber");


--
-- Name: IX_Orders_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Orders_OrganizationId" ON public."Orders" USING btree ("OrganizationId");


--
-- Name: IX_Orders_OrganizationId_StoreId_KioskId_PlacedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Orders_OrganizationId_StoreId_KioskId_PlacedAt" ON public."Orders" USING btree ("OrganizationId", "StoreId", "KioskId", "PlacedAt");


--
-- Name: IX_Orders_StoreId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Orders_StoreId" ON public."Orders" USING btree ("StoreId");


--
-- Name: IX_Organizations_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Organizations_Code" ON public."Organizations" USING btree ("Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_PasswordResetRequests_AccountId_RequestedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_PasswordResetRequests_AccountId_RequestedAt" ON public."PasswordResetRequests" USING btree ("AccountId", "RequestedAt");


--
-- Name: IX_PasswordResetRequests_TokenHash; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_PasswordResetRequests_TokenHash" ON public."PasswordResetRequests" USING btree ("TokenHash");


--
-- Name: IX_PaymentCallbacks_PaymentTransactionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_PaymentCallbacks_PaymentTransactionId" ON public."PaymentCallbacks" USING btree ("PaymentTransactionId");


--
-- Name: IX_PaymentCallbacks_Provider_ProviderEventId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_PaymentCallbacks_Provider_ProviderEventId" ON public."PaymentCallbacks" USING btree ("Provider", "ProviderEventId") WHERE ("ProviderEventId" IS NOT NULL);


--
-- Name: IX_PaymentMethods_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_PaymentMethods_Code" ON public."PaymentMethods" USING btree ("Code");


--
-- Name: IX_PaymentTransactions_IdempotencyKey; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_PaymentTransactions_IdempotencyKey" ON public."PaymentTransactions" USING btree ("IdempotencyKey") WHERE ("IdempotencyKey" IS NOT NULL);


--
-- Name: IX_PaymentTransactions_OrderId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_PaymentTransactions_OrderId" ON public."PaymentTransactions" USING btree ("OrderId");


--
-- Name: IX_PaymentTransactions_PaymentMethodId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_PaymentTransactions_PaymentMethodId" ON public."PaymentTransactions" USING btree ("PaymentMethodId");


--
-- Name: IX_PaymentTransactions_ProviderOrderCode; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_PaymentTransactions_ProviderOrderCode" ON public."PaymentTransactions" USING btree ("ProviderOrderCode") WHERE ("ProviderOrderCode" IS NOT NULL);


--
-- Name: IX_PaymentTransactions_ProviderTransactionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_PaymentTransactions_ProviderTransactionId" ON public."PaymentTransactions" USING btree ("ProviderTransactionId") WHERE ("ProviderTransactionId" IS NOT NULL);


--
-- Name: IX_PaymentTransactions_TransactionNumber; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_PaymentTransactions_TransactionNumber" ON public."PaymentTransactions" USING btree ("TransactionNumber");


--
-- Name: IX_ProductCategories_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ProductCategories_Code" ON public."ProductCategories" USING btree ("Code");


--
-- Name: IX_ProductCategories_ParentCategoryId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ProductCategories_ParentCategoryId" ON public."ProductCategories" USING btree ("ParentCategoryId");


--
-- Name: IX_ProductOptions_OptionGroupId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ProductOptions_OptionGroupId" ON public."ProductOptions" USING btree ("OptionGroupId");


--
-- Name: IX_ProductOptions_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ProductOptions_OrganizationId" ON public."ProductOptions" USING btree ("OrganizationId");


--
-- Name: IX_ProductOptions_OrganizationId_OptionGroupId_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ProductOptions_OrganizationId_OptionGroupId_Code" ON public."ProductOptions" USING btree ("OrganizationId", "OptionGroupId", "Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_ProductOptions_TemplateProductOptionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ProductOptions_TemplateProductOptionId" ON public."ProductOptions" USING btree ("TemplateProductOptionId");


--
-- Name: IX_ProductProductOptions_ProductOptionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ProductProductOptions_ProductOptionId" ON public."ProductProductOptions" USING btree ("ProductOptionId");


--
-- Name: IX_ProductVariants_ProductId_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ProductVariants_ProductId_Code" ON public."ProductVariants" USING btree ("ProductId", "Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_ProductVariants_ProductId_DisplayOrder; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ProductVariants_ProductId_DisplayOrder" ON public."ProductVariants" USING btree ("ProductId", "DisplayOrder");


--
-- Name: IX_ProductionEventCheckpoints_KioskExecutionEndpointId_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ProductionEventCheckpoints_KioskExecutionEndpointId_KioskId" ON public."ProductionEventCheckpoints" USING btree ("KioskExecutionEndpointId", "KioskId");


--
-- Name: IX_ProductionEventCheckpoints_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ProductionEventCheckpoints_KioskId" ON public."ProductionEventCheckpoints" USING btree ("KioskId");


--
-- Name: IX_ProductionEventCheckpoints_SourceExecutorId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ProductionEventCheckpoints_SourceExecutorId" ON public."ProductionEventCheckpoints" USING btree ("SourceExecutorId");


--
-- Name: IX_ProductionExecutionRecords_KioskExecutionEndpointId_SourceE~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ProductionExecutionRecords_KioskExecutionEndpointId_SourceE~" ON public."ProductionExecutionRecords" USING btree ("KioskExecutionEndpointId", "SourceExecutorId", "LastAppliedSourceEventId");


--
-- Name: IX_ProductionExecutionRecords_KioskExecutionEndpointId_Status_~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ProductionExecutionRecords_KioskExecutionEndpointId_Status_~" ON public."ProductionExecutionRecords" USING btree ("KioskExecutionEndpointId", "Status", "LastExecutorReportedAt");


--
-- Name: IX_ProductionExecutionRecords_SourceCommandId_KioskExecutionEn~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ProductionExecutionRecords_SourceCommandId_KioskExecutionEn~" ON public."ProductionExecutionRecords" USING btree ("SourceCommandId", "KioskExecutionEndpointId");


--
-- Name: IX_ProductionExecutionRecords_SourceCommandId_SourceProduction~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_ProductionExecutionRecords_SourceCommandId_SourceProduction~" ON public."ProductionExecutionRecords" USING btree ("SourceCommandId", "SourceProductionJobId");


--
-- Name: IX_Products_CategoryId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Products_CategoryId" ON public."Products" USING btree ("CategoryId");


--
-- Name: IX_Products_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Products_KioskId" ON public."Products" USING btree ("KioskId");


--
-- Name: IX_Products_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Products_OrganizationId" ON public."Products" USING btree ("OrganizationId");


--
-- Name: IX_Products_OrganizationId_StoreId_KioskId_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Products_OrganizationId_StoreId_KioskId_Code" ON public."Products" USING btree ("OrganizationId", "StoreId", "KioskId", "Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_Products_StoreId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Products_StoreId" ON public."Products" USING btree ("StoreId");


--
-- Name: IX_Products_TemplateProductId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Products_TemplateProductId" ON public."Products" USING btree ("TemplateProductId");


--
-- Name: IX_RecipeItems_IngredientId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RecipeItems_IngredientId" ON public."RecipeItems" USING btree ("IngredientId");


--
-- Name: IX_RecipeItems_RecipeId_IngredientId_StepOrder; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RecipeItems_RecipeId_IngredientId_StepOrder" ON public."RecipeItems" USING btree ("RecipeId", "IngredientId", "StepOrder") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_Recipes_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Recipes_KioskId" ON public."Recipes" USING btree ("KioskId");


--
-- Name: IX_Recipes_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Recipes_OrganizationId" ON public."Recipes" USING btree ("OrganizationId");


--
-- Name: IX_Recipes_OrganizationId_StoreId_KioskId_ProductVariantId_Cod~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Recipes_OrganizationId_StoreId_KioskId_ProductVariantId_Cod~" ON public."Recipes" USING btree ("OrganizationId", "StoreId", "KioskId", "ProductVariantId", "Code", "Version") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_Recipes_ProductVariantId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Recipes_ProductVariantId" ON public."Recipes" USING btree ("ProductVariantId");


--
-- Name: IX_Recipes_StoreId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Recipes_StoreId" ON public."Recipes" USING btree ("StoreId");


--
-- Name: IX_Recipes_TemplateRecipeId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Recipes_TemplateRecipeId" ON public."Recipes" USING btree ("TemplateRecipeId");


--
-- Name: IX_RefreshTokens_AccountDeviceId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RefreshTokens_AccountDeviceId" ON public."RefreshTokens" USING btree ("AccountDeviceId");


--
-- Name: IX_RefreshTokens_AccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RefreshTokens_AccountId" ON public."RefreshTokens" USING btree ("AccountId");


--
-- Name: IX_RefreshTokens_ReplacedByTokenId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RefreshTokens_ReplacedByTokenId" ON public."RefreshTokens" USING btree ("ReplacedByTokenId");


--
-- Name: IX_RefreshTokens_TokenHash; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RefreshTokens_TokenHash" ON public."RefreshTokens" USING btree ("TokenHash");


--
-- Name: IX_Refunds_IdempotencyKey; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Refunds_IdempotencyKey" ON public."Refunds" USING btree ("IdempotencyKey") WHERE ("IdempotencyKey" IS NOT NULL);


--
-- Name: IX_Refunds_PaymentTransactionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Refunds_PaymentTransactionId" ON public."Refunds" USING btree ("PaymentTransactionId");


--
-- Name: IX_Refunds_ProviderRefundId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Refunds_ProviderRefundId" ON public."Refunds" USING btree ("ProviderRefundId") WHERE ("ProviderRefundId" IS NOT NULL);


--
-- Name: IX_Refunds_RefundNumber; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Refunds_RefundNumber" ON public."Refunds" USING btree ("RefundNumber");


--
-- Name: IX_Refunds_RequestedByAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Refunds_RequestedByAccountId" ON public."Refunds" USING btree ("RequestedByAccountId");


--
-- Name: IX_RobotArtifactTemplates_RuntimeTargetCode_MachineModelCode_S~; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotArtifactTemplates_RuntimeTargetCode_MachineModelCode_S~" ON public."RobotArtifactTemplates" USING btree ("RuntimeTargetCode", "MachineModelCode", "Status");


--
-- Name: IX_RobotArtifactTemplates_StorageKey; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RobotArtifactTemplates_StorageKey" ON public."RobotArtifactTemplates" USING btree ("StorageKey") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_RobotArtifactTemplates_TemplateCode_Checksum; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RobotArtifactTemplates_TemplateCode_Checksum" ON public."RobotArtifactTemplates" USING btree ("TemplateCode", "Checksum") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_RobotArtifacts_OrganizationId_ArtifactCode_Checksum; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RobotArtifacts_OrganizationId_ArtifactCode_Checksum" ON public."RobotArtifacts" USING btree ("OrganizationId", "ArtifactCode", "Checksum") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_RobotArtifacts_OriginNodeId_Version; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotArtifacts_OriginNodeId_Version" ON public."RobotArtifacts" USING btree ("OriginNodeId", "Version");


--
-- Name: IX_RobotArtifacts_RuntimeTargetCode_MachineModelCode_Status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotArtifacts_RuntimeTargetCode_MachineModelCode_Status" ON public."RobotArtifacts" USING btree ("RuntimeTargetCode", "MachineModelCode", "Status");


--
-- Name: IX_RobotArtifacts_SourceRobotArtifactTemplateId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotArtifacts_SourceRobotArtifactTemplateId" ON public."RobotArtifacts" USING btree ("SourceRobotArtifactTemplateId");


--
-- Name: IX_RobotArtifacts_StorageKey; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RobotArtifacts_StorageKey" ON public."RobotArtifacts" USING btree ("StorageKey") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_RobotProgramArtifacts_OriginNodeId_Version; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotProgramArtifacts_OriginNodeId_Version" ON public."RobotProgramArtifacts" USING btree ("OriginNodeId", "Version");


--
-- Name: IX_RobotProgramArtifacts_RobotArtifactId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotProgramArtifacts_RobotArtifactId" ON public."RobotProgramArtifacts" USING btree ("RobotArtifactId");


--
-- Name: IX_RobotProgramArtifacts_RobotProgramId_RunOrder; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RobotProgramArtifacts_RobotProgramId_RunOrder" ON public."RobotProgramArtifacts" USING btree ("RobotProgramId", "RunOrder") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_RobotPrograms_DeviceId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotPrograms_DeviceId" ON public."RobotPrograms" USING btree ("DeviceId");


--
-- Name: IX_RobotPrograms_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotPrograms_KioskId" ON public."RobotPrograms" USING btree ("KioskId");


--
-- Name: IX_RobotPrograms_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotPrograms_OrganizationId" ON public."RobotPrograms" USING btree ("OrganizationId");


--
-- Name: IX_RobotPrograms_OrganizationId_StoreId_KioskId_DeviceId_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RobotPrograms_OrganizationId_StoreId_KioskId_DeviceId_Code" ON public."RobotPrograms" USING btree ("OrganizationId", "StoreId", "KioskId", "DeviceId", "Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_RobotPrograms_OriginNodeId_Version; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotPrograms_OriginNodeId_Version" ON public."RobotPrograms" USING btree ("OriginNodeId", "Version");


--
-- Name: IX_RobotPrograms_ProgramManifestChecksum; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RobotPrograms_ProgramManifestChecksum" ON public."RobotPrograms" USING btree ("ProgramManifestChecksum") WHERE ("ProgramManifestChecksum" IS NOT NULL);


--
-- Name: IX_RobotPrograms_StoreId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RobotPrograms_StoreId" ON public."RobotPrograms" USING btree ("StoreId");


--
-- Name: IX_Roles_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Roles_Code" ON public."Roles" USING btree ("Code");


--
-- Name: IX_StockMovements_CreatedByAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_StockMovements_CreatedByAccountId" ON public."StockMovements" USING btree ("CreatedByAccountId");


--
-- Name: IX_StockMovements_DeviceId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_StockMovements_DeviceId" ON public."StockMovements" USING btree ("DeviceId");


--
-- Name: IX_StockMovements_IngredientDispenserStateId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_StockMovements_IngredientDispenserStateId" ON public."StockMovements" USING btree ("IngredientDispenserStateId");


--
-- Name: IX_StockMovements_IngredientId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_StockMovements_IngredientId" ON public."StockMovements" USING btree ("IngredientId");


--
-- Name: IX_StockMovements_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_StockMovements_KioskId" ON public."StockMovements" USING btree ("KioskId");


--
-- Name: IX_StockMovements_OrganizationId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_StockMovements_OrganizationId" ON public."StockMovements" USING btree ("OrganizationId");


--
-- Name: IX_StockMovements_OrganizationId_StoreId_KioskId_OccurredAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_StockMovements_OrganizationId_StoreId_KioskId_OccurredAt" ON public."StockMovements" USING btree ("OrganizationId", "StoreId", "KioskId", "OccurredAt");


--
-- Name: IX_StockMovements_OriginNodeId_Version; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_StockMovements_OriginNodeId_Version" ON public."StockMovements" USING btree ("OriginNodeId", "Version");


--
-- Name: IX_StockMovements_SourceEventId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_StockMovements_SourceEventId" ON public."StockMovements" USING btree ("SourceEventId") WHERE ("SourceEventId" IS NOT NULL);


--
-- Name: IX_StockMovements_StoreId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_StockMovements_StoreId" ON public."StockMovements" USING btree ("StoreId");


--
-- Name: IX_Stores_OrganizationId_Code; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Stores_OrganizationId_Code" ON public."Stores" USING btree ("OrganizationId", "Code") WHERE ("DeletedAt" IS NULL);


--
-- Name: IX_SyncDeadLetterRetryAttempts_RequestedByAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_SyncDeadLetterRetryAttempts_RequestedByAccountId" ON public."SyncDeadLetterRetryAttempts" USING btree ("RequestedByAccountId");


--
-- Name: IX_SyncDeadLetterRetryAttempts_SyncDeadLetterId_AttemptNumber; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_SyncDeadLetterRetryAttempts_SyncDeadLetterId_AttemptNumber" ON public."SyncDeadLetterRetryAttempts" USING btree ("SyncDeadLetterId", "AttemptNumber");


--
-- Name: IX_SyncDeadLetters_EventId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_SyncDeadLetters_EventId" ON public."SyncDeadLetters" USING btree ("EventId") WHERE ("EventId" IS NOT NULL);


--
-- Name: IX_SyncDeadLetters_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_SyncDeadLetters_KioskId" ON public."SyncDeadLetters" USING btree ("KioskId");


--
-- Name: IX_SyncDeadLetters_ResolvedByAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_SyncDeadLetters_ResolvedByAccountId" ON public."SyncDeadLetters" USING btree ("ResolvedByAccountId");


--
-- Name: IX_SyncDeadLetters_Status_FailedAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_SyncDeadLetters_Status_FailedAt" ON public."SyncDeadLetters" USING btree ("Status", "FailedAt");


--
-- Name: IX_SyncDeadLetters_SyncEventInboxId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_SyncDeadLetters_SyncEventInboxId" ON public."SyncDeadLetters" USING btree ("SyncEventInboxId");


--
-- Name: IX_SyncEventInbox_KioskId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_SyncEventInbox_KioskId" ON public."SyncEventInbox" USING btree ("KioskId");


--
-- Name: IX_SyncEventInbox_SourceNodeId_EventId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_SyncEventInbox_SourceNodeId_EventId" ON public."SyncEventInbox" USING btree ("SourceNodeId", "EventId");


--
-- Name: IX_SyncEventInbox_SourceNodeId_EventType_OccurredAt; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_SyncEventInbox_SourceNodeId_EventType_OccurredAt" ON public."SyncEventInbox" USING btree ("SourceNodeId", "EventType", "OccurredAt");


--
-- Name: IX_SyncEventInbox_SourceNodeId_SequenceNumber; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_SyncEventInbox_SourceNodeId_SequenceNumber" ON public."SyncEventInbox" USING btree ("SourceNodeId", "SequenceNumber") WHERE ("SequenceNumber" IS NOT NULL);


--
-- Name: IX_SyncEventInbox_Status_NextRetryAt_LockedUntil; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_SyncEventInbox_Status_NextRetryAt_LockedUntil" ON public."SyncEventInbox" USING btree ("Status", "NextRetryAt", "LockedUntil");


--
-- Name: AccountDevices FK_AccountDevices_Accounts_AccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountDevices"
    ADD CONSTRAINT "FK_AccountDevices_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: AccountInvitations FK_AccountInvitations_Accounts_AccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountInvitations"
    ADD CONSTRAINT "FK_AccountInvitations_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: AccountRoles FK_AccountRoles_Accounts_AccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountRoles"
    ADD CONSTRAINT "FK_AccountRoles_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: AccountRoles FK_AccountRoles_Accounts_AssignedByAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountRoles"
    ADD CONSTRAINT "FK_AccountRoles_Accounts_AssignedByAccountId" FOREIGN KEY ("AssignedByAccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: AccountRoles FK_AccountRoles_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountRoles"
    ADD CONSTRAINT "FK_AccountRoles_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: AccountRoles FK_AccountRoles_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountRoles"
    ADD CONSTRAINT "FK_AccountRoles_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: AccountRoles FK_AccountRoles_Roles_RoleId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountRoles"
    ADD CONSTRAINT "FK_AccountRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."Roles"("Id") ON DELETE RESTRICT;


--
-- Name: AccountRoles FK_AccountRoles_Stores_StoreId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountRoles"
    ADD CONSTRAINT "FK_AccountRoles_Stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES public."Stores"("Id") ON DELETE RESTRICT;


--
-- Name: AccountStores FK_AccountStores_Accounts_AccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountStores"
    ADD CONSTRAINT "FK_AccountStores_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: AccountStores FK_AccountStores_Stores_StoreId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."AccountStores"
    ADD CONSTRAINT "FK_AccountStores_Stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES public."Stores"("Id") ON DELETE RESTRICT;


--
-- Name: Alerts FK_Alerts_Accounts_AcknowledgedByAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Alerts"
    ADD CONSTRAINT "FK_Alerts_Accounts_AcknowledgedByAccountId" FOREIGN KEY ("AcknowledgedByAccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: Alerts FK_Alerts_Devices_DeviceId_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Alerts"
    ADD CONSTRAINT "FK_Alerts_Devices_DeviceId_KioskId" FOREIGN KEY ("DeviceId", "KioskId") REFERENCES public."Devices"("Id", "KioskId") ON DELETE RESTRICT;


--
-- Name: Alerts FK_Alerts_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Alerts"
    ADD CONSTRAINT "FK_Alerts_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: ConfigurationReleases FK_ConfigurationReleases_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ConfigurationReleases"
    ADD CONSTRAINT "FK_ConfigurationReleases_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: ControllerArtifactSetDeployments FK_ControllerArtifactSetDeployments_ConfigurationReleases_Sour~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ControllerArtifactSetDeployments"
    ADD CONSTRAINT "FK_ControllerArtifactSetDeployments_ConfigurationReleases_Sour~" FOREIGN KEY ("SourceConfigurationReleaseId", "OrganizationId") REFERENCES public."ConfigurationReleases"("Id", "OrganizationId") ON DELETE RESTRICT;


--
-- Name: ControllerArtifactSetDeployments FK_ControllerArtifactSetDeployments_KioskExecutionEndpoints_Ki~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ControllerArtifactSetDeployments"
    ADD CONSTRAINT "FK_ControllerArtifactSetDeployments_KioskExecutionEndpoints_Ki~" FOREIGN KEY ("KioskExecutionEndpointId", "KioskId") REFERENCES public."KioskExecutionEndpoints"("Id", "KioskId") ON DELETE RESTRICT;


--
-- Name: ControllerArtifactSetDeployments FK_ControllerArtifactSetDeployments_Kiosks_KioskId_Organizatio~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ControllerArtifactSetDeployments"
    ADD CONSTRAINT "FK_ControllerArtifactSetDeployments_Kiosks_KioskId_Organizatio~" FOREIGN KEY ("KioskId", "OrganizationId") REFERENCES public."Kiosks"("Id", "OrganizationId") ON DELETE RESTRICT;


--
-- Name: ControllerArtifactSetItems FK_ControllerArtifactSetItems_ControllerArtifactSetDeployments~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ControllerArtifactSetItems"
    ADD CONSTRAINT "FK_ControllerArtifactSetItems_ControllerArtifactSetDeployments~" FOREIGN KEY ("ControllerArtifactSetDeploymentId") REFERENCES public."ControllerArtifactSetDeployments"("Id") ON DELETE RESTRICT;


--
-- Name: DeviceEvents FK_DeviceEvents_Devices_DeviceId_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DeviceEvents"
    ADD CONSTRAINT "FK_DeviceEvents_Devices_DeviceId_KioskId" FOREIGN KEY ("DeviceId", "KioskId") REFERENCES public."Devices"("Id", "KioskId") ON DELETE RESTRICT;


--
-- Name: DeviceEvents FK_DeviceEvents_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DeviceEvents"
    ADD CONSTRAINT "FK_DeviceEvents_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: DeviceModels FK_DeviceModels_DeviceTypes_DeviceTypeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DeviceModels"
    ADD CONSTRAINT "FK_DeviceModels_DeviceTypes_DeviceTypeId" FOREIGN KEY ("DeviceTypeId") REFERENCES public."DeviceTypes"("Id") ON DELETE RESTRICT;


--
-- Name: Devices FK_Devices_DeviceModels_DeviceModelId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Devices"
    ADD CONSTRAINT "FK_Devices_DeviceModels_DeviceModelId" FOREIGN KEY ("DeviceModelId") REFERENCES public."DeviceModels"("Id") ON DELETE RESTRICT;


--
-- Name: Devices FK_Devices_DeviceTypes_DeviceTypeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Devices"
    ADD CONSTRAINT "FK_Devices_DeviceTypes_DeviceTypeId" FOREIGN KEY ("DeviceTypeId") REFERENCES public."DeviceTypes"("Id") ON DELETE RESTRICT;


--
-- Name: Devices FK_Devices_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Devices"
    ADD CONSTRAINT "FK_Devices_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: EdgeCommandDeliveryAttempts FK_EdgeCommandDeliveryAttempts_EdgeCommands_EdgeCommandId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EdgeCommandDeliveryAttempts"
    ADD CONSTRAINT "FK_EdgeCommandDeliveryAttempts_EdgeCommands_EdgeCommandId" FOREIGN KEY ("EdgeCommandId") REFERENCES public."EdgeCommands"("Id") ON DELETE RESTRICT;


--
-- Name: EdgeCommands FK_EdgeCommands_KioskExecutionEndpoints_TargetExecutionEndpoin~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EdgeCommands"
    ADD CONSTRAINT "FK_EdgeCommands_KioskExecutionEndpoints_TargetExecutionEndpoin~" FOREIGN KEY ("TargetExecutionEndpointId", "KioskId") REFERENCES public."KioskExecutionEndpoints"("Id", "KioskId") ON DELETE RESTRICT;


--
-- Name: EdgeStateSummaries FK_EdgeStateSummaries_KioskExecutionEndpoints_KioskExecutionEn~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EdgeStateSummaries"
    ADD CONSTRAINT "FK_EdgeStateSummaries_KioskExecutionEndpoints_KioskExecutionEn~" FOREIGN KEY ("KioskExecutionEndpointId", "KioskId") REFERENCES public."KioskExecutionEndpoints"("Id", "KioskId") ON DELETE RESTRICT;


--
-- Name: EdgeStateSummaries FK_EdgeStateSummaries_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EdgeStateSummaries"
    ADD CONSTRAINT "FK_EdgeStateSummaries_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: ExecutionEndpointCapabilityProjections FK_ExecutionEndpointCapabilityProjections_ExecutionEndpointRea~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointCapabilityProjections"
    ADD CONSTRAINT "FK_ExecutionEndpointCapabilityProjections_ExecutionEndpointRea~" FOREIGN KEY ("ExecutionEndpointReadinessProjectionId") REFERENCES public."ExecutionEndpointReadinessProjections"("Id") ON DELETE RESTRICT;


--
-- Name: ExecutionEndpointCredentialBindings FK_ExecutionEndpointCredentialBindings_KioskExecutionEndpoints~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointCredentialBindings"
    ADD CONSTRAINT "FK_ExecutionEndpointCredentialBindings_KioskExecutionEndpoints~" FOREIGN KEY ("KioskExecutionEndpointId") REFERENCES public."KioskExecutionEndpoints"("Id") ON DELETE RESTRICT;


--
-- Name: ExecutionEndpointMqttCredentials FK_ExecutionEndpointMqttCredentials_KioskExecutionEndpoints_Ki~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointMqttCredentials"
    ADD CONSTRAINT "FK_ExecutionEndpointMqttCredentials_KioskExecutionEndpoints_Ki~" FOREIGN KEY ("KioskExecutionEndpointId") REFERENCES public."KioskExecutionEndpoints"("Id") ON DELETE RESTRICT;


--
-- Name: ExecutionEndpointReadinessProjections FK_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointReadinessProjections"
    ADD CONSTRAINT "FK_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~" FOREIGN KEY ("KioskExecutionEndpointId", "KioskId") REFERENCES public."KioskExecutionEndpoints"("Id", "KioskId") ON DELETE RESTRICT;


--
-- Name: ExecutionEndpointReadinessProjections FK_ExecutionEndpointReadinessProjections_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointReadinessProjections"
    ADD CONSTRAINT "FK_ExecutionEndpointReadinessProjections_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: ExecutionEndpointRequestNonces FK_ExecutionEndpointRequestNonces_KioskExecutionEndpoints_Kios~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointRequestNonces"
    ADD CONSTRAINT "FK_ExecutionEndpointRequestNonces_KioskExecutionEndpoints_Kios~" FOREIGN KEY ("KioskExecutionEndpointId") REFERENCES public."KioskExecutionEndpoints"("Id") ON DELETE RESTRICT;


--
-- Name: ExecutionEndpointSupportedRobotTargets FK_ExecutionEndpointSupportedRobotTargets_Devices_DeviceId_Kio~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointSupportedRobotTargets"
    ADD CONSTRAINT "FK_ExecutionEndpointSupportedRobotTargets_Devices_DeviceId_Kio~" FOREIGN KEY ("DeviceId", "KioskId") REFERENCES public."Devices"("Id", "KioskId") ON DELETE RESTRICT;


--
-- Name: ExecutionEndpointSupportedRobotTargets FK_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionEndpointSupportedRobotTargets"
    ADD CONSTRAINT "FK_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~" FOREIGN KEY ("KioskExecutionEndpointId", "KioskId") REFERENCES public."KioskExecutionEndpoints"("Id", "KioskId") ON DELETE RESTRICT;


--
-- Name: ExecutionRouteRobotBindings FK_ExecutionRouteRobotBindings_ExecutionRoutes_ExecutionRouteId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionRouteRobotBindings"
    ADD CONSTRAINT "FK_ExecutionRouteRobotBindings_ExecutionRoutes_ExecutionRouteId" FOREIGN KEY ("ExecutionRouteId") REFERENCES public."ExecutionRoutes"("Id") ON DELETE RESTRICT;


--
-- Name: ExecutionRouteRobotBindings FK_ExecutionRouteRobotBindings_RobotPrograms_RobotProgramId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionRouteRobotBindings"
    ADD CONSTRAINT "FK_ExecutionRouteRobotBindings_RobotPrograms_RobotProgramId" FOREIGN KEY ("RobotProgramId") REFERENCES public."RobotPrograms"("Id") ON DELETE RESTRICT;


--
-- Name: ExecutionRoutes FK_ExecutionRoutes_ConfigurationReleases_ConfigurationReleaseId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionRoutes"
    ADD CONSTRAINT "FK_ExecutionRoutes_ConfigurationReleases_ConfigurationReleaseId" FOREIGN KEY ("ConfigurationReleaseId") REFERENCES public."ConfigurationReleases"("Id") ON DELETE RESTRICT;


--
-- Name: ExecutionRoutes FK_ExecutionRoutes_ProductVariants_ProductVariantId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionRoutes"
    ADD CONSTRAINT "FK_ExecutionRoutes_ProductVariants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES public."ProductVariants"("Id") ON DELETE RESTRICT;


--
-- Name: ExecutionRoutes FK_ExecutionRoutes_Recipes_RecipeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExecutionRoutes"
    ADD CONSTRAINT "FK_ExecutionRoutes_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES public."Recipes"("Id") ON DELETE RESTRICT;


--
-- Name: IngredientDispenserStates FK_IngredientDispenserStates_Devices_DeviceId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."IngredientDispenserStates"
    ADD CONSTRAINT "FK_IngredientDispenserStates_Devices_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES public."Devices"("Id") ON DELETE RESTRICT;


--
-- Name: IngredientDispenserStates FK_IngredientDispenserStates_Ingredients_IngredientId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."IngredientDispenserStates"
    ADD CONSTRAINT "FK_IngredientDispenserStates_Ingredients_IngredientId" FOREIGN KEY ("IngredientId") REFERENCES public."Ingredients"("Id") ON DELETE RESTRICT;


--
-- Name: IngredientDispenserStates FK_IngredientDispenserStates_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."IngredientDispenserStates"
    ADD CONSTRAINT "FK_IngredientDispenserStates_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: KioskConfigurationDeployments FK_KioskConfigurationDeployments_Accounts_RequestedByAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskConfigurationDeployments"
    ADD CONSTRAINT "FK_KioskConfigurationDeployments_Accounts_RequestedByAccountId" FOREIGN KEY ("RequestedByAccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: KioskConfigurationDeployments FK_KioskConfigurationDeployments_ConfigurationReleases_Configu~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskConfigurationDeployments"
    ADD CONSTRAINT "FK_KioskConfigurationDeployments_ConfigurationReleases_Configu~" FOREIGN KEY ("ConfigurationReleaseId", "OrganizationId") REFERENCES public."ConfigurationReleases"("Id", "OrganizationId") ON DELETE RESTRICT;


--
-- Name: KioskConfigurationDeployments FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskConfigurationDeployments"
    ADD CONSTRAINT "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~" FOREIGN KEY ("KioskExecutionEndpointId", "KioskId") REFERENCES public."KioskExecutionEndpoints"("Id", "KioskId") ON DELETE RESTRICT;


--
-- Name: KioskConfigurationDeployments FK_KioskConfigurationDeployments_Kiosks_KioskId_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskConfigurationDeployments"
    ADD CONSTRAINT "FK_KioskConfigurationDeployments_Kiosks_KioskId_OrganizationId" FOREIGN KEY ("KioskId", "OrganizationId") REFERENCES public."Kiosks"("Id", "OrganizationId") ON DELETE RESTRICT;


--
-- Name: KioskExecutionEndpoints FK_KioskExecutionEndpoints_ExecutionEndpointCredentialBindings~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskExecutionEndpoints"
    ADD CONSTRAINT "FK_KioskExecutionEndpoints_ExecutionEndpointCredentialBindings~" FOREIGN KEY ("CredentialBindingId") REFERENCES public."ExecutionEndpointCredentialBindings"("Id") ON DELETE RESTRICT;


--
-- Name: KioskExecutionEndpoints FK_KioskExecutionEndpoints_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskExecutionEndpoints"
    ADD CONSTRAINT "FK_KioskExecutionEndpoints_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: KioskHeartbeats FK_KioskHeartbeats_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."KioskHeartbeats"
    ADD CONSTRAINT "FK_KioskHeartbeats_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: Kiosks FK_Kiosks_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Kiosks"
    ADD CONSTRAINT "FK_Kiosks_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: Kiosks FK_Kiosks_Stores_StoreId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Kiosks"
    ADD CONSTRAINT "FK_Kiosks_Stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES public."Stores"("Id") ON DELETE RESTRICT;


--
-- Name: MaintenanceTickets FK_MaintenanceTickets_Accounts_AssignedToAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MaintenanceTickets"
    ADD CONSTRAINT "FK_MaintenanceTickets_Accounts_AssignedToAccountId" FOREIGN KEY ("AssignedToAccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: MaintenanceTickets FK_MaintenanceTickets_Accounts_CreatedByAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MaintenanceTickets"
    ADD CONSTRAINT "FK_MaintenanceTickets_Accounts_CreatedByAccountId" FOREIGN KEY ("CreatedByAccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: MaintenanceTickets FK_MaintenanceTickets_DeviceEvents_DeviceEventId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MaintenanceTickets"
    ADD CONSTRAINT "FK_MaintenanceTickets_DeviceEvents_DeviceEventId" FOREIGN KEY ("DeviceEventId") REFERENCES public."DeviceEvents"("Id") ON DELETE RESTRICT;


--
-- Name: MaintenanceTickets FK_MaintenanceTickets_Devices_DeviceId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MaintenanceTickets"
    ADD CONSTRAINT "FK_MaintenanceTickets_Devices_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES public."Devices"("Id") ON DELETE RESTRICT;


--
-- Name: MaintenanceTickets FK_MaintenanceTickets_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MaintenanceTickets"
    ADD CONSTRAINT "FK_MaintenanceTickets_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: MaintenanceTickets FK_MaintenanceTickets_Orders_OrderId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MaintenanceTickets"
    ADD CONSTRAINT "FK_MaintenanceTickets_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES public."Orders"("Id") ON DELETE RESTRICT;


--
-- Name: MaintenanceTickets FK_MaintenanceTickets_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MaintenanceTickets"
    ADD CONSTRAINT "FK_MaintenanceTickets_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: MaintenanceTickets FK_MaintenanceTickets_Stores_StoreId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MaintenanceTickets"
    ADD CONSTRAINT "FK_MaintenanceTickets_Stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES public."Stores"("Id") ON DELETE RESTRICT;


--
-- Name: MenuItems FK_MenuItems_Menus_MenuId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MenuItems"
    ADD CONSTRAINT "FK_MenuItems_Menus_MenuId" FOREIGN KEY ("MenuId") REFERENCES public."Menus"("Id") ON DELETE RESTRICT;


--
-- Name: MenuItems FK_MenuItems_ProductVariants_ProductVariantId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MenuItems"
    ADD CONSTRAINT "FK_MenuItems_ProductVariants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES public."ProductVariants"("Id") ON DELETE RESTRICT;


--
-- Name: MenuItems FK_MenuItems_Products_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MenuItems"
    ADD CONSTRAINT "FK_MenuItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: MenuItems FK_MenuItems_Recipes_RecipeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MenuItems"
    ADD CONSTRAINT "FK_MenuItems_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES public."Recipes"("Id") ON DELETE RESTRICT;


--
-- Name: Menus FK_Menus_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Menus"
    ADD CONSTRAINT "FK_Menus_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: Menus FK_Menus_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Menus"
    ADD CONSTRAINT "FK_Menus_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: Menus FK_Menus_Stores_StoreId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Menus"
    ADD CONSTRAINT "FK_Menus_Stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES public."Stores"("Id") ON DELETE RESTRICT;


--
-- Name: OperationLogs FK_OperationLogs_Accounts_AccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OperationLogs"
    ADD CONSTRAINT "FK_OperationLogs_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: OperationLogs FK_OperationLogs_Devices_DeviceId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OperationLogs"
    ADD CONSTRAINT "FK_OperationLogs_Devices_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES public."Devices"("Id") ON DELETE RESTRICT;


--
-- Name: OperationLogs FK_OperationLogs_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OperationLogs"
    ADD CONSTRAINT "FK_OperationLogs_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: OperationLogs FK_OperationLogs_Orders_OrderId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OperationLogs"
    ADD CONSTRAINT "FK_OperationLogs_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES public."Orders"("Id") ON DELETE RESTRICT;


--
-- Name: OrderExecutionRecords FK_OrderExecutionRecords_ConfigurationReleases_SourceConfigura~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderExecutionRecords"
    ADD CONSTRAINT "FK_OrderExecutionRecords_ConfigurationReleases_SourceConfigura~" FOREIGN KEY ("SourceConfigurationReleaseId") REFERENCES public."ConfigurationReleases"("Id") ON DELETE RESTRICT;


--
-- Name: OrderExecutionRecords FK_OrderExecutionRecords_EdgeCommands_SourceCommandId_KioskExe~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderExecutionRecords"
    ADD CONSTRAINT "FK_OrderExecutionRecords_EdgeCommands_SourceCommandId_KioskExe~" FOREIGN KEY ("SourceCommandId", "KioskExecutionEndpointId") REFERENCES public."EdgeCommands"("Id", "TargetExecutionEndpointId") ON DELETE RESTRICT;


--
-- Name: OrderExecutionRecords FK_OrderExecutionRecords_KioskExecutionEndpoints_KioskExecutio~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderExecutionRecords"
    ADD CONSTRAINT "FK_OrderExecutionRecords_KioskExecutionEndpoints_KioskExecutio~" FOREIGN KEY ("KioskExecutionEndpointId") REFERENCES public."KioskExecutionEndpoints"("Id") ON DELETE RESTRICT;


--
-- Name: OrderItemProductOptions FK_OrderItemProductOptions_OrderItems_OrderItemId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderItemProductOptions"
    ADD CONSTRAINT "FK_OrderItemProductOptions_OrderItems_OrderItemId" FOREIGN KEY ("OrderItemId") REFERENCES public."OrderItems"("Id") ON DELETE RESTRICT;


--
-- Name: OrderItemProductOptions FK_OrderItemProductOptions_ProductOptions_ProductOptionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderItemProductOptions"
    ADD CONSTRAINT "FK_OrderItemProductOptions_ProductOptions_ProductOptionId" FOREIGN KEY ("ProductOptionId") REFERENCES public."ProductOptions"("Id") ON DELETE RESTRICT;


--
-- Name: OrderItems FK_OrderItems_MenuItems_MenuItemId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderItems"
    ADD CONSTRAINT "FK_OrderItems_MenuItems_MenuItemId" FOREIGN KEY ("MenuItemId") REFERENCES public."MenuItems"("Id") ON DELETE RESTRICT;


--
-- Name: OrderItems FK_OrderItems_Orders_OrderId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderItems"
    ADD CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES public."Orders"("Id") ON DELETE RESTRICT;


--
-- Name: OrderItems FK_OrderItems_ProductVariants_ProductVariantId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderItems"
    ADD CONSTRAINT "FK_OrderItems_ProductVariants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES public."ProductVariants"("Id") ON DELETE RESTRICT;


--
-- Name: OrderItems FK_OrderItems_Products_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderItems"
    ADD CONSTRAINT "FK_OrderItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: OrderItems FK_OrderItems_Recipes_RecipeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderItems"
    ADD CONSTRAINT "FK_OrderItems_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES public."Recipes"("Id") ON DELETE RESTRICT;


--
-- Name: OrderStatusHistories FK_OrderStatusHistories_Accounts_ChangedByAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderStatusHistories"
    ADD CONSTRAINT "FK_OrderStatusHistories_Accounts_ChangedByAccountId" FOREIGN KEY ("ChangedByAccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: OrderStatusHistories FK_OrderStatusHistories_Orders_OrderId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OrderStatusHistories"
    ADD CONSTRAINT "FK_OrderStatusHistories_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES public."Orders"("Id") ON DELETE RESTRICT;


--
-- Name: Orders FK_Orders_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Orders"
    ADD CONSTRAINT "FK_Orders_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: Orders FK_Orders_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Orders"
    ADD CONSTRAINT "FK_Orders_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: Orders FK_Orders_Stores_StoreId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Orders"
    ADD CONSTRAINT "FK_Orders_Stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES public."Stores"("Id") ON DELETE RESTRICT;


--
-- Name: PasswordResetRequests FK_PasswordResetRequests_Accounts_AccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PasswordResetRequests"
    ADD CONSTRAINT "FK_PasswordResetRequests_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: PaymentCallbacks FK_PaymentCallbacks_PaymentTransactions_PaymentTransactionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PaymentCallbacks"
    ADD CONSTRAINT "FK_PaymentCallbacks_PaymentTransactions_PaymentTransactionId" FOREIGN KEY ("PaymentTransactionId") REFERENCES public."PaymentTransactions"("Id") ON DELETE RESTRICT;


--
-- Name: PaymentTransactions FK_PaymentTransactions_Orders_OrderId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PaymentTransactions"
    ADD CONSTRAINT "FK_PaymentTransactions_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES public."Orders"("Id") ON DELETE RESTRICT;


--
-- Name: PaymentTransactions FK_PaymentTransactions_PaymentMethods_PaymentMethodId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PaymentTransactions"
    ADD CONSTRAINT "FK_PaymentTransactions_PaymentMethods_PaymentMethodId" FOREIGN KEY ("PaymentMethodId") REFERENCES public."PaymentMethods"("Id") ON DELETE RESTRICT;


--
-- Name: ProductCategories FK_ProductCategories_ProductCategories_ParentCategoryId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductCategories"
    ADD CONSTRAINT "FK_ProductCategories_ProductCategories_ParentCategoryId" FOREIGN KEY ("ParentCategoryId") REFERENCES public."ProductCategories"("Id") ON DELETE RESTRICT;


--
-- Name: ProductOptions FK_ProductOptions_OptionGroups_OptionGroupId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductOptions"
    ADD CONSTRAINT "FK_ProductOptions_OptionGroups_OptionGroupId" FOREIGN KEY ("OptionGroupId") REFERENCES public."OptionGroups"("Id") ON DELETE RESTRICT;


--
-- Name: ProductOptions FK_ProductOptions_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductOptions"
    ADD CONSTRAINT "FK_ProductOptions_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: ProductOptions FK_ProductOptions_ProductOptions_TemplateProductOptionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductOptions"
    ADD CONSTRAINT "FK_ProductOptions_ProductOptions_TemplateProductOptionId" FOREIGN KEY ("TemplateProductOptionId") REFERENCES public."ProductOptions"("Id") ON DELETE RESTRICT;


--
-- Name: ProductProductOptions FK_ProductProductOptions_ProductOptions_ProductOptionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductProductOptions"
    ADD CONSTRAINT "FK_ProductProductOptions_ProductOptions_ProductOptionId" FOREIGN KEY ("ProductOptionId") REFERENCES public."ProductOptions"("Id") ON DELETE RESTRICT;


--
-- Name: ProductProductOptions FK_ProductProductOptions_Products_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductProductOptions"
    ADD CONSTRAINT "FK_ProductProductOptions_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: ProductVariants FK_ProductVariants_Products_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductVariants"
    ADD CONSTRAINT "FK_ProductVariants_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: ProductionEventCheckpoints FK_ProductionEventCheckpoints_KioskExecutionEndpoints_KioskExe~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductionEventCheckpoints"
    ADD CONSTRAINT "FK_ProductionEventCheckpoints_KioskExecutionEndpoints_KioskExe~" FOREIGN KEY ("KioskExecutionEndpointId", "KioskId") REFERENCES public."KioskExecutionEndpoints"("Id", "KioskId") ON DELETE RESTRICT;


--
-- Name: ProductionEventCheckpoints FK_ProductionEventCheckpoints_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductionEventCheckpoints"
    ADD CONSTRAINT "FK_ProductionEventCheckpoints_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: ProductionExecutionRecords FK_ProductionExecutionRecords_EdgeCommands_SourceCommandId_Kio~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductionExecutionRecords"
    ADD CONSTRAINT "FK_ProductionExecutionRecords_EdgeCommands_SourceCommandId_Kio~" FOREIGN KEY ("SourceCommandId", "KioskExecutionEndpointId") REFERENCES public."EdgeCommands"("Id", "TargetExecutionEndpointId") ON DELETE RESTRICT;


--
-- Name: ProductionExecutionRecords FK_ProductionExecutionRecords_KioskExecutionEndpoints_KioskExe~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ProductionExecutionRecords"
    ADD CONSTRAINT "FK_ProductionExecutionRecords_KioskExecutionEndpoints_KioskExe~" FOREIGN KEY ("KioskExecutionEndpointId") REFERENCES public."KioskExecutionEndpoints"("Id") ON DELETE RESTRICT;


--
-- Name: Products FK_Products_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Products"
    ADD CONSTRAINT "FK_Products_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: Products FK_Products_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Products"
    ADD CONSTRAINT "FK_Products_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: Products FK_Products_ProductCategories_CategoryId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Products"
    ADD CONSTRAINT "FK_Products_ProductCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES public."ProductCategories"("Id") ON DELETE RESTRICT;


--
-- Name: Products FK_Products_Products_TemplateProductId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Products"
    ADD CONSTRAINT "FK_Products_Products_TemplateProductId" FOREIGN KEY ("TemplateProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: Products FK_Products_Stores_StoreId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Products"
    ADD CONSTRAINT "FK_Products_Stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES public."Stores"("Id") ON DELETE RESTRICT;


--
-- Name: RecipeItems FK_RecipeItems_Ingredients_IngredientId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RecipeItems"
    ADD CONSTRAINT "FK_RecipeItems_Ingredients_IngredientId" FOREIGN KEY ("IngredientId") REFERENCES public."Ingredients"("Id") ON DELETE RESTRICT;


--
-- Name: RecipeItems FK_RecipeItems_Recipes_RecipeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RecipeItems"
    ADD CONSTRAINT "FK_RecipeItems_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES public."Recipes"("Id") ON DELETE RESTRICT;


--
-- Name: Recipes FK_Recipes_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Recipes"
    ADD CONSTRAINT "FK_Recipes_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: Recipes FK_Recipes_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Recipes"
    ADD CONSTRAINT "FK_Recipes_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: Recipes FK_Recipes_ProductVariants_ProductVariantId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Recipes"
    ADD CONSTRAINT "FK_Recipes_ProductVariants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES public."ProductVariants"("Id") ON DELETE RESTRICT;


--
-- Name: Recipes FK_Recipes_Recipes_TemplateRecipeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Recipes"
    ADD CONSTRAINT "FK_Recipes_Recipes_TemplateRecipeId" FOREIGN KEY ("TemplateRecipeId") REFERENCES public."Recipes"("Id") ON DELETE RESTRICT;


--
-- Name: Recipes FK_Recipes_Stores_StoreId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Recipes"
    ADD CONSTRAINT "FK_Recipes_Stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES public."Stores"("Id") ON DELETE RESTRICT;


--
-- Name: RefreshTokens FK_RefreshTokens_AccountDevices_AccountDeviceId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "FK_RefreshTokens_AccountDevices_AccountDeviceId" FOREIGN KEY ("AccountDeviceId") REFERENCES public."AccountDevices"("Id") ON DELETE RESTRICT;


--
-- Name: RefreshTokens FK_RefreshTokens_Accounts_AccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "FK_RefreshTokens_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: RefreshTokens FK_RefreshTokens_RefreshTokens_ReplacedByTokenId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId" FOREIGN KEY ("ReplacedByTokenId") REFERENCES public."RefreshTokens"("Id") ON DELETE RESTRICT;


--
-- Name: Refunds FK_Refunds_Accounts_RequestedByAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Refunds"
    ADD CONSTRAINT "FK_Refunds_Accounts_RequestedByAccountId" FOREIGN KEY ("RequestedByAccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: Refunds FK_Refunds_PaymentTransactions_PaymentTransactionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Refunds"
    ADD CONSTRAINT "FK_Refunds_PaymentTransactions_PaymentTransactionId" FOREIGN KEY ("PaymentTransactionId") REFERENCES public."PaymentTransactions"("Id") ON DELETE RESTRICT;


--
-- Name: RobotArtifacts FK_RobotArtifacts_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotArtifacts"
    ADD CONSTRAINT "FK_RobotArtifacts_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: RobotArtifacts FK_RobotArtifacts_RobotArtifactTemplates_SourceRobotArtifactTe~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotArtifacts"
    ADD CONSTRAINT "FK_RobotArtifacts_RobotArtifactTemplates_SourceRobotArtifactTe~" FOREIGN KEY ("SourceRobotArtifactTemplateId") REFERENCES public."RobotArtifactTemplates"("Id") ON DELETE RESTRICT;


--
-- Name: RobotProgramArtifacts FK_RobotProgramArtifacts_RobotArtifacts_RobotArtifactId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotProgramArtifacts"
    ADD CONSTRAINT "FK_RobotProgramArtifacts_RobotArtifacts_RobotArtifactId" FOREIGN KEY ("RobotArtifactId") REFERENCES public."RobotArtifacts"("Id") ON DELETE RESTRICT;


--
-- Name: RobotProgramArtifacts FK_RobotProgramArtifacts_RobotPrograms_RobotProgramId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotProgramArtifacts"
    ADD CONSTRAINT "FK_RobotProgramArtifacts_RobotPrograms_RobotProgramId" FOREIGN KEY ("RobotProgramId") REFERENCES public."RobotPrograms"("Id") ON DELETE RESTRICT;


--
-- Name: RobotPrograms FK_RobotPrograms_Devices_DeviceId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotPrograms"
    ADD CONSTRAINT "FK_RobotPrograms_Devices_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES public."Devices"("Id") ON DELETE RESTRICT;


--
-- Name: RobotPrograms FK_RobotPrograms_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotPrograms"
    ADD CONSTRAINT "FK_RobotPrograms_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: RobotPrograms FK_RobotPrograms_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotPrograms"
    ADD CONSTRAINT "FK_RobotPrograms_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: RobotPrograms FK_RobotPrograms_Stores_StoreId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RobotPrograms"
    ADD CONSTRAINT "FK_RobotPrograms_Stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES public."Stores"("Id") ON DELETE RESTRICT;


--
-- Name: StockMovements FK_StockMovements_Accounts_CreatedByAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "FK_StockMovements_Accounts_CreatedByAccountId" FOREIGN KEY ("CreatedByAccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: StockMovements FK_StockMovements_Devices_DeviceId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "FK_StockMovements_Devices_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES public."Devices"("Id") ON DELETE RESTRICT;


--
-- Name: StockMovements FK_StockMovements_IngredientDispenserStates_IngredientDispense~; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "FK_StockMovements_IngredientDispenserStates_IngredientDispense~" FOREIGN KEY ("IngredientDispenserStateId") REFERENCES public."IngredientDispenserStates"("Id") ON DELETE RESTRICT;


--
-- Name: StockMovements FK_StockMovements_Ingredients_IngredientId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "FK_StockMovements_Ingredients_IngredientId" FOREIGN KEY ("IngredientId") REFERENCES public."Ingredients"("Id") ON DELETE RESTRICT;


--
-- Name: StockMovements FK_StockMovements_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "FK_StockMovements_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: StockMovements FK_StockMovements_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "FK_StockMovements_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: StockMovements FK_StockMovements_Stores_StoreId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "FK_StockMovements_Stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES public."Stores"("Id") ON DELETE RESTRICT;


--
-- Name: Stores FK_Stores_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Stores"
    ADD CONSTRAINT "FK_Stores_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: SyncDeadLetterRetryAttempts FK_SyncDeadLetterRetryAttempts_Accounts_RequestedByAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyncDeadLetterRetryAttempts"
    ADD CONSTRAINT "FK_SyncDeadLetterRetryAttempts_Accounts_RequestedByAccountId" FOREIGN KEY ("RequestedByAccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: SyncDeadLetterRetryAttempts FK_SyncDeadLetterRetryAttempts_SyncDeadLetters_SyncDeadLetterId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyncDeadLetterRetryAttempts"
    ADD CONSTRAINT "FK_SyncDeadLetterRetryAttempts_SyncDeadLetters_SyncDeadLetterId" FOREIGN KEY ("SyncDeadLetterId") REFERENCES public."SyncDeadLetters"("Id") ON DELETE RESTRICT;


--
-- Name: SyncDeadLetters FK_SyncDeadLetters_Accounts_ResolvedByAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyncDeadLetters"
    ADD CONSTRAINT "FK_SyncDeadLetters_Accounts_ResolvedByAccountId" FOREIGN KEY ("ResolvedByAccountId") REFERENCES public."Accounts"("Id") ON DELETE RESTRICT;


--
-- Name: SyncDeadLetters FK_SyncDeadLetters_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyncDeadLetters"
    ADD CONSTRAINT "FK_SyncDeadLetters_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- Name: SyncDeadLetters FK_SyncDeadLetters_SyncEventInbox_SyncEventInboxId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyncDeadLetters"
    ADD CONSTRAINT "FK_SyncDeadLetters_SyncEventInbox_SyncEventInboxId" FOREIGN KEY ("SyncEventInboxId") REFERENCES public."SyncEventInbox"("Id") ON DELETE RESTRICT;


--
-- Name: SyncEventInbox FK_SyncEventInbox_Kiosks_KioskId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyncEventInbox"
    ADD CONSTRAINT "FK_SyncEventInbox_Kiosks_KioskId" FOREIGN KEY ("KioskId") REFERENCES public."Kiosks"("Id") ON DELETE RESTRICT;


--
-- PostgreSQL database dump complete
--

\unrestrict ZyoqaYjPylAxkHCQ7VCQQbQ8wEvcKF3nicGJ4xRHfU26piDBOlckBNqeu2r05Pv

