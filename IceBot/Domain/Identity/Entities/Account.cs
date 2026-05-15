using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Identity.ValueObjects;
using Domain.Payments.Entities;

namespace Domain.Identity.Entities;

public partial class Account : BusinessEntity
{
    public long? PrimaryRoleId { get; set; }

    public string UserName { get; set; } = null!;

    public string? FullName { get; set; }

    public string Email { get; set; } = null!;

    public bool EmailConfirmed { get; set; }

    public DateTimeOffset? EmailConfirmedAt { get; set; }

    public HashedPassword? Password { get; set; }

    public string? ImageUrl { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public DateTimeOffset? PhoneNumberConfirmedAt { get; set; }

    public string? Address { get; set; }

    public string Gender { get; set; } = "Other";

    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public bool IsExternal { get; set; }

    public string? ExternalProvider { get; set; }

    public string? ExternalId { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public int FailedLoginCount { get; set; }

    public virtual Role? PrimaryRole { get; set; }

    public virtual ICollection<AccountDevice> AccountDevices { get; set; } = new List<AccountDevice>();

    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    public virtual ICollection<MaintenanceTicket> MaintenanceTicketAssignedToAccounts { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<MaintenanceTicket> MaintenanceTicketCreatedByAccounts { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
}
