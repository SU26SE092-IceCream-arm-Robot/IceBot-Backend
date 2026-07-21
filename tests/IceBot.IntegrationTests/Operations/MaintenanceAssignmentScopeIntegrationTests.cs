using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Operations.Entities;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Operations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.Operations;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class MaintenanceAssignmentScopeIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task TicketNumberCollision_CanBeRetriedInSameUnitOfWork()
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"ORG-{Guid.NewGuid():N}",
            Name = "Maintenance collision organization"
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Maintenance collision store",
            TimeZone = "Asia/Bangkok"
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Maintenance collision kiosk"
        };
        db.AddRange(organization, store, kiosk);
        await db.SaveChangesAsync();

        var existing = NewTicket(organization.Id, store.Id, kiosk.Id, "MNT-COLLISION");
        db.MaintenanceTickets.Add(existing);
        await db.SaveChangesAsync();

        var candidate = NewTicket(organization.Id, store.Id, kiosk.Id, existing.TicketNumber);
        var subject = new MaintenanceTicketStore(db);
        await subject.AddAsync(candidate);

        Assert.False(await subject.TrySaveNewTicketAsync());

        candidate.TicketNumber = $"MNT-{Guid.NewGuid():N}";
        Assert.True(await subject.TrySaveNewTicketAsync());
        Assert.True(await db.MaintenanceTickets.AnyAsync(ticket => ticket.Id == candidate.Id));
    }

    [IntegrationFact]
    public async Task AssigneeRoleScope_MustMatchTicketTenant()
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"ORG-{Guid.NewGuid():N}",
            Name = "Maintenance organization"
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Maintenance store",
            TimeZone = "Asia/Bangkok"
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Maintenance kiosk"
        };
        var account = new Account
        {
            UserName = $"technician-{Guid.NewGuid():N}",
            Email = $"technician-{Guid.NewGuid():N}@example.test",
            Status = AccountStatus.Active
        };
        var technician = await db.Roles.SingleOrDefaultAsync(role => role.Code == "Technician");
        if (technician is null)
        {
            technician = new Role { Code = "Technician", Name = "Technician", IsSystemRole = true };
            db.Roles.Add(technician);
        }

        db.AddRange(organization, store, kiosk, account);
        await db.SaveChangesAsync();
        db.AccountRoles.Add(new AccountRole
        {
            AccountId = account.Id,
            RoleId = technician.Id,
            OrganizationId = organization.Id,
            AssignedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var subject = new MaintenanceTicketStore(db);
        Assert.True(await subject.CanAssignAccountAsync(
            account.Id, organization.Id, store.Id, kiosk.Id));
        Assert.False(await subject.CanAssignAccountAsync(
            account.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
    }

    private static MaintenanceTicket NewTicket(
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        string ticketNumber) =>
        new()
        {
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            TicketNumber = ticketNumber,
            IssueCode = "TEST",
            Title = "Collision test",
            ReportedAt = DateTimeOffset.UtcNow
        };
}
