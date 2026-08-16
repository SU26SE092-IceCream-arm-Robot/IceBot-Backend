using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;
using Infrastructure.Catalog.Bootstrap;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity.Bootstrap;

/// <summary>
/// Seeds the operational roles for the isolated ICEBOT-DEMO tenant. The local
/// credentials intentionally reuse the existing bootstrap SystemAdmin password
/// so no password is committed to source control.
/// </summary>
public sealed class IceBotDemoRoleAccountsSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IPasswordHasher passwordHasher,
    ILogger<IceBotDemoRoleAccountsSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!IceBotDemoTenantSeedHostedService.IsEnabled(configuration))
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();
        var organization = await dbContext.Organizations.SingleAsync(
            candidate => candidate.Code == IceBotDemoTenantSeedHostedService.OrganizationCode,
            cancellationToken);
        var store = await dbContext.Stores.SingleAsync(
            candidate => candidate.OrganizationId == organization.Id && candidate.Code == IceBotDemoTenantSeedHostedService.StoreCode,
            cancellationToken);
        var kiosk = await dbContext.Kiosks.SingleAsync(
            candidate => candidate.OrganizationId == organization.Id && candidate.StoreId == store.Id && candidate.Code == IceBotDemoTenantSeedHostedService.KioskCode,
            cancellationToken);
        var configuredPassword = configuration["DemoCatalogSeed:AccountPassword"]
            ?? Environment.GetEnvironmentVariable("DEMO_CATALOG_SEED_PASSWORD");
        var passwordHash = !string.IsNullOrWhiteSpace(configuredPassword)
            ? HashedPassword.From(passwordHasher.HashPassword(configuredPassword))
            : await dbContext.AccountRoles
            .Where(assignment => assignment.IsActive && assignment.Role.Code == "SystemAdmin" && assignment.Account.DeletedAt == null)
            .Select(assignment => assignment.Account.Password)
            .FirstOrDefaultAsync(cancellationToken);
        if (passwordHash is null)
        {
            logger.LogWarning(
                "Skipped ICEBOT-DEMO role accounts because no password is available. Set DemoCatalogSeed__AccountPassword or DEMO_CATALOG_SEED_PASSWORD.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var roleCodes = new[] { "OrgAdmin", "Manager", "Staff", "Technician" };
        var roles = await dbContext.Roles
            .Where(role => roleCodes.Contains(role.Code))
            .ToDictionaryAsync(role => role.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var seeds = new[]
        {
            new DemoAccountSeed("demo-orgadmin", "demo-orgadmin@icebot.local", "Demo Organization Admin", "OrgAdmin", organization.Id, null, null),
            new DemoAccountSeed("demo-manager", "demo-manager@icebot.local", "Demo Store Manager", "Manager", organization.Id, store.Id, null),
            new DemoAccountSeed("demo-staff", "demo-staff@icebot.local", "Demo Store Staff", "Staff", organization.Id, store.Id, null),
            new DemoAccountSeed("demo-technician", "demo-technician@icebot.local", "Demo Kiosk Technician", "Technician", organization.Id, store.Id, kiosk.Id)
        };

        foreach (var seed in seeds)
        {
            var role = roles[seed.RoleCode];
            var account = await dbContext.Accounts
                .Include(candidate => candidate.AccountRoles)
                .SingleOrDefaultAsync(candidate => candidate.UserName == seed.UserName || candidate.Email == seed.Email, cancellationToken);
            if (account is null)
            {
                account = new Account
                {
                    UserName = seed.UserName,
                    Email = seed.Email,
                    FullName = seed.FullName,
                    Status = AccountStatus.Active,
                    LocalLoginEnabled = true,
                    GoogleLoginEnabled = false,
                    EmailConfirmed = true,
                    EmailConfirmedAt = now,
                    Password = HashedPassword.From(passwordHash.Value),
                    CreatedAt = now
                };
                dbContext.Accounts.Add(account);
            }

            if (!account.AccountRoles.Any(assignment =>
                    assignment.RoleId == role.Id &&
                    assignment.OrganizationId == seed.OrganizationId &&
                    assignment.StoreId == seed.StoreId &&
                    assignment.KioskId == seed.KioskId &&
                    assignment.IsActive))
            {
                account.AccountRoles.Add(new AccountRole
                {
                    RoleId = role.Id,
                    OrganizationId = seed.OrganizationId,
                    StoreId = seed.StoreId,
                    KioskId = seed.KioskId,
                    IsActive = true,
                    AssignedAt = now
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Ensured ICEBOT-DEMO accounts. They reuse the bootstrap SystemAdmin local password and must not be used outside the demo environment.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private sealed record DemoAccountSeed(
        string UserName,
        string Email,
        string FullName,
        string RoleCode,
        Guid OrganizationId,
        Guid? StoreId,
        Guid? KioskId);
}
