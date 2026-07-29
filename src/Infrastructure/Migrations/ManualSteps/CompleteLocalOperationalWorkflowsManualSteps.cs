using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations;

internal static class CompleteLocalOperationalWorkflowsManualSteps
{
    public static void EnsureUniqueProviderPaymentIdentity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM "PaymentTransactions"
                    WHERE "ProviderOrderCode" IS NOT NULL
                    GROUP BY "Provider", "ProviderOrderCode"
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Cannot enforce provider payment identity: duplicate Provider and ProviderOrderCode values exist.';
                END IF;
            END $$;
            """);
    }
}
