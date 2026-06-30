using System.ComponentModel.DataAnnotations;

namespace Application.Operations.Alerts.Requests;

public sealed class ResolveAlertRequest
{
    [Required]
    [StringLength(500)]
    public string ResolutionNotes { get; init; } = null!;
}
