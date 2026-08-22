using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Operations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.Operations;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class CriticalAlertNotificationRecipientStoreIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task RecipientPolicy_PrefersScopedTechnicalRolesAndFallsBackToOrganizationAdmin()
    {
        var organizationId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var kioskId = Guid.NewGuid();
        var technician = CreateAccount("technician");
        var managerWithoutDevice = CreateAccount("manager");
        var organizationAdmin = CreateAccount("org-admin");

        await using (var seed = fixture.CreateDbContext())
        {
            seed.Organizations.Add(new Organization
            {
                Id = organizationId,
                Code = $"ORG-{Guid.NewGuid():N}",
                Name = "Notification policy organization",
                Status = EntityStatus.Active
            });
            seed.Stores.Add(new Store
            {
                Id = storeId,
                OrganizationId = organizationId,
                Code = $"STORE-{Guid.NewGuid():N}",
                Name = "Notification policy store",
                Status = EntityStatus.Active
            });
            seed.Kiosks.Add(new Kiosk
            {
                Id = kioskId,
                OrganizationId = organizationId,
                StoreId = storeId,
                Code = $"KIOSK-{Guid.NewGuid():N}",
                Name = "Notification policy kiosk",
                Status = KioskStatus.Active
            });
            var managerRole = await GetOrCreateRoleAsync(seed, "Manager");
            var organizationAdminRole = await GetOrCreateRoleAsync(seed, "OrgAdmin");
            await seed.SaveChangesAsync();
            seed.Accounts.AddRange(technician, managerWithoutDevice, organizationAdmin);
            seed.PlatformTechnicianProfiles.Add(new PlatformTechnicianProfile
            {
                AccountId = technician.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
            seed.TechnicianSupportGrants.Add(TechnicianSupportGrant.Create(
                technician.Id, organizationId, storeId, null, DateTimeOffset.UtcNow, null));
            seed.AccountRoles.AddRange(
                CreateRole(managerWithoutDevice.Id, managerRole.Id, organizationId, storeId, kioskId),
                CreateRole(organizationAdmin.Id, organizationAdminRole.Id, organizationId: organizationId));
            seed.AccountNotificationDevices.AddRange(
                CreateDevice(technician.Id, "technician"),
                CreateDevice(organizationAdmin.Id, "org-admin"));
            await seed.SaveChangesAsync();
        }

        await using (var primaryContext = fixture.CreateDbContext())
        {
            var recipientStore = new CriticalAlertNotificationRecipientStore(primaryContext);
            var recipients = await recipientStore.ListRecipientAccountIdsAsync(
                organizationId, storeId, kioskId);
            Assert.Equal([technician.Id], recipients);
        }

        await using (var mutation = fixture.CreateDbContext())
        {
            var grant = await mutation.TechnicianSupportGrants.SingleAsync(x => x.AccountId == technician.Id);
            grant.Revoke(DateTimeOffset.UtcNow, null);
            await mutation.SaveChangesAsync();
        }

        await using var fallbackContext = fixture.CreateDbContext();
        var fallbackStore = new CriticalAlertNotificationRecipientStore(fallbackContext);
        var fallbackRecipients = await fallbackStore.ListRecipientAccountIdsAsync(
            organizationId, storeId, kioskId);
        Assert.Equal([organizationAdmin.Id], fallbackRecipients);
    }

    private static Account CreateAccount(string prefix) => new()
    {
        UserName = $"{prefix}-{Guid.NewGuid():N}",
        Email = $"{prefix}-{Guid.NewGuid():N}@example.test",
        Status = AccountStatus.Active
    };

    private static AccountRole CreateRole(
        Guid accountId,
        long roleId,
        Guid? organizationId = null,
        Guid? storeId = null,
        Guid? kioskId = null) => new()
        {
            AccountId = accountId,
            RoleId = roleId,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            IsActive = true,
            AssignedAt = DateTimeOffset.UtcNow
        };

    private static AccountNotificationDevice CreateDevice(Guid accountId, string prefix) => new()
    {
        AccountId = accountId,
        InstallationId = Guid.NewGuid(),
        Platform = "Android",
        PushToken = $"token-{prefix}-{Guid.NewGuid():N}",
        PushTokenHash = $"hash-{prefix}-{Guid.NewGuid():N}"
    };

    private static async Task<Role> GetOrCreateRoleAsync(global::Infrastructure.Data.IceBotDbContext db, string code)
    {
        var existing = await db.Roles.SingleOrDefaultAsync(role => role.Code == code);
        if (existing is not null)
        {
            return existing;
        }

        var role = new Role { Code = code, Name = code, IsSystemRole = true };
        db.Roles.Add(role);
        return role;
    }
}
