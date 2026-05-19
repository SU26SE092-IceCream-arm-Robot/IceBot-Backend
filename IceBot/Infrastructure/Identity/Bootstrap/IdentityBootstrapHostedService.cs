using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;
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
        new("Technician", "Technician", false, 30),
        new("LocationOwner", "Location Owner", false, 40)
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityBootstrapHostedService> _logger;

    public IdentityBootstrapHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<IdentityBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await SeedRolesAsync(dbContext, cancellationToken);
        await BootstrapSystemAdminAsync(dbContext, passwordHasher, cancellationToken);
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

        var existingAccount = await dbContext.Accounts
            .Include(x => x.AccountRoles)
            .FirstOrDefaultAsync(
                x => x.UserName == normalizedUserName || x.Email == normalizedEmail,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var account = existingAccount ?? new Account
        {
            Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
                RoleId = systemAdminRole.Id,
                AssignedAt = now,
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Bootstrapped SystemAdmin account '{Email}'.", normalizedEmail);
    }

    private static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeUserName(string value) => value.Trim().ToLowerInvariant();

    private sealed record SeedRole(string Code, string Name, bool IsSystemRole, int Priority);
}
