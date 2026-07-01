using Application.Catalog.Products.Commands;
using Application.Catalog.Products.Queries;
using Application.Catalog.Products.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Catalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/products")]
[Authorize(Policy = "products.manage")]
public sealed class ManagementProductsController : ControllerBase
{
    private readonly ListProductsQueryHandler _listProductsHandler;
    private readonly GetProductQueryHandler _getProductHandler;
    private readonly CreateProductCommandHandler _createProductHandler;
    private readonly CloneProductTemplateCommandHandler _cloneTemplateHandler;
    private readonly UpdateProductCommandHandler _updateProductHandler;
    private readonly SetProductAvailabilityCommandHandler _setProductAvailabilityHandler;
    private readonly DeleteProductCommandHandler _deleteProductHandler;
    private readonly AddProductVariantCommandHandler _addVariantHandler;
    private readonly UpdateProductVariantCommandHandler _updateVariantHandler;
    private readonly SetProductVariantAvailabilityCommandHandler _setVariantAvailabilityHandler;
    private readonly DeleteProductVariantCommandHandler _deleteVariantHandler;

    public ManagementProductsController(
        ListProductsQueryHandler listProductsHandler,
        GetProductQueryHandler getProductHandler,
        CreateProductCommandHandler createProductHandler,
        CloneProductTemplateCommandHandler cloneTemplateHandler,
        UpdateProductCommandHandler updateProductHandler,
        SetProductAvailabilityCommandHandler setProductAvailabilityHandler,
        DeleteProductCommandHandler deleteProductHandler,
        AddProductVariantCommandHandler addVariantHandler,
        UpdateProductVariantCommandHandler updateVariantHandler,
        SetProductVariantAvailabilityCommandHandler setVariantAvailabilityHandler,
        DeleteProductVariantCommandHandler deleteVariantHandler)
    {
        _listProductsHandler = listProductsHandler;
        _getProductHandler = getProductHandler;
        _createProductHandler = createProductHandler;
        _cloneTemplateHandler = cloneTemplateHandler;
        _updateProductHandler = updateProductHandler;
        _setProductAvailabilityHandler = setProductAvailabilityHandler;
        _deleteProductHandler = deleteProductHandler;
        _addVariantHandler = addVariantHandler;
        _updateVariantHandler = updateVariantHandler;
        _setVariantAvailabilityHandler = setVariantAvailabilityHandler;
        _deleteVariantHandler = deleteVariantHandler;
    }

    [HttpGet]
    public async Task<IActionResult> ListProducts(
        Guid organizationId,
        [FromQuery] string? search,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListProductsQuery
        {
            UserContext = User.GetUserContext(),
            Search = search,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _listProductsHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetProduct(Guid organizationId, Guid productId, CancellationToken cancellationToken)
    {
        var query = new GetProductQuery(productId)
        {
            OrganizationId = organizationId,
            UserContext = User.GetUserContext()
        };
        var result = await _getProductHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(
        Guid organizationId,
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand
        {
            Scope = Scope(organizationId),
            Request = request,
            CreatedByAccountId = GetCurrentAccountId()
        };
        var result = await _createProductHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("from-template")]
    public async Task<IActionResult> CloneTemplate(
        Guid organizationId,
        [FromBody] CloneProductTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cloneTemplateHandler.HandleAsync(new CloneProductTemplateCommand
        {
            Scope = Scope(organizationId),
            Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid organizationId,
        Guid productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand
        {
            Scope = Scope(organizationId),
            ProductId = productId,
            Request = request,
            UpdatedByAccountId = GetCurrentAccountId()
        };
        var result = await _updateProductHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{productId:guid}/availability")]
    public async Task<IActionResult> SetProductAvailability(
        Guid organizationId,
        Guid productId,
        [FromBody] SetAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetProductAvailabilityCommand
        {
            Scope = Scope(organizationId),
            ProductId = productId,
            IsAvailable = request.IsAvailable,
            UpdatedByAccountId = GetCurrentAccountId()
        };
        var result = await _setProductAvailabilityHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid organizationId, Guid productId, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand
        {
            Scope = Scope(organizationId),
            ProductId = productId,
            DeletedByAccountId = GetCurrentAccountId()
        };
        var result = await _deleteProductHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{productId:guid}/variants")]
    public async Task<IActionResult> AddVariant(
        Guid organizationId,
        Guid productId,
        [FromBody] UpsertProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddProductVariantCommand
        {
            Scope = Scope(organizationId),
            ProductId = productId,
            Request = request,
            CreatedByAccountId = GetCurrentAccountId()
        };
        var result = await _addVariantHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{productId:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> UpdateVariant(
        Guid organizationId,
        Guid productId,
        Guid variantId,
        [FromBody] UpdateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductVariantCommand
        {
            Scope = Scope(organizationId),
            ProductId = productId,
            VariantId = variantId,
            Request = request,
            UpdatedByAccountId = GetCurrentAccountId()
        };
        var result = await _updateVariantHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{productId:guid}/variants/{variantId:guid}/availability")]
    public async Task<IActionResult> SetVariantAvailability(
        Guid organizationId,
        Guid productId,
        Guid variantId,
        [FromBody] SetAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetProductVariantAvailabilityCommand
        {
            Scope = Scope(organizationId),
            ProductId = productId,
            VariantId = variantId,
            IsAvailable = request.IsAvailable,
            UpdatedByAccountId = GetCurrentAccountId()
        };
        var result = await _setVariantAvailabilityHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{productId:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> DeleteVariant(
        Guid organizationId,
        Guid productId,
        Guid variantId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteProductVariantCommand
        {
            Scope = Scope(organizationId),
            ProductId = productId,
            VariantId = variantId,
            DeletedByAccountId = GetCurrentAccountId()
        };
        var result = await _deleteVariantHandler.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    private Guid? GetCurrentAccountId()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(accountId, out var parsedAccountId) ? parsedAccountId : null;
    }

    private ProductManagementCommandScope Scope(Guid organizationId) =>
        new(User.GetUserContext(), organizationId);
}
