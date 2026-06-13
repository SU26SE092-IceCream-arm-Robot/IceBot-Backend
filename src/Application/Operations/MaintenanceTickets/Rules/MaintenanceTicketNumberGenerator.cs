using Application.Operations.Abstractions;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Operations.MaintenanceTickets.Rules;

public static class MaintenanceTicketNumberGenerator
{
    private static readonly char[] AlphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    private static readonly Random Random = new();

    public static async Task<string> GenerateAsync(IMaintenanceTicketStore store, CancellationToken cancellationToken = default)
    {
        string ticketNumber;
        bool exists;
        var datePart = DateTimeOffset.UtcNow.ToString("yyyyMMdd");

        do
        {
            var randomPart = new StringBuilder(4);
            for (int i = 0; i < 4; i++)
            {
                randomPart.Append(AlphanumericChars[Random.Next(AlphanumericChars.Length)]);
            }

            ticketNumber = $"MNT-{datePart}-{randomPart}";
            exists = await store.TicketNumberExistsAsync(ticketNumber, cancellationToken);
        }
        while (exists);

        return ticketNumber;
    }
}
