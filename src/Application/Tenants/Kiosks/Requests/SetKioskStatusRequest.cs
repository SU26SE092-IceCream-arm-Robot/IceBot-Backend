using Domain.Tenants.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Tenants.Kiosks.Requests;

public sealed class SetKioskStatusRequest
{
    [Required]
    public KioskStatus Status { get; set; }
}
