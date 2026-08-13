using Application.Payments.PaymentMethods.Commands;
using Application.Payments.PaymentMethods.DTOs;
using Application.Payments.PaymentMethods.Queries;
using Application.Shared.Exceptions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/payment-methods")]
[Authorize(Policy = "payment-methods.manage")]
public sealed class ManagementPaymentMethodsController : ControllerBase
{
    private readonly ListPaymentMethodsQueryHandler _listPaymentMethodsHandler;
    private readonly SetPaymentMethodStatusCommandHandler _setStatusHandler;

    public ManagementPaymentMethodsController(
        ListPaymentMethodsQueryHandler listPaymentMethodsHandler,
        SetPaymentMethodStatusCommandHandler setStatusHandler)
    {
        _listPaymentMethodsHandler = listPaymentMethodsHandler;
        _setStatusHandler = setStatusHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = new ListPaymentMethodsQuery(activeOnly: null);
        var result = await _listPaymentMethodsHandler.HandleAsync(query);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:long}/status")]
    [Authorize(Policy = "payment-methods.manage")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] PaymentMethodStatusUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                x => x.Key,
                x => x.Value?.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid");
            throw new ValidationException(errors);
        }

        var command = new SetPaymentMethodStatusCommand(id, request.IsActive);
        var result = await _setStatusHandler.HandleAsync(command);
        return StatusCode(result.StatusCode, result);
    }
}
