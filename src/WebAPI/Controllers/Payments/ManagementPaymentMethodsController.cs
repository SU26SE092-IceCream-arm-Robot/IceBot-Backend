using Application.Payments.PaymentMethods.DTOs;
using Application.Payments.PaymentMethods.Interfaces;
using Application.Shared.Exceptions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/payment-methods")]
[Authorize(Policy = "payments.manage")]
public sealed class ManagementPaymentMethodsController : ControllerBase
{
    private readonly IManagePaymentMethodService _paymentMethodService;

    public ManagementPaymentMethodsController(IManagePaymentMethodService paymentMethodService)
    {
        _paymentMethodService = paymentMethodService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _paymentMethodService.GetAllAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] PaymentMethodStatusUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                x => x.Key,
                x => x.Value?.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid");
            throw new ValidationException(errors);
        }

        var result = await _paymentMethodService.SetStatusAsync(id, request.IsActive);
        return StatusCode(result.StatusCode, result);
    }
}
