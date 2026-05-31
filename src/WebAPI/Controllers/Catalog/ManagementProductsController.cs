using Application.Catalog.Products.Requests;
using Application.Catalog.Products.Services;
using Application.Shared.Exceptions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers.Catalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/products")]
[Authorize(Policy = "products.manage")]
public sealed class ManagementProductsController : ControllerBase
{
    private readonly ProductManagementService _products;

    public ManagementProductsController(ProductManagementService products)
    {
        _products = products;
    }

    [HttpGet]
    public async Task<IActionResult> ListProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _products.ListProductsAsync(
            search,
            organizationId,
            storeId,
            kioskId,
            pageNumber,
            pageSize,
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetProduct(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _products.GetProductAsync(productId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _products.CreateProductAsync(
            request,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _products.UpdateProductAsync(
            productId,
            request,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{productId:guid}/availability")]
    public async Task<IActionResult> SetProductAvailability(
        Guid productId,
        [FromBody] SetAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _products.SetProductAvailabilityAsync(
            productId,
            request.IsAvailable,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _products.DeleteProductAsync(
            productId,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{productId:guid}/variants")]
    public async Task<IActionResult> AddVariant(
        Guid productId,
        [FromBody] UpsertProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _products.AddVariantAsync(
            productId,
            request,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{productId:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> UpdateVariant(
        Guid productId,
        Guid variantId,
        [FromBody] UpdateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _products.UpdateVariantAsync(
            productId,
            variantId,
            request,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{productId:guid}/variants/{variantId:guid}/availability")]
    public async Task<IActionResult> SetVariantAvailability(
        Guid productId,
        Guid variantId,
        [FromBody] SetAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var result = await _products.SetVariantAvailabilityAsync(
            productId,
            variantId,
            request.IsAvailable,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{productId:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> DeleteVariant(
        Guid productId,
        Guid variantId,
        CancellationToken cancellationToken)
    {
        var result = await _products.DeleteVariantAsync(
            productId,
            variantId,
            GetCurrentAccountId(),
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    private Guid? GetCurrentAccountId()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(accountId, out var parsedAccountId) ? parsedAccountId : null;
    }

    private void EnsureValidModel()
    {
        if (ModelState.IsValid)
        {
            return;
        }

        var errors = ModelState.ToDictionary(
            item => item.Key,
            item => item.Value?.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid");

        throw new ValidationException(errors);
    }
}
