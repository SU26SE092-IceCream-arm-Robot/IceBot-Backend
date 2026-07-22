using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    EmailConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneNumberConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Gender = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LocalLoginEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    GoogleLoginEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    GoogleSubjectId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GoogleEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedLoginCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Category = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequiresKioskAssignment = table.Column<bool>(type: "boolean", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IngredientType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StorageRequirement = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPerishable = table.Column<bool>(type: "boolean", nullable: false),
                    IsAllergen = table.Column<bool>(type: "boolean", nullable: false),
                    ShelfLifeDays = table.Column<int>(type: "integer", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTopologyChangeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispenserStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    BeforeIsActive = table.Column<bool>(type: "boolean", nullable: true),
                    AfterIsActive = table.Column<bool>(type: "boolean", nullable: true),
                    BeforeCapacityQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    AfterCapacityQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    BeforeUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AfterUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTopologyChangeRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MethodType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ConfigJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    ProductType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategories_ProductCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotArtifactTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TemplateName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Checksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentLengthBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotArtifactTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    InvitedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EmailSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InvitedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedByIp = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AcceptedByUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountInvitations_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountNotificationDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PushToken = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    PushTokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InvalidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InvalidationReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountNotificationDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountNotificationDevices_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequestedByIp = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedByUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UsedByIp = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UsedByUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetRequests_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacedByTokenId = table.Column<Guid>(type: "uuid", nullable: true),
                    TokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByIp = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RevokedByIp = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RevokedByUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RevokeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReuseDetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ModelNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FirmwareFamily = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CapabilitiesSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    CapabilitiesJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceModels_DeviceTypes_DeviceTypeId",
                        column: x => x.DeviceTypeId,
                        principalTable: "DeviceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseNumber = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReleaseManifestSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ManifestJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RetiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationReleases", x => x.Id);
                    table.UniqueConstraint("AK_ConfigurationReleases_Id_OrganizationId", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ConfigurationReleases_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StoreType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Province = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Country = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TimeZone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OpeningHoursSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    OpeningHoursJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stores_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRobotArtifactTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    ArtifactCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ArtifactName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Checksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentLengthBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotArtifacts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotArtifacts_RobotArtifactTemplates_SourceRobotArtifactTe~",
                        column: x => x.SourceRobotArtifactTemplateId,
                        principalTable: "RobotArtifactTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountStores",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountStores", x => new { x.AccountId, x.StoreId });
                    table.ForeignKey(
                        name: "FK_AccountStores_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountStores_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kiosks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    KioskType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TimeZone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    InstalledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastOnlineAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SupportsOfflineMode = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigurationVersion = table.Column<long>(type: "bigint", nullable: false),
                    SettingsSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    SettingsJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kiosks", x => x.Id);
                    table.UniqueConstraint("AK_Kiosks_Id_OrganizationId", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Kiosks_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kiosks_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssignedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountRoles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountRoles_Accounts_AssignedByAccountId",
                        column: x => x.AssignedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountRoles_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountRoles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountRoles_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceTypeId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PositionLabel = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FirmwareVersion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InstalledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.UniqueConstraint("AK_Devices_Id_KioskId", x => new { x.Id, x.KioskId });
                    table.ForeignKey(
                        name: "FK_Devices_DeviceModels_DeviceModelId",
                        column: x => x.DeviceModelId,
                        principalTable: "DeviceModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_DeviceTypes_DeviceTypeId",
                        column: x => x.DeviceTypeId,
                        principalTable: "DeviceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KioskHeartbeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    HeartbeatSequence = table.Column<long>(type: "bigint", nullable: true),
                    ReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RobotStatus = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NetworkStatus = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FirmwareVersion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CpuUsagePercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    MemoryUsagePercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DiskUsagePercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    PendingSyncEventCount = table.Column<int>(type: "integer", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskHeartbeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KioskHeartbeats_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScopeType = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    MetadataSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menus_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Menus_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Menus_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClientOrderId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuntimeSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuntimeSnapshotGeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    ExternalChannel = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CustomerPhoneNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PlacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplateProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProductType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    PreparationTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ScopeType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Products_TemplateProductId",
                        column: x => x.TemplateProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncEventInbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    HeadersJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockId = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    MaxProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncEventInbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncEventInbox_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcknowledgedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlertCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CorrelationKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RaisedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastOccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_Accounts_AcknowledgedByAccountId",
                        column: x => x.AcknowledgedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_Devices_DeviceId_KioskId",
                        columns: x => new { x.DeviceId, x.KioskId },
                        principalTable: "Devices",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceEvents_Devices_DeviceId_KioskId",
                        columns: x => new { x.DeviceId, x.KioskId },
                        principalTable: "Devices",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceEvents_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IngredientDispenserStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CurrentLevelStatus = table.Column<int>(type: "integer", nullable: false),
                    EstimatedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    CapacityQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Unit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LevelToQuantityProfileSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    LevelToQuantityProfileJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    LastMeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRefilledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SensorPayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientDispenserStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredientDispenserStates_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngredientDispenserStates_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngredientDispenserStates_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ScopeType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProgramManifestSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ProgramManifestJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ProgramManifestChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotPrograms_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotPrograms_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotPrograms_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotPrograms_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationLogs_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationLogs_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationLogs_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromStatus = table.Column<int>(type: "integer", nullable: true),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_Accounts_ChangedByAccountId",
                        column: x => x.ChangedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethodId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaymentIntentId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProviderOrderCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderPaymentLinkId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    QrCodePayload = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProviderStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderPaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AuthorizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RawRequestJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    RawResponseJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OptionGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectionType = table.Column<int>(type: "integer", nullable: false),
                    MinSelections = table.Column<int>(type: "integer", nullable: false),
                    MaxSelections = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptionGroups_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VariantType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FulfillmentType = table.Column<int>(type: "integer", nullable: false),
                    SizeCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BasePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    PreparationTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncDeadLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncEventInboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ErrorDetails = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncDeadLetters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncDeadLetters_Accounts_ResolvedByAccountId",
                        column: x => x.ResolvedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncDeadLetters_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncDeadLetters_SyncEventInbox_SyncEventInboxId",
                        column: x => x.SyncEventInboxId,
                        principalTable: "SyncEventInbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedToAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    TicketNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IssueCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Accounts_AssignedToAccountId",
                        column: x => x.AssignedToAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_DeviceEvents_DeviceEventId",
                        column: x => x.DeviceEventId,
                        principalTable: "DeviceEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTopologyRebindRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDispenserStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacementDispenserStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacementDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceIngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacementIngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceContainerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReplacementContainerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstimateDisposition = table.Column<int>(type: "integer", nullable: false),
                    PreviousEstimatedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    TransferredQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SourceUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReplacementUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTopologyRebindRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTopologyRebindRecords_IngredientDispenserStates_Re~",
                        column: x => x.ReplacementDispenserStateId,
                        principalTable: "IngredientDispenserStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTopologyRebindRecords_IngredientDispenserStates_So~",
                        column: x => x.SourceDispenserStateId,
                        principalTable: "IngredientDispenserStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientDispenserStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MovementType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    IsEstimated = table.Column<bool>(type: "boolean", nullable: false),
                    Unit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_IngredientDispenserStates_IngredientDispense~",
                        column: x => x.IngredientDispenserStateId,
                        principalTable: "IngredientDispenserStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotProgramArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunOrder = table.Column<int>(type: "integer", nullable: false),
                    ParametersSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotProgramArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotProgramArtifacts_RobotArtifacts_RobotArtifactId",
                        column: x => x.RobotArtifactId,
                        principalTable: "RobotArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotProgramArtifacts_RobotPrograms_RobotProgramId",
                        column: x => x.RobotProgramId,
                        principalTable: "RobotPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentCallbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EventType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    Signature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProcessingStatus = table.Column<int>(type: "integer", nullable: false),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    MaxProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentCallbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentCallbacks_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderRefundId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_Accounts_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionGroupId = table.Column<long>(type: "bigint", nullable: false),
                    TemplateProductOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PriceDelta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOptions_OptionGroups_OptionGroupId",
                        column: x => x.OptionGroupId,
                        principalTable: "OptionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateRecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    YieldQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EstimatedDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InstructionsSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    InstructionsJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ScopeType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recipes_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipes_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipes_Recipes_TemplateRecipeId",
                        column: x => x.TemplateRecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipes_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncDeadLetterRetryAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncDeadLetterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: true),
                    ResultMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncDeadLetterRetryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncDeadLetterRetryAttempts_Accounts_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncDeadLetterRetryAttempts_SyncDeadLetters_SyncDeadLetterId",
                        column: x => x.SyncDeadLetterId,
                        principalTable: "SyncDeadLetters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RequiredCapabilitiesJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionRoutes_ConfigurationReleases_ConfigurationReleaseId",
                        column: x => x.ConfigurationReleaseId,
                        principalTable: "ConfigurationReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionRoutes_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionRoutes_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    PreparationTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MetadataSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItems_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItems_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeItems_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeItems_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionRouteRobotBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionRouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    BindingOrder = table.Column<int>(type: "integer", nullable: false),
                    RequiredWorkcellCapabilityCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RobotProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionRouteRobotBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionRouteRobotBindings_ExecutionRoutes_ExecutionRouteId",
                        column: x => x.ExecutionRouteId,
                        principalTable: "ExecutionRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionRouteRobotBindings_RobotPrograms_RobotProgramId",
                        column: x => x.RobotProgramId,
                        principalTable: "RobotPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemProductOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemProductOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemProductOptions_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItemProductOptions_ProductOptions_ProductOptionId",
                        column: x => x.ProductOptionId,
                        principalTable: "ProductOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientLineId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MenuItemCodeSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MenuItemNameSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductCodeSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductVariantCodeSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductVariantNameSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecipeVersionSnapshot = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RecipeSnapshotSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    RecipeSnapshotJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionGroupId = table.Column<long>(type: "bigint", nullable: false),
                    OptionGroupCodeSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CodeSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NameSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UnitPriceDelta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TotalPriceDelta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemOptions_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ControllerArtifactSetDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    ControllerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActiveSetVersion = table.Column<long>(type: "bigint", nullable: false),
                    ActiveSetChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaxArtifactCount = table.Column<int>(type: "integer", nullable: false),
                    MaxArtifactStorageBytes = table.Column<long>(type: "bigint", nullable: false),
                    RequestedArtifactCount = table.Column<int>(type: "integer", nullable: false),
                    RequestedArtifactStorageBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ControllerReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastControllerReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControllerArtifactSetDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControllerArtifactSetDeployments_ConfigurationReleases_Sour~",
                        columns: x => new { x.SourceConfigurationReleaseId, x.OrganizationId },
                        principalTable: "ConfigurationReleases",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ControllerArtifactSetDeployments_Kiosks_KioskId_Organizatio~",
                        columns: x => new { x.KioskId, x.OrganizationId },
                        principalTable: "Kiosks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ControllerArtifactSetItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ControllerArtifactSetDeploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionRouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotProgramManifestChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RobotArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentLengthBytes = table.Column<long>(type: "bigint", nullable: false),
                    RunOrder = table.Column<int>(type: "integer", nullable: false),
                    ParametersSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControllerArtifactSetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControllerArtifactSetItems_ControllerArtifactSetDeployments~",
                        column: x => x.ControllerArtifactSetDeploymentId,
                        principalTable: "ControllerArtifactSetDeployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EdgeCommandDeliveryAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EdgeCommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryAttemptNo = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    ResponseCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResponseMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdgeCommandDeliveryAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EdgeCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandType = table.Column<int>(type: "integer", nullable: false),
                    DispatchAttemptNo = table.Column<int>(type: "integer", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeploymentKind = table.Column<int>(type: "integer", nullable: true),
                    RollbackTargetDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedCommandExpiryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    CommandExpiryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RejectionMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdgeCommands", x => x.Id);
                    table.UniqueConstraint("AK_EdgeCommands_Id_TargetExecutionEndpointId", x => new { x.Id, x.TargetExecutionEndpointId });
                });

            migrationBuilder.CreateTable(
                name: "EdgeStateSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryKind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateRevision = table.Column<long>(type: "bigint", nullable: false),
                    SummarySchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    EdgeCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdgeStateSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EdgeStateSummaries_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointCapabilityProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionEndpointReadinessProjectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WorkcellCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    UnavailableReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointCapabilityProjections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointCredentialBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "integer", nullable: false),
                    CredentialReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PublicKeyPem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProvisionedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointCredentialBindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KioskExecutionEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndpointCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExecutionProfile = table.Column<int>(type: "integer", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "integer", nullable: false),
                    CredentialBindingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FullEdgeRuntimeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ControllerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProvisionedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveConfigurationDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveConfigurationReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastEdgeActivationEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveConfigurationEdgeReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveConfigurationCloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveArtifactSetDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveArtifactSetReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveArtifactSetReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActiveArtifactSetVersion = table.Column<long>(type: "bigint", nullable: true),
                    ActiveArtifactSetChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastControllerActivationReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveArtifactSetControllerReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveArtifactSetCloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskExecutionEndpoints", x => x.Id);
                    table.UniqueConstraint("AK_KioskExecutionEndpoints_Id_KioskId", x => new { x.Id, x.KioskId });
                    table.CheckConstraint("CK_KioskExecutionEndpoints_ProfileIdentity", "((\"ExecutionProfile\" = 1 AND \"ControllerId\" IS NULL) OR (\"ExecutionProfile\" = 2 AND \"FullEdgeRuntimeId\" IS NULL)) AND (\"Status\" <> 2 OR ((\"ExecutionProfile\" = 1 AND \"FullEdgeRuntimeId\" IS NOT NULL) OR (\"ExecutionProfile\" = 2 AND \"ControllerId\" IS NOT NULL)))");
                    table.ForeignKey(
                        name: "FK_KioskExecutionEndpoints_ExecutionEndpointCredentialBindings~",
                        column: x => x.CredentialBindingId,
                        principalTable: "ExecutionEndpointCredentialBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskExecutionEndpoints_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointMqttCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BrokerProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CredentialVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointMqttCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointMqttCredentials_KioskExecutionEndpoints_Ki~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointReadinessProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateRevision = table.Column<long>(type: "bigint", nullable: false),
                    Readiness = table.Column<int>(type: "integer", nullable: false),
                    Activity = table.Column<int>(type: "integer", nullable: false),
                    Safety = table.Column<int>(type: "integer", nullable: false),
                    CurrentCommandId = table.Column<Guid>(type: "uuid", nullable: true),
                    PhysicalOutputState = table.Column<int>(type: "integer", nullable: false),
                    FaultCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExecutorReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointReadinessProjections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~",
                        columns: x => new { x.KioskExecutionEndpointId, x.KioskId },
                        principalTable: "KioskExecutionEndpoints",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointReadinessProjections_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointRequestNonces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nonce = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointRequestNonces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointRequestNonces_KioskExecutionEndpoints_Kios~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointSupportedRobotTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointSupportedRobotTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointSupportedRobotTargets_Devices_DeviceId_Kio~",
                        columns: x => new { x.DeviceId, x.KioskId },
                        principalTable: "Devices",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                        columns: x => new { x.KioskExecutionEndpointId, x.KioskId },
                        principalTable: "KioskExecutionEndpoints",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KioskConfigurationDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    EdgeRuntimeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AttemptNo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    EdgeReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastEdgeDeploymentEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskConfigurationDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KioskConfigurationDeployments_Accounts_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskConfigurationDeployments_ConfigurationReleases_Configu~",
                        columns: x => new { x.ConfigurationReleaseId, x.OrganizationId },
                        principalTable: "ConfigurationReleases",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~",
                        columns: x => new { x.KioskExecutionEndpointId, x.KioskId },
                        principalTable: "KioskExecutionEndpoints",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskConfigurationDeployments_Kiosks_KioskId_OrganizationId",
                        columns: x => new { x.KioskId, x.OrganizationId },
                        principalTable: "Kiosks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderExecutionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchAttemptNo = table.Column<int>(type: "integer", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionProfile = table.Column<int>(type: "integer", nullable: false),
                    SourceConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ObservationStatus = table.Column<int>(type: "integer", nullable: false),
                    CustomerExecutionStatus = table.Column<int>(type: "integer", nullable: false),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAppliedSourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAppliedSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    LastEdgeCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastExecutorReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderExecutionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderExecutionRecords_ConfigurationReleases_SourceConfigura~",
                        column: x => x.SourceConfigurationReleaseId,
                        principalTable: "ConfigurationReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderExecutionRecords_EdgeCommands_SourceCommandId_KioskExe~",
                        columns: x => new { x.SourceCommandId, x.KioskExecutionEndpointId },
                        principalTable: "EdgeCommands",
                        principalColumns: new[] { "Id", "TargetExecutionEndpointId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderExecutionRecords_KioskExecutionEndpoints_KioskExecutio~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionEventCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastContiguousSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    LastContiguousEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionEventCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionEventCheckpoints_KioskExecutionEndpoints_KioskExe~",
                        columns: x => new { x.KioskExecutionEndpointId, x.KioskId },
                        principalTable: "KioskExecutionEndpoints",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionEventCheckpoints_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionExecutionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionProfile = table.Column<int>(type: "integer", nullable: false),
                    SourceProductionJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkcellId = table.Column<Guid>(type: "uuid", nullable: true),
                    ControllerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecutionPlanChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActiveSetVersion = table.Column<long>(type: "bigint", nullable: true),
                    ActiveSetChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PhysicalOutputState = table.Column<int>(type: "integer", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAppliedSourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAppliedSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    LastEdgeCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastExecutorReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionExecutionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionExecutionRecords_EdgeCommands_SourceCommandId_Kio~",
                        columns: x => new { x.SourceCommandId, x.KioskExecutionEndpointId },
                        principalTable: "EdgeCommands",
                        principalColumns: new[] { "Id", "TargetExecutionEndpointId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionExecutionRecords_KioskExecutionEndpoints_KioskExe~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountInvitations_AccountId_InvitedAt",
                table: "AccountInvitations",
                columns: new[] { "AccountId", "InvitedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountInvitations_TokenHash",
                table: "AccountInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountNotificationDevices_AccountId_InstallationId",
                table: "AccountNotificationDevices",
                columns: new[] { "AccountId", "InstallationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountNotificationDevices_AccountId_InvalidatedAt",
                table: "AccountNotificationDevices",
                columns: new[] { "AccountId", "InvalidatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountNotificationDevices_PushTokenHash",
                table: "AccountNotificationDevices",
                column: "PushTokenHash",
                unique: true,
                filter: "\"PushTokenHash\" IS NOT NULL AND \"InvalidatedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_AccountId_RoleId_OrganizationId_StoreId_KioskId",
                table: "AccountRoles",
                columns: new[] { "AccountId", "RoleId", "OrganizationId", "StoreId", "KioskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_AssignedByAccountId",
                table: "AccountRoles",
                column: "AssignedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_KioskId",
                table: "AccountRoles",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_OrganizationId_StoreId_KioskId",
                table: "AccountRoles",
                columns: new[] { "OrganizationId", "StoreId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_RoleId",
                table: "AccountRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_StoreId",
                table: "AccountRoles",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                table: "Accounts",
                column: "Email",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_GoogleEmail",
                table: "Accounts",
                column: "GoogleEmail",
                filter: "\"GoogleEmail\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_GoogleSubjectId",
                table: "Accounts",
                column: "GoogleSubjectId",
                unique: true,
                filter: "\"GoogleSubjectId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserName",
                table: "Accounts",
                column: "UserName",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountStores_StoreId",
                table: "AccountStores",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AcknowledgedByAccountId",
                table: "Alerts",
                column: "AcknowledgedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DeviceId_KioskId",
                table: "Alerts",
                columns: new[] { "DeviceId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_KioskId_DeviceId_CorrelationKey_Status_LastOccurredAt",
                table: "Alerts",
                columns: new[] { "KioskId", "DeviceId", "CorrelationKey", "Status", "LastOccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_KioskId_Status_RaisedAt",
                table: "Alerts",
                columns: new[] { "KioskId", "Status", "RaisedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_OriginNodeId_Version",
                table: "Alerts",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationReleases_Id_OrganizationId",
                table: "ConfigurationReleases",
                columns: new[] { "Id", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationReleases_OrganizationId_ReleaseNumber",
                table: "ConfigurationReleases",
                columns: new[] { "OrganizationId", "ReleaseNumber" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationReleases_ReleaseChecksum",
                table: "ConfigurationReleases",
                column: "ReleaseChecksum",
                unique: true,
                filter: "\"ReleaseChecksum\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationReleases_Status_PublishedAt",
                table: "ConfigurationReleases",
                columns: new[] { "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_ControllerId_ActiveSetVers~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "ControllerId", "ActiveSetVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_ControllerId_LastControlle~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "ControllerId", "LastControllerReportId" },
                unique: true,
                filter: "\"LastControllerReportId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_I~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskExecutionEndpointId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_K~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_S~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskExecutionEndpointId", "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskId_OrganizationId",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_SourceConfigurationRelease~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "SourceConfigurationReleaseId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetItems_ControllerArtifactSetDeploymentI~",
                table: "ControllerArtifactSetItems",
                columns: new[] { "ControllerArtifactSetDeploymentId", "ExecutionRouteId", "RobotProgramId", "RunOrder", "RobotArtifactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_DeviceId_KioskId",
                table: "DeviceEvents",
                columns: new[] { "DeviceId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_DeviceId_OccurredAt",
                table: "DeviceEvents",
                columns: new[] { "DeviceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_EventId",
                table: "DeviceEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_KioskId_OccurredAt",
                table: "DeviceEvents",
                columns: new[] { "KioskId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_OriginNodeId_Version",
                table: "DeviceEvents",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModels_DeviceTypeId_Code",
                table: "DeviceModels",
                columns: new[] { "DeviceTypeId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceModelId",
                table: "Devices",
                column: "DeviceModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceTypeId",
                table: "Devices",
                column: "DeviceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Id_KioskId",
                table: "Devices",
                columns: new[] { "Id", "KioskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_KioskId_Code",
                table: "Devices",
                columns: new[] { "KioskId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_SerialNumber",
                table: "Devices",
                column: "SerialNumber",
                unique: true,
                filter: "\"SerialNumber\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTypes_Code",
                table: "DeviceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCommandDeliveryAttempts_EdgeCommandId_DeliveryAttemptNo",
                table: "EdgeCommandDeliveryAttempts",
                columns: new[] { "EdgeCommandId", "DeliveryAttemptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCommands_DeploymentId",
                table: "EdgeCommands",
                column: "DeploymentId",
                unique: true,
                filter: "\"DeploymentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCommands_OrderId_DispatchAttemptNo",
                table: "EdgeCommands",
                columns: new[] { "OrderId", "DispatchAttemptNo" },
                unique: true,
                filter: "\"OrderId\" IS NOT NULL AND \"DispatchAttemptNo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCommands_TargetExecutionEndpointId_KioskId",
                table: "EdgeCommands",
                columns: new[] { "TargetExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCommands_TargetExecutionEndpointId_Status_CreatedAt",
                table: "EdgeCommands",
                columns: new[] { "TargetExecutionEndpointId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EdgeStateSummaries_KioskExecutionEndpointId_KioskId",
                table: "EdgeStateSummaries",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_EdgeStateSummaries_KioskId",
                table: "EdgeStateSummaries",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_EdgeStateSummaries_SourceExecutorId_SummaryKind",
                table: "EdgeStateSummaries",
                columns: new[] { "SourceExecutorId", "SummaryKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointCapabilityProjections_ExecutionEndpointRea~",
                table: "ExecutionEndpointCapabilityProjections",
                columns: new[] { "ExecutionEndpointReadinessProjectionId", "CapabilityCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointCredentialBindings_CredentialReference",
                table: "ExecutionEndpointCredentialBindings",
                column: "CredentialReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointCredentialBindings_KioskExecutionEndpointI~",
                table: "ExecutionEndpointCredentialBindings",
                columns: new[] { "KioskExecutionEndpointId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointMqttCredentials_KioskExecutionEndpointId",
                table: "ExecutionEndpointMqttCredentials",
                column: "KioskExecutionEndpointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointMqttCredentials_Username",
                table: "ExecutionEndpointMqttCredentials",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointReadinessProjections_KioskExecutionEndpoi~1",
                table: "ExecutionEndpointReadinessProjections",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~",
                table: "ExecutionEndpointReadinessProjections",
                column: "KioskExecutionEndpointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointReadinessProjections_KioskId_Readiness_Act~",
                table: "ExecutionEndpointReadinessProjections",
                columns: new[] { "KioskId", "Readiness", "Activity" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointRequestNonces_ExpiresAt",
                table: "ExecutionEndpointRequestNonces",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointRequestNonces_KioskExecutionEndpointId_Non~",
                table: "ExecutionEndpointRequestNonces",
                columns: new[] { "KioskExecutionEndpointId", "Nonce" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_DeviceId_KioskId",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "DeviceId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~1",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "KioskExecutionEndpointId", "RuntimeTargetCode", "MachineModelCode" },
                unique: true,
                filter: "\"DeviceId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~2",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "KioskExecutionEndpointId", "RuntimeTargetCode", "MachineModelCode", "DeviceId" },
                unique: true,
                filter: "\"DeviceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRouteRobotBindings_ExecutionRouteId_BindingOrder",
                table: "ExecutionRouteRobotBindings",
                columns: new[] { "ExecutionRouteId", "BindingOrder" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRouteRobotBindings_RobotProgramId",
                table: "ExecutionRouteRobotBindings",
                column: "RobotProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRoutes_ConfigurationReleaseId_ProductVariantId_Rec~",
                table: "ExecutionRoutes",
                columns: new[] { "ConfigurationReleaseId", "ProductVariantId", "RecipeId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRoutes_ConfigurationReleaseId_RouteCode",
                table: "ExecutionRoutes",
                columns: new[] { "ConfigurationReleaseId", "RouteCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRoutes_ProductVariantId",
                table: "ExecutionRoutes",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRoutes_RecipeId",
                table: "ExecutionRoutes",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientDispenserStates_DeviceId_ContainerCode",
                table: "IngredientDispenserStates",
                columns: new[] { "DeviceId", "ContainerCode" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientDispenserStates_IngredientId",
                table: "IngredientDispenserStates",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientDispenserStates_KioskId",
                table: "IngredientDispenserStates",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientDispenserStates_OriginNodeId_Version",
                table: "IngredientDispenserStates",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_Code",
                table: "Ingredients",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTopologyChangeRecords_DispenserStateId_CreatedAt",
                table: "InventoryTopologyChangeRecords",
                columns: new[] { "DispenserStateId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTopologyChangeRecords_KioskId_CreatedAt",
                table: "InventoryTopologyChangeRecords",
                columns: new[] { "KioskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTopologyRebindRecords_KioskId_CreatedAt",
                table: "InventoryTopologyRebindRecords",
                columns: new[] { "KioskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTopologyRebindRecords_ReplacementDispenserStateId",
                table: "InventoryTopologyRebindRecords",
                column: "ReplacementDispenserStateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTopologyRebindRecords_SourceDispenserStateId",
                table: "InventoryTopologyRebindRecords",
                column: "SourceDispenserStateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_ConfigurationReleaseId_Organi~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "ConfigurationReleaseId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Idem~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskExecutionEndpointId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Kios~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Stat~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskExecutionEndpointId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskId",
                table: "KioskConfigurationDeployments",
                column: "KioskId",
                unique: true,
                filter: "\"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskId_ConfigurationReleaseI~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskId", "ConfigurationReleaseId", "AttemptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskId_OrganizationId",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskId_Status_RequestedAt",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskId", "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_RequestedByAccountId",
                table: "KioskConfigurationDeployments",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_ControllerId",
                table: "KioskExecutionEndpoints",
                column: "ControllerId",
                unique: true,
                filter: "\"ControllerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_CredentialBindingId",
                table: "KioskExecutionEndpoints",
                column: "CredentialBindingId",
                unique: true,
                filter: "\"CredentialBindingId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_FullEdgeRuntimeId",
                table: "KioskExecutionEndpoints",
                column: "FullEdgeRuntimeId",
                unique: true,
                filter: "\"FullEdgeRuntimeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_Id_KioskId",
                table: "KioskExecutionEndpoints",
                columns: new[] { "Id", "KioskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_KioskId_EndpointCode",
                table: "KioskExecutionEndpoints",
                columns: new[] { "KioskId", "EndpointCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_LastControllerActivationReportId",
                table: "KioskExecutionEndpoints",
                column: "LastControllerActivationReportId",
                unique: true,
                filter: "\"LastControllerActivationReportId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_LastEdgeActivationEventId",
                table: "KioskExecutionEndpoints",
                column: "LastEdgeActivationEventId",
                unique: true,
                filter: "\"LastEdgeActivationEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskHeartbeats_KioskId_NodeId_HeartbeatSequence",
                table: "KioskHeartbeats",
                columns: new[] { "KioskId", "NodeId", "HeartbeatSequence" },
                unique: true,
                filter: "\"HeartbeatSequence\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskHeartbeats_KioskId_ReportedAt",
                table: "KioskHeartbeats",
                columns: new[] { "KioskId", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskHeartbeats_OriginNodeId_Version",
                table: "KioskHeartbeats",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_Kiosks_Id_OrganizationId",
                table: "Kiosks",
                columns: new[] { "Id", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kiosks_OrganizationId",
                table: "Kiosks",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Kiosks_OrganizationId_Code",
                table: "Kiosks",
                columns: new[] { "OrganizationId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Kiosks_SerialNumber",
                table: "Kiosks",
                column: "SerialNumber",
                unique: true,
                filter: "\"SerialNumber\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Kiosks_StoreId",
                table: "Kiosks",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_AssignedToAccountId",
                table: "MaintenanceTickets",
                column: "AssignedToAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_CreatedByAccountId",
                table: "MaintenanceTickets",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_DeviceEventId",
                table: "MaintenanceTickets",
                column: "DeviceEventId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_DeviceId",
                table: "MaintenanceTickets",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_KioskId",
                table: "MaintenanceTickets",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_OrderId",
                table: "MaintenanceTickets",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_OrganizationId",
                table: "MaintenanceTickets",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_OrganizationId_StoreId_KioskId_Status_Re~",
                table: "MaintenanceTickets",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "Status", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_OriginNodeId_Version",
                table: "MaintenanceTickets",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_StoreId",
                table: "MaintenanceTickets",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_TicketNumber",
                table: "MaintenanceTickets",
                column: "TicketNumber",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemProductOptions_MenuItemId_ProductOptionId",
                table: "MenuItemProductOptions",
                columns: new[] { "MenuItemId", "ProductOptionId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemProductOptions_ProductOptionId",
                table: "MenuItemProductOptions",
                column: "ProductOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuId_Code",
                table: "MenuItems",
                columns: new[] { "MenuId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuId_Status_DisplayOrder",
                table: "MenuItems",
                columns: new[] { "MenuId", "Status", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_ProductId",
                table: "MenuItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_ProductVariantId",
                table: "MenuItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_RecipeId",
                table: "MenuItems",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_KioskId",
                table: "Menus",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_OrganizationId",
                table: "Menus",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_OrganizationId_StoreId_KioskId_Code",
                table: "Menus",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_OrganizationId_StoreId_KioskId_Status",
                table: "Menus",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Menus_StoreId",
                table: "Menus",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_AccountId",
                table: "OperationLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_DeviceId",
                table: "OperationLogs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_KioskId_OccurredAt",
                table: "OperationLogs",
                columns: new[] { "KioskId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_OrderId",
                table: "OperationLogs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_OriginNodeId_Version",
                table: "OperationLogs",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_OptionGroups_ProductId_Code",
                table: "OptionGroups",
                columns: new[] { "ProductId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_KioskExecutionEndpointId_SourceExecut~",
                table: "OrderExecutionRecords",
                columns: new[] { "KioskExecutionEndpointId", "SourceExecutorId", "LastAppliedSourceEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_KioskExecutionEndpointId_Status_LastE~",
                table: "OrderExecutionRecords",
                columns: new[] { "KioskExecutionEndpointId", "Status", "LastExecutorReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_OrderId_CloudReceivedAt",
                table: "OrderExecutionRecords",
                columns: new[] { "OrderId", "CloudReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_SourceCommandId",
                table: "OrderExecutionRecords",
                column: "SourceCommandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_SourceCommandId_KioskExecutionEndpoin~",
                table: "OrderExecutionRecords",
                columns: new[] { "SourceCommandId", "KioskExecutionEndpointId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_SourceConfigurationReleaseId",
                table: "OrderExecutionRecords",
                column: "SourceConfigurationReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemOptions_OrderItemId_OptionGroupId",
                table: "OrderItemOptions",
                columns: new[] { "OrderItemId", "OptionGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemOptions_OrderItemId_ProductOptionId",
                table: "OrderItemOptions",
                columns: new[] { "OrderItemId", "ProductOptionId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_MenuItemId",
                table: "OrderItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ClientLineId",
                table: "OrderItems",
                columns: new[] { "OrderId", "ClientLineId" },
                unique: true,
                filter: "\"ClientLineId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductVariantId",
                table: "OrderItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_RecipeId",
                table: "OrderItems",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IdempotencyKey",
                table: "Orders",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_KioskId_ClientOrderId",
                table: "Orders",
                columns: new[] { "KioskId", "ClientOrderId" },
                unique: true,
                filter: "\"ClientOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrganizationId",
                table: "Orders",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrganizationId_StoreId_KioskId_PlacedAt",
                table: "Orders",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "PlacedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StoreId",
                table: "Orders",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_ChangedByAccountId",
                table: "OrderStatusHistories",
                column: "ChangedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_OrderId_ChangedAt",
                table: "OrderStatusHistories",
                columns: new[] { "OrderId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Code",
                table: "Organizations",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_AccountId_RequestedAt",
                table: "PasswordResetRequests",
                columns: new[] { "AccountId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_TokenHash",
                table: "PasswordResetRequests",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCallbacks_PaymentTransactionId",
                table: "PaymentCallbacks",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCallbacks_Provider_ProviderEventId",
                table: "PaymentCallbacks",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true,
                filter: "\"ProviderEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Code",
                table: "PaymentMethods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_IdempotencyKey",
                table: "PaymentTransactions",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrderId",
                table: "PaymentTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentMethodId",
                table: "PaymentTransactions",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ProviderOrderCode",
                table: "PaymentTransactions",
                column: "ProviderOrderCode",
                filter: "\"ProviderOrderCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ProviderTransactionId",
                table: "PaymentTransactions",
                column: "ProviderTransactionId",
                filter: "\"ProviderTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_TransactionNumber",
                table: "PaymentTransactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Code",
                table: "ProductCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_ParentCategoryId",
                table: "ProductCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEventCheckpoints_KioskExecutionEndpointId_KioskId",
                table: "ProductionEventCheckpoints",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEventCheckpoints_KioskId",
                table: "ProductionEventCheckpoints",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEventCheckpoints_SourceExecutorId",
                table: "ProductionEventCheckpoints",
                column: "SourceExecutorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_KioskExecutionEndpointId_SourceE~",
                table: "ProductionExecutionRecords",
                columns: new[] { "KioskExecutionEndpointId", "SourceExecutorId", "LastAppliedSourceEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_KioskExecutionEndpointId_Status_~",
                table: "ProductionExecutionRecords",
                columns: new[] { "KioskExecutionEndpointId", "Status", "LastExecutorReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId_KioskExecutionEn~",
                table: "ProductionExecutionRecords",
                columns: new[] { "SourceCommandId", "KioskExecutionEndpointId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId_SourceProduction~",
                table: "ProductionExecutionRecords",
                columns: new[] { "SourceCommandId", "SourceProductionJobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_OptionGroupId",
                table: "ProductOptions",
                column: "OptionGroupId",
                unique: true,
                filter: "\"IsDefault\" = TRUE AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_OptionGroupId_Code",
                table: "ProductOptions",
                columns: new[] { "OptionGroupId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_KioskId",
                table: "Products",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_OrganizationId",
                table: "Products",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_OrganizationId_StoreId_KioskId_Code",
                table: "Products",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId",
                table: "Products",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TemplateProductId",
                table: "Products",
                column: "TemplateProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_Code",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_DisplayOrder",
                table: "ProductVariants",
                columns: new[] { "ProductId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeItems_IngredientId",
                table: "RecipeItems",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeItems_RecipeId_IngredientId_StepOrder",
                table: "RecipeItems",
                columns: new[] { "RecipeId", "IngredientId", "StepOrder" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_KioskId",
                table: "Recipes",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_OrganizationId",
                table: "Recipes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_OrganizationId_StoreId_KioskId_ProductVariantId_Cod~",
                table: "Recipes",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "ProductVariantId", "Code", "Version" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ProductVariantId",
                table: "Recipes",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ProductVariantId_Default",
                table: "Recipes",
                column: "ProductVariantId",
                unique: true,
                filter: "\"IsDefault\" = TRUE AND \"Status\" <> 4 AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_StoreId",
                table: "Recipes",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_TemplateRecipeId",
                table: "Recipes",
                column: "TemplateRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AccountId",
                table: "RefreshTokens",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenId",
                table: "RefreshTokens",
                column: "ReplacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_IdempotencyKey",
                table: "Refunds",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PaymentTransactionId",
                table: "Refunds",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_ProviderRefundId",
                table: "Refunds",
                column: "ProviderRefundId",
                filter: "\"ProviderRefundId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RefundNumber",
                table: "Refunds",
                column: "RefundNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RequestedByAccountId",
                table: "Refunds",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_OrganizationId_ArtifactCode_Checksum",
                table: "RobotArtifacts",
                columns: new[] { "OrganizationId", "ArtifactCode", "Checksum" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_OriginNodeId_Version",
                table: "RobotArtifacts",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_RuntimeTargetCode_MachineModelCode_Status",
                table: "RobotArtifacts",
                columns: new[] { "RuntimeTargetCode", "MachineModelCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_SourceRobotArtifactTemplateId",
                table: "RobotArtifacts",
                column: "SourceRobotArtifactTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_StorageKey",
                table: "RobotArtifacts",
                column: "StorageKey",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTemplates_RuntimeTargetCode_MachineModelCode_S~",
                table: "RobotArtifactTemplates",
                columns: new[] { "RuntimeTargetCode", "MachineModelCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTemplates_StorageKey",
                table: "RobotArtifactTemplates",
                column: "StorageKey",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTemplates_TemplateCode_Checksum",
                table: "RobotArtifactTemplates",
                columns: new[] { "TemplateCode", "Checksum" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotProgramArtifacts_OriginNodeId_Version",
                table: "RobotProgramArtifacts",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotProgramArtifacts_RobotArtifactId",
                table: "RobotProgramArtifacts",
                column: "RobotArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotProgramArtifacts_RobotProgramId_RunOrder",
                table: "RobotProgramArtifacts",
                columns: new[] { "RobotProgramId", "RunOrder" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_DeviceId",
                table: "RobotPrograms",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_KioskId",
                table: "RobotPrograms",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_OrganizationId",
                table: "RobotPrograms",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_OrganizationId_StoreId_KioskId_DeviceId_Code",
                table: "RobotPrograms",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "DeviceId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_OriginNodeId_Version",
                table: "RobotPrograms",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_ProgramManifestChecksum",
                table: "RobotPrograms",
                column: "ProgramManifestChecksum",
                unique: true,
                filter: "\"ProgramManifestChecksum\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_StoreId",
                table: "RobotPrograms",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedByAccountId",
                table: "StockMovements",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_DeviceId",
                table: "StockMovements",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_IngredientDispenserStateId",
                table: "StockMovements",
                column: "IngredientDispenserStateId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_IngredientId",
                table: "StockMovements",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_KioskId",
                table: "StockMovements",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_OrganizationId",
                table: "StockMovements",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_OrganizationId_StoreId_KioskId_OccurredAt",
                table: "StockMovements",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_OriginNodeId_Version",
                table: "StockMovements",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SourceEventId",
                table: "StockMovements",
                column: "SourceEventId",
                unique: true,
                filter: "\"SourceEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StoreId",
                table: "StockMovements",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_OrganizationId_Code",
                table: "Stores",
                columns: new[] { "OrganizationId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SyncDeadLetterRetryAttempts_RequestedByAccountId",
                table: "SyncDeadLetterRetryAttempts",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncDeadLetterRetryAttempts_SyncDeadLetterId_AttemptNumber",
                table: "SyncDeadLetterRetryAttempts",
                columns: new[] { "SyncDeadLetterId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncDeadLetters_EventId",
                table: "SyncDeadLetters",
                column: "EventId",
                filter: "\"EventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SyncDeadLetters_KioskId",
                table: "SyncDeadLetters",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncDeadLetters_ResolvedByAccountId",
                table: "SyncDeadLetters",
                column: "ResolvedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncDeadLetters_Status_FailedAt",
                table: "SyncDeadLetters",
                columns: new[] { "Status", "FailedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncDeadLetters_SyncEventInboxId",
                table: "SyncDeadLetters",
                column: "SyncEventInboxId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncEventInbox_KioskId",
                table: "SyncEventInbox",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncEventInbox_SourceNodeId_EventId",
                table: "SyncEventInbox",
                columns: new[] { "SourceNodeId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncEventInbox_SourceNodeId_EventType_OccurredAt",
                table: "SyncEventInbox",
                columns: new[] { "SourceNodeId", "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncEventInbox_SourceNodeId_SequenceNumber",
                table: "SyncEventInbox",
                columns: new[] { "SourceNodeId", "SequenceNumber" },
                unique: true,
                filter: "\"SequenceNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SyncEventInbox_Status_NextRetryAt_LockedUntil",
                table: "SyncEventInbox",
                columns: new[] { "Status", "NextRetryAt", "LockedUntil" });

            migrationBuilder.AddForeignKey(
                name: "FK_ControllerArtifactSetDeployments_KioskExecutionEndpoints_Ki~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" },
                principalTable: "KioskExecutionEndpoints",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EdgeCommandDeliveryAttempts_EdgeCommands_EdgeCommandId",
                table: "EdgeCommandDeliveryAttempts",
                column: "EdgeCommandId",
                principalTable: "EdgeCommands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EdgeCommands_KioskExecutionEndpoints_TargetExecutionEndpoin~",
                table: "EdgeCommands",
                columns: new[] { "TargetExecutionEndpointId", "KioskId" },
                principalTable: "KioskExecutionEndpoints",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EdgeStateSummaries_KioskExecutionEndpoints_KioskExecutionEn~",
                table: "EdgeStateSummaries",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" },
                principalTable: "KioskExecutionEndpoints",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionEndpointCapabilityProjections_ExecutionEndpointRea~",
                table: "ExecutionEndpointCapabilityProjections",
                column: "ExecutionEndpointReadinessProjectionId",
                principalTable: "ExecutionEndpointReadinessProjections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionEndpointCredentialBindings_KioskExecutionEndpoints~",
                table: "ExecutionEndpointCredentialBindings",
                column: "KioskExecutionEndpointId",
                principalTable: "KioskExecutionEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KioskExecutionEndpoints_Kiosks_KioskId",
                table: "KioskExecutionEndpoints");

            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionEndpointCredentialBindings_KioskExecutionEndpoints~",
                table: "ExecutionEndpointCredentialBindings");

            migrationBuilder.DropTable(
                name: "AccountInvitations");

            migrationBuilder.DropTable(
                name: "AccountNotificationDevices");

            migrationBuilder.DropTable(
                name: "AccountRoles");

            migrationBuilder.DropTable(
                name: "AccountStores");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "ControllerArtifactSetItems");

            migrationBuilder.DropTable(
                name: "EdgeCommandDeliveryAttempts");

            migrationBuilder.DropTable(
                name: "EdgeStateSummaries");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointCapabilityProjections");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointMqttCredentials");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointRequestNonces");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.DropTable(
                name: "ExecutionRouteRobotBindings");

            migrationBuilder.DropTable(
                name: "InventoryTopologyChangeRecords");

            migrationBuilder.DropTable(
                name: "InventoryTopologyRebindRecords");

            migrationBuilder.DropTable(
                name: "KioskConfigurationDeployments");

            migrationBuilder.DropTable(
                name: "KioskHeartbeats");

            migrationBuilder.DropTable(
                name: "MaintenanceTickets");

            migrationBuilder.DropTable(
                name: "MenuItemProductOptions");

            migrationBuilder.DropTable(
                name: "OperationLogs");

            migrationBuilder.DropTable(
                name: "OrderExecutionRecords");

            migrationBuilder.DropTable(
                name: "OrderItemOptions");

            migrationBuilder.DropTable(
                name: "OrderStatusHistories");

            migrationBuilder.DropTable(
                name: "PasswordResetRequests");

            migrationBuilder.DropTable(
                name: "PaymentCallbacks");

            migrationBuilder.DropTable(
                name: "ProductionEventCheckpoints");

            migrationBuilder.DropTable(
                name: "ProductionExecutionRecords");

            migrationBuilder.DropTable(
                name: "RecipeItems");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "RobotProgramArtifacts");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "SyncDeadLetterRetryAttempts");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ControllerArtifactSetDeployments");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointReadinessProjections");

            migrationBuilder.DropTable(
                name: "ExecutionRoutes");

            migrationBuilder.DropTable(
                name: "DeviceEvents");

            migrationBuilder.DropTable(
                name: "ProductOptions");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "EdgeCommands");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "RobotArtifacts");

            migrationBuilder.DropTable(
                name: "RobotPrograms");

            migrationBuilder.DropTable(
                name: "IngredientDispenserStates");

            migrationBuilder.DropTable(
                name: "SyncDeadLetters");

            migrationBuilder.DropTable(
                name: "ConfigurationReleases");

            migrationBuilder.DropTable(
                name: "OptionGroups");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "RobotArtifactTemplates");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Ingredients");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "SyncEventInbox");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "DeviceModels");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "DeviceTypes");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropTable(
                name: "Kiosks");

            migrationBuilder.DropTable(
                name: "Stores");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "KioskExecutionEndpoints");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointCredentialBindings");
        }
    }
}
