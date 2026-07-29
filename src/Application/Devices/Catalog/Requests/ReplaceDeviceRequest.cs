using System.ComponentModel.DataAnnotations;

namespace Application.Devices.Catalog.Requests;

public sealed class ReplaceDeviceRequest
{
    public Guid ReplacementDeviceId { get; set; }

    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = null!;
}
