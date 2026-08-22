using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity.Bootstrap;

public class IdentityBootstrapHostedService : IHostedService
{
    private static readonly SeedRole[] SeedRoles =
    [
        new("SystemAdmin", "System Admin", true, 10),
        new("Manager", "Manager", false, 20),
        new("Staff", "Staff", false, 30),
        new("Technician", "Technician", false, 40),
        new("OrgAdmin", "Organization Admin", false, 50)
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<IdentityBootstrapHostedService> _logger;

    public IdentityBootstrapHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<IdentityBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await SeedRolesAsync(dbContext, cancellationToken);
        await BootstrapSystemAdminAsync(dbContext, passwordHasher, cancellationToken);
        await BootstrapDevelopmentRoleAccountsAsync(dbContext, passwordHasher, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedRolesAsync(IceBotDbContext dbContext, CancellationToken cancellationToken)
    {
        foreach (var seedRole in SeedRoles)
        {
            var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Code == seedRole.Code, cancellationToken);
            if (role is null)
            {
                role = new Role
                {
                    Code = seedRole.Code,
                    Name = seedRole.Name,
                    IsSystemRole = seedRole.IsSystemRole,
                    Priority = seedRole.Priority,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                dbContext.Roles.Add(role);
            }
            else
            {
                role.Name = seedRole.Name;
                role.IsSystemRole = seedRole.IsSystemRole;
                role.Priority = seedRole.Priority;
                role.IsActive = true;
                role.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task BootstrapSystemAdminAsync(
        IceBotDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        var hasSystemAdmin = await dbContext.AccountRoles
            .AnyAsync(x => x.Role.Code == "SystemAdmin" && x.IsActive, cancellationToken);

        if (hasSystemAdmin)
        {
            return;
        }

        var options = _configuration.GetSection(IdentityBootstrapOptions.SectionName).Get<IdentityBootstrapOptions>();
        var userName = options?.UserName ?? Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_USERNAME");
        var email = options?.Email ?? Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_EMAIL");
        var password = options?.Password ?? Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD");
        var fullName = options?.FullName ?? Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_FULLNAME");

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("No SystemAdmin account exists, but bootstrap admin config/env is incomplete.");
            return;
        }

        var systemAdminRole = await dbContext.Roles.FirstAsync(x => x.Code == "SystemAdmin", cancellationToken);
        var normalizedUserName = NormalizeUserName(userName);
        var normalizedEmail = NormalizeEmail(email);

        var existingAccount = await dbContext.Accounts.WhereNotDeleted()
            .Include(x => x.AccountRoles)
            .FirstOrDefaultAsync(
                x => x.UserName == normalizedUserName || x.Email == normalizedEmail,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var account = existingAccount ?? new Account
        {
            UserName = normalizedUserName,
            Email = normalizedEmail,
            CreatedAt = now
        };

        account.FullName = string.IsNullOrWhiteSpace(fullName) ? "Bootstrap System Admin" : fullName.Trim();
        account.Status = AccountStatus.Active;
        account.LocalLoginEnabled = true;
        account.GoogleLoginEnabled = false;
        account.EmailConfirmed = true;
        account.EmailConfirmedAt ??= now;
        account.Password = HashedPassword.From(passwordHasher.HashPassword(password));

        if (existingAccount is null)
        {
            dbContext.Accounts.Add(account);
        }

        if (!account.AccountRoles.Any(x => x.RoleId == systemAdminRole.Id && x.IsActive))
        {
            account.AccountRoles.Add(new AccountRole
            {
                RoleId = systemAdminRole.Id,
                AssignedAt = now,
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Bootstrapped SystemAdmin account '{Email}'.", normalizedEmail);
    }

    private async Task BootstrapDevelopmentRoleAccountsAsync(
        IceBotDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        var roleCodes = new[] { "OrgAdmin", "Manager", "Staff" };
        var options = _configuration.GetSection(DevelopmentRoleAccountsBootstrapOptions.SectionName)
            .Get<DevelopmentRoleAccountsBootstrapOptions>();
        if (!_hostEnvironment.IsDevelopment() || options?.Enabled != true)
        {
            return;
        }

        var password = _configuration[$"{IdentityBootstrapOptions.SectionName}:Password"]
            ?? Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "Development role accounts are enabled but BootstrapAdmin password is unavailable; skipping local role-account seed.");
            return;
        }

        var assignedRoleCodes = await dbContext.AccountRoles
            .Where(assignment =>
                assignment.IsActive &&
                assignment.Account.DeletedAt == null &&
                roleCodes.Contains(assignment.Role.Code))
            .Select(assignment => assignment.Role.Code)
            .Distinct()
            .ToListAsync(cancellationToken);
        var missingRoleCodes = roleCodes
            .Except(assignedRoleCodes, StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (missingRoleCodes.Count == 0)
        {
            _logger.LogInformation(
                "Skipped development role-account seed because every configured role already has an active account assignment.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var organization = await EnsureDevelopmentOrganizationAsync(dbContext, now, cancellationToken);
        var store = await EnsureDevelopmentStoreAsync(dbContext, organization, now, cancellationToken);
        var kiosk = await EnsureDevelopmentKioskAsync(dbContext, organization, store, now, cancellationToken);

        var roles = await dbContext.Roles
            .Where(role => missingRoleCodes.Contains(role.Code))
            .ToDictionaryAsync(role => role.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var seeds = new[]
        {
            new DevelopmentRoleAccountSeed("orgadmin", "orgadmin@icebot.local", "Organization Admin", "OrgAdmin", organization.Id, null, null),
            new DevelopmentRoleAccountSeed("manager", "manager@icebot.local", "Store Manager", "Manager", organization.Id, store.Id, null),
            new DevelopmentRoleAccountSeed("staff", "staff@icebot.local", "Store Staff", "Staff", organization.Id, store.Id, null)
        }.Where(seed => missingRoleCodes.Contains(seed.RoleCode));

        foreach (var seed in seeds)
        {
            if (!roles.TryGetValue(seed.RoleCode, out var role))
            {
                throw new InvalidOperationException($"Required development role '{seed.RoleCode}' was not seeded.");
            }

            var account = await dbContext.Accounts
                .Include(candidate => candidate.AccountRoles)
                .FirstOrDefaultAsync(candidate =>
                    candidate.DeletedAt == null &&
                    (candidate.UserName == seed.UserName || candidate.Email == seed.Email), cancellationToken);
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
                    Password = HashedPassword.From(passwordHasher.HashPassword(password)),
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
        _logger.LogInformation(
            "Seeded development accounts for missing roles: {RoleCodes}.",
            string.Join(", ", missingRoleCodes.OrderBy(roleCode => roleCode)));
    }

    private static async Task<Organization> EnsureDevelopmentOrganizationAsync(
        IceBotDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(
            candidate => candidate.DeletedAt == null && candidate.Code == "ICEBOT-DEMO", cancellationToken);
        if (organization is not null)
        {
            return organization;
        }

        organization = new Organization
        {
            Code = "ICEBOT-DEMO",
            Name = "IceBot Demo Organization",
            Status = EntityStatus.Active,
            CreatedAt = now
        };
        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync(cancellationToken);
        return organization;
    }

    private static async Task<Store> EnsureDevelopmentStoreAsync(
        IceBotDbContext dbContext,
        Organization organization,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var store = await dbContext.Stores.FirstOrDefaultAsync(candidate =>
            candidate.DeletedAt == null && candidate.OrganizationId == organization.Id &&
            candidate.Code == "ICEBOT-DEMO-STORE", cancellationToken);
        if (store is not null)
        {
            return store;
        }

        store = new Store
        {
            OrganizationId = organization.Id,
            Code = "ICEBOT-DEMO-STORE",
            Name = "IceBot Demo Store",
            StoreType = "Retail",
            Status = EntityStatus.Active,
            TimeZone = "Asia/Bangkok",
            CreatedAt = now
        };
        dbContext.Stores.Add(store);
        await dbContext.SaveChangesAsync(cancellationToken);
        return store;
    }

    private static async Task<Kiosk> EnsureDevelopmentKioskAsync(
        IceBotDbContext dbContext,
        Organization organization,
        Store store,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var kiosk = await dbContext.Kiosks.FirstOrDefaultAsync(candidate =>
            candidate.DeletedAt == null && candidate.OrganizationId == organization.Id &&
            candidate.Code == "ICEBOT-DEMO-KIOSK", cancellationToken);
        if (kiosk is not null)
        {
            return kiosk;
        }

        kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = "ICEBOT-DEMO-KIOSK",
            Name = "IceBot Demo Kiosk",
            KioskType = "RoboticVending",
            Status = KioskStatus.Active,
            TimeZone = "Asia/Bangkok",
            CreatedAt = now
        };
        dbContext.Kiosks.Add(kiosk);
        await dbContext.SaveChangesAsync(cancellationToken);
        return kiosk;
    }

    private static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeUserName(string value) => value.Trim().ToLowerInvariant();

    private sealed record SeedRole(string Code, string Name, bool IsSystemRole, int Priority);

    private sealed record DevelopmentRoleAccountSeed(
        string UserName,
        string Email,
        string FullName,
        string RoleCode,
        Guid OrganizationId,
        Guid? StoreId,
        Guid? KioskId);
}
