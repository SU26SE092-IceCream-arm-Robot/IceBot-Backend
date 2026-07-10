using Domain.Devices.Catalog;
using System.ComponentModel.DataAnnotations;

namespace Application.Devices.Catalog.Requests;

public sealed class SetDeviceStatusRequest
{
    [Required]
    public DeviceStatus Status { get; set; }
}
