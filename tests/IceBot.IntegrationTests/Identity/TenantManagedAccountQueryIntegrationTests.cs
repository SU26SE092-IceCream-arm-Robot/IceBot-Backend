using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Identity.Persistence;
using IceBotDbContext = global::Infrastructure.Data.IceBotDbContext;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.Identity;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class TenantManagedAccountQueryIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task TenantQuery_ReturnsTenantAccount_InRequestedOrganization()
    {
        await using var db = fixture.CreateDbContext();
        var organization = NewOrganization();
        var manager = NewAccount("manager");
        var managerRole = await GetOrCreateRoleAsync(db, "Manager");
        db.AddRange(organization, manager);
        await db.SaveChangesAsync();
        db.AccountRoles.Add(new AccountRole
        {
            AccountId = manager.Id,
            RoleId = managerRole.Id,
            OrganizationId = organization.Id,
            IsActive = true,
            AssignedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await new IdentityAccountStore(db).GetTenantManagedByIdAsync(
            manager.Id,
            organization.Id);

        Assert.NotNull(result);
        Assert.Equal(manager.Id, result.Id);
    }

    [IntegrationFact]
    public async Task TenantQuery_ExcludesPlatformTechnician_BeforeMaterialization()
    {
        await using var db = fixture.CreateDbContext();
        var organization = NewOrganization();
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Technician support store",
            TimeZone = "Asia/Bangkok"
        };
        var platformTechnician = NewAccount("platform-technician");
        platformTechnician.PlatformTechnicianProfile = new PlatformTechnicianProfile();
        db.AddRange(organization, store, platformTechnician);
        await db.SaveChangesAsync();
        db.TechnicianSupportGrants.Add(TechnicianSupportGrant.Create(
            platformTechnician.Id, organization.Id, store.Id, null, DateTimeOffset.UtcNow, null));
        await db.SaveChangesAsync();
        var subject = new IdentityAccountStore(db);

        var platformResult = await subject.GetTenantManagedByIdAsync(
            platformTechnician.Id,
            organization.Id);
        Assert.Null(platformResult);
    }

    private static Organization NewOrganization() => new()
    {
        Code = $"ORG-{Guid.NewGuid():N}",
        Name = "Tenant managed account query organization"
    };

    private static Account NewAccount(string prefix) => new()
    {
        UserName = $"{prefix}-{Guid.NewGuid():N}",
        Email = $"{prefix}-{Guid.NewGuid():N}@example.test",
        Status = AccountStatus.Active
    };

    private static async Task<Role> GetOrCreateRoleAsync(
        IceBotDbContext db,
        string code)
    {
        var role = await db.Roles.SingleOrDefaultAsync(item => item.Code == code);
        if (role is not null) return role;
        role = new Role { Code = code, Name = code, IsSystemRole = true };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }
}
