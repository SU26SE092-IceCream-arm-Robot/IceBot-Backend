using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations;

internal static class CompleteLocalOperationalChangesManualSteps
{
    public static void BackfillPaymentSettlementAndOrderDeadline(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            WITH ranked_paid AS (
                SELECT
                    "Id",
                    ROW_NUMBER() OVER (
                        PARTITION BY "OrderId"
                        ORDER BY "PaidAt" NULLS LAST, "RequestedAt", "Id"
                    ) AS settlement_rank
                FROM "PaymentTransactions"
                WHERE "Status" = 3
                  AND "DeletedAt" IS NULL
            )
            UPDATE "PaymentTransactions" AS payment
            SET
                "SettlementDisposition" = CASE
                    WHEN ranked.settlement_rank = 1 THEN 1
                    ELSE 2
                END,
                "LastErrorCode" = CASE
                    WHEN ranked.settlement_rank > 1
                        THEN 'DUPLICATE_PAYMENT_REFUND_REQUIRED'
                    ELSE payment."LastErrorCode"
                END,
                "LastErrorMessage" = CASE
                    WHEN ranked.settlement_rank > 1
                        THEN 'Multiple provider-confirmed payments existed when settlement ownership was introduced. Manual refund review is required.'
                    ELSE payment."LastErrorMessage"
                END
            FROM ranked_paid AS ranked
            WHERE payment."Id" = ranked."Id";
            """);

        migrationBuilder.Sql(
            """
            UPDATE "Orders" AS orders
            SET
                "PaidAmount" = COALESCE(primary_payment."PaidAmount", primary_payment."Amount"),
                "PaidAt" = COALESCE(primary_payment."PaidAt", primary_payment."ProviderPaidAt", orders."PaidAt")
            FROM "PaymentTransactions" AS primary_payment
            WHERE primary_payment."OrderId" = orders."Id"
              AND primary_payment."SettlementDisposition" = 1
              AND EXISTS (
                  SELECT 1
                  FROM "PaymentTransactions" AS duplicate_payment
                  WHERE duplicate_payment."OrderId" = orders."Id"
                    AND duplicate_payment."SettlementDisposition" = 2
                    AND duplicate_payment."DeletedAt" IS NULL
              );
            """);

        migrationBuilder.Sql(
            """
            UPDATE "Orders" AS orders
            SET
                "StatusBeforeDuplicatePaymentIntervention" = CASE
                    WHEN orders."Status" <> 12 THEN orders."Status"
                    ELSE NULL
                END,
                "Status" = 12,
                "Notes" = COALESCE(
                    orders."Notes",
                    'Duplicate provider payment detected during settlement migration. Manual refund review is required.')
            WHERE orders."DeletedAt" IS NULL
              AND EXISTS (
                  SELECT 1
                  FROM "PaymentTransactions" AS duplicate_payment
                  WHERE duplicate_payment."OrderId" = orders."Id"
                    AND duplicate_payment."SettlementDisposition" = 2
                    AND duplicate_payment."DeletedAt" IS NULL
              );
            """);

        migrationBuilder.Sql(
            """
            UPDATE "Orders"
            SET "PaymentDeadlineAt" = "PlacedAt" + INTERVAL '15 minutes'
            WHERE "PaymentDeadlineAt" IS NULL;
            """);
    }

    public static void MigrateLegacyKioskMaintenanceState(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "Kiosks"
            SET "Status" = 2,
                "OperationalState" = 3,
                "OperationalStateReason" = 'Migrated from legacy KioskStatus.Maintenance',
                "OperationalStateChangedAt" = COALESCE("UpdatedAt", "CreatedAt")
            WHERE "Status" = 4;
            """);
    }

    public static void RestoreLegacyKioskMaintenanceState(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "Kiosks"
            SET "Status" = 4
            WHERE "Status" = 2
              AND "OperationalState" = 3
              AND "OperationalStateReason" = 'Migrated from legacy KioskStatus.Maintenance';
            """);
    }
}
