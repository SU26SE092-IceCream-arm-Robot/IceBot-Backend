using Domain.Devices.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Devices.Requests;

public sealed class SetDeviceStatusRequest
{
    [Required]
    public DeviceStatus Status { get; set; }
}
