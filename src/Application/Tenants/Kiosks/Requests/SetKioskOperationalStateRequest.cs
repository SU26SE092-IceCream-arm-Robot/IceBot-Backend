using System.ComponentModel.DataAnnotations;
using Domain.Tenants.Enums;

namespace Application.Tenants.Kiosks.Requests;

public sealed class SetKioskOperationalStateRequest
{
    public required KioskOperationalState State { get; init; }

    [Required]
    [StringLength(500)]
    public required string Reason { get; init; }
}
