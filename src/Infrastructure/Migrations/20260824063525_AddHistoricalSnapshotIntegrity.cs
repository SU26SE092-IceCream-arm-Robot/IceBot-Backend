using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricalSnapshotIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE evidence_count bigint;
                BEGIN
                    SELECT count(*)
                    INTO evidence_count
                    FROM "PaymentTransactions"
                    WHERE "RawRequestJson" IS NOT NULL OR "RawResponseJson" IS NOT NULL;

                    RAISE NOTICE 'Historical snapshot migration: dropping raw payment evidence from % payment transaction(s).', evidence_count;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "RawRequestJson",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "RawResponseJson",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<int>(
                name: "PreparationTimeSecondsSnapshot",
                table: "OrderItems",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "OrderItems" AS item
                SET "PreparationTimeSecondsSnapshot" = CASE
                    WHEN menu_item."PreparationTimeSeconds" IS NOT NULL
                        THEN NULLIF(menu_item."PreparationTimeSeconds", 0)
                    WHEN variant."PreparationTimeSeconds" IS NOT NULL
                        THEN NULLIF(variant."PreparationTimeSeconds", 0)
                    WHEN product."PreparationTimeSeconds" IS NOT NULL
                        THEN NULLIF(product."PreparationTimeSeconds", 0)
                    ELSE NULL
                END
                FROM "MenuItems" AS menu_item,
                     "ProductVariants" AS variant,
                     "Products" AS product
                WHERE item."MenuItemId" = menu_item."Id"
                  AND item."ProductVariantId" = variant."Id"
                  AND item."ProductId" = product."Id"
                  AND item."PreparationTimeSecondsSnapshot" IS NULL;
                """);

            migrationBuilder.Sql("""
                DO $$
                DECLARE invalid_command_count bigint;
                BEGIN
                    SELECT count(*)
                    INTO invalid_command_count
                    FROM "EdgeCommands" AS command
                    WHERE command."CommandType" = 1
                      AND (
                          command."PayloadJson" @> '{"SchemaVersion":5}'::jsonb
                          OR command."PayloadJson" @> '{"SchemaVersion":"5"}'::jsonb)
                      AND (
                          jsonb_typeof(command."PayloadJson") <> 'object'
                          OR jsonb_typeof(command."PayloadJson" -> 'ReleaseManifestSchemaVersion') <> 'number'
                          OR COALESCE(command."PayloadJson" ->> 'ReleaseManifestSchemaVersion', '') !~ '^[1-9][0-9]*$'
                          OR jsonb_typeof(command."PayloadJson" -> 'ManifestJson') <> 'string'
                          OR COALESCE(NULLIF(command."PayloadJson" ->> 'ManifestJson', ''), '') = ''
                          OR jsonb_typeof(command."PayloadJson" -> 'ReleaseChecksum') <> 'string'
                          OR COALESCE(NULLIF(command."PayloadJson" ->> 'ReleaseChecksum', ''), '') = ''
                          OR jsonb_typeof(command."PayloadJson" -> 'OrderLines') <> 'array'
                          OR CASE
                              WHEN jsonb_typeof(command."PayloadJson" -> 'OrderLines') = 'array'
                                  THEN jsonb_array_length(command."PayloadJson" -> 'OrderLines') = 0
                              ELSE true
                          END
                          OR EXISTS (
                              SELECT 1
                              FROM jsonb_array_elements(
                                  CASE
                                      WHEN jsonb_typeof(command."PayloadJson" -> 'OrderLines') = 'array'
                                          THEN command."PayloadJson" -> 'OrderLines'
                                      ELSE '[]'::jsonb
                                  END) AS lines(line)
                              WHERE jsonb_typeof(line -> 'ProductCodeSnapshot') <> 'string'
                                 OR COALESCE(NULLIF(line ->> 'ProductCodeSnapshot', ''), '') = ''
                                 OR jsonb_typeof(line -> 'ProductVariantCodeSnapshot') <> 'string'
                                 OR COALESCE(NULLIF(line ->> 'ProductVariantCodeSnapshot', ''), '') = ''
                                 OR jsonb_typeof(line -> 'RouteCode') <> 'string'
                                 OR COALESCE(NULLIF(line ->> 'RouteCode', ''), '') = ''
                                 OR jsonb_typeof(line -> 'ProductionDefinitionChecksum') <> 'string'
                                 OR COALESCE(NULLIF(line ->> 'ProductionDefinitionChecksum', ''), '') = ''
                                 OR (
                                     line -> 'RecipeId' IS NOT NULL
                                     AND jsonb_typeof(line -> 'RecipeId') <> 'null'
                                     AND (
                                         jsonb_typeof(line -> 'RecipeSnapshotSchemaVersion') <> 'number'
                                         OR COALESCE(line ->> 'RecipeSnapshotSchemaVersion', '') !~ '^[1-9][0-9]*$'
                                         OR jsonb_typeof(line -> 'RecipeSnapshotJson') <> 'string'
                                         OR COALESCE(NULLIF(line ->> 'RecipeSnapshotJson', ''), '') = ''
                                     ))
                                 OR jsonb_typeof(line -> 'RobotPrograms') <> 'array'
                                 OR CASE
                                     WHEN jsonb_typeof(line -> 'RobotPrograms') = 'array'
                                         THEN jsonb_array_length(line -> 'RobotPrograms') = 0
                                     ELSE true
                                 END
                                 OR (
                                     SELECT count(*)
                                     FROM jsonb_array_elements(
                                         CASE
                                             WHEN jsonb_typeof(line -> 'RobotPrograms') = 'array'
                                                 THEN line -> 'RobotPrograms'
                                             ELSE '[]'::jsonb
                                         END) AS bindings(binding))
                                     <>
                                     (
                                         SELECT count(DISTINCT binding ->> 'BindingOrder')
                                         FROM jsonb_array_elements(
                                             CASE
                                                 WHEN jsonb_typeof(line -> 'RobotPrograms') = 'array'
                                                     THEN line -> 'RobotPrograms'
                                                 ELSE '[]'::jsonb
                                             END) AS bindings(binding))
                                 OR EXISTS (
                                     SELECT 1
                                     FROM jsonb_array_elements(
                                         CASE
                                             WHEN jsonb_typeof(line -> 'RobotPrograms') = 'array'
                                                 THEN line -> 'RobotPrograms'
                                             ELSE '[]'::jsonb
                                         END) AS programs(program)
                                     WHERE jsonb_typeof(program -> 'ProgramManifestSchemaVersion') <> 'number'
                                        OR COALESCE(program ->> 'ProgramManifestSchemaVersion', '') !~ '^[1-9][0-9]*$'
                                        OR jsonb_typeof(program -> 'ProgramManifestChecksum') <> 'string'
                                        OR COALESCE(NULLIF(program ->> 'ProgramManifestChecksum', ''), '') = ''
                                        OR jsonb_typeof(program -> 'Artifacts') <> 'array'
                                        OR CASE
                                            WHEN jsonb_typeof(program -> 'Artifacts') = 'array'
                                                THEN jsonb_array_length(program -> 'Artifacts') = 0
                                            ELSE true
                                        END
                                        OR (
                                            SELECT count(*)
                                            FROM jsonb_array_elements(
                                                CASE
                                                    WHEN jsonb_typeof(program -> 'Artifacts') = 'array'
                                                        THEN program -> 'Artifacts'
                                                    ELSE '[]'::jsonb
                                                END) AS artifact_orders(artifact))
                                            <>
                                            (
                                                SELECT count(DISTINCT artifact ->> 'RunOrder')
                                                FROM jsonb_array_elements(
                                                    CASE
                                                        WHEN jsonb_typeof(program -> 'Artifacts') = 'array'
                                                            THEN program -> 'Artifacts'
                                                        ELSE '[]'::jsonb
                                                    END) AS artifact_orders(artifact))
                                        OR EXISTS (
                                            SELECT 1
                                            FROM jsonb_array_elements(
                                                CASE
                                                    WHEN jsonb_typeof(program -> 'Artifacts') = 'array'
                                                        THEN program -> 'Artifacts'
                                                    ELSE '[]'::jsonb
                                                END) AS artifacts(artifact)
                                            WHERE jsonb_typeof(artifact -> 'ParametersSchemaVersion') <> 'number'
                                               OR COALESCE(artifact ->> 'ParametersSchemaVersion', '') !~ '^[1-9][0-9]*$'
                                               OR jsonb_typeof(artifact -> 'RuntimeTargetCode') <> 'string'
                                               OR COALESCE(NULLIF(artifact ->> 'RuntimeTargetCode', ''), '') = ''
                                               OR jsonb_typeof(artifact -> 'MachineModelCode') <> 'string'
                                               OR COALESCE(NULLIF(artifact ->> 'MachineModelCode', ''), '') = ''
                                               OR (
                                                   (artifact -> 'TechnicalContractId' IS NOT NULL
                                                       AND jsonb_typeof(artifact -> 'TechnicalContractId') <> 'null')
                                                   <>
                                                   (jsonb_typeof(artifact -> 'TechnicalContractChecksum') = 'string'
                                                       AND COALESCE(NULLIF(artifact ->> 'TechnicalContractChecksum', ''), '') <> '')
                                               ))
                                 )));

                    IF invalid_command_count > 0 THEN
                        RAISE EXCEPTION
                            'Historical snapshot migration blocked: % Schema V5 ExecuteOrder command(s) lack the immutable provenance required by runtime validation.',
                            invalid_command_count;
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateTable(
                name: "PaymentProviderExchanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Operation = table.Column<int>(type: "integer", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: true),
                    ProviderOrderCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestPayloadJson = table.Column<string>(type: "jsonb", maxLength: 262144, nullable: true),
                    ResponsePayloadJson = table.Column<string>(type: "jsonb", maxLength: 262144, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviderExchanges", x => x.Id);
                    table.CheckConstraint("CK_PaymentProviderExchanges_AttemptNumber_Positive", "\"AttemptNumber\" > 0");
                    table.CheckConstraint("CK_PaymentProviderExchanges_CompletionAfterStart", "\"CompletedAt\" IS NULL OR \"CompletedAt\" >= \"StartedAt\"");
                    table.CheckConstraint("CK_PaymentProviderExchanges_HttpStatusCode_Valid", "\"HttpStatusCode\" IS NULL OR \"HttpStatusCode\" BETWEEN 100 AND 599");
                    table.CheckConstraint("CK_PaymentProviderExchanges_Lifecycle", "(\"Status\" = 1 AND \"Outcome\" IS NULL AND \"CompletedAt\" IS NULL) OR (\"Status\" = 2 AND \"Outcome\" IS NOT NULL AND \"CompletedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_PaymentProviderExchanges_RequestPayload_Bounded", "\"RequestPayloadJson\" IS NULL OR octet_length(\"RequestPayloadJson\"::text) <= 262144");
                    table.CheckConstraint("CK_PaymentProviderExchanges_ResponsePayload_Bounded", "\"ResponsePayloadJson\" IS NULL OR octet_length(\"ResponsePayloadJson\"::text) <= 262144");
                    table.ForeignKey(
                        name: "FK_PaymentProviderExchanges_PaymentTransactions_PaymentTransac~",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderExchanges_PaymentTransactionId_Operation",
                table: "PaymentProviderExchanges",
                columns: new[] { "PaymentTransactionId", "Operation" },
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderExchanges_PaymentTransactionId_Operation_Att~",
                table: "PaymentProviderExchanges",
                columns: new[] { "PaymentTransactionId", "Operation", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderExchanges_PaymentTransactionId_StartedAt",
                table: "PaymentProviderExchanges",
                columns: new[] { "PaymentTransactionId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentProviderExchanges");

            migrationBuilder.DropColumn(
                name: "PreparationTimeSecondsSnapshot",
                table: "OrderItems");

            migrationBuilder.AddColumn<string>(
                name: "RawRequestJson",
                table: "PaymentTransactions",
                type: "jsonb",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawResponseJson",
                table: "PaymentTransactions",
                type: "jsonb",
                maxLength: 500,
                nullable: true);
        }
    }
}
