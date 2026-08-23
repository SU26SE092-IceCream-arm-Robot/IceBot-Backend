using Application.Catalog.Products.Commands;
using Application.Catalog.Products.Queries;
using Application.Catalog.Products.Requests;
using Asp.Versioning;
using Domain.Tenants.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Catalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/product-templates")]
public sealed class ManagementProductTemplatesController : ControllerBase
{
    private readonly ListProductsQueryHandler _list;
    private readonly GetProductQueryHandler _get;
    private readonly CreateProductCommandHandler _create;
    private readonly UpdateProductCommandHandler _update;
    private readonly SetProductAvailabilityCommandHandler _setAvailability;
    private readonly DeleteProductCommandHandler _delete;
    private readonly AddProductVariantCommandHandler _addVariant;
    private readonly UpdateProductVariantCommandHandler _updateVariant;
    private readonly SetProductVariantAvailabilityCommandHandler _setVariantAvailability;
    private readonly DeleteProductVariantCommandHandler _deleteVariant;
    private readonly ReplaceCatalogImageCommandHandler _replaceImage;
    private readonly RemoveCatalogImageCommandHandler _removeImage;

    public ManagementProductTemplatesController(
        ListProductsQueryHandler list,
        GetProductQueryHandler get,
        CreateProductCommandHandler create,
        UpdateProductCommandHandler update,
        SetProductAvailabilityCommandHandler setAvailability,
        DeleteProductCommandHandler delete,
        AddProductVariantCommandHandler addVariant,
        UpdateProductVariantCommandHandler updateVariant,
        SetProductVariantAvailabilityCommandHandler setVariantAvailability,
        DeleteProductVariantCommandHandler deleteVariant,
        ReplaceCatalogImageCommandHandler replaceImage,
        RemoveCatalogImageCommandHandler removeImage)
    {
        _list = list;
        _get = get;
        _create = create;
        _update = update;
        _setAvailability = setAvailability;
        _delete = delete;
        _addVariant = addVariant;
        _updateVariant = updateVariant;
        _setVariantAvailability = setVariantAvailability;
        _deleteVariant = deleteVariant;
        _replaceImage = replaceImage;
        _removeImage = removeImage;
    }

    [HttpGet]
    [Authorize(Policy = "product-templates.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _list.HandleAsync(new ListProductsQuery
        {
            UserContext = User.GetUserContext(),
            Search = search,
            GlobalTemplatesOnly = true,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{productId:guid}")]
    [Authorize(Policy = "product-templates.read")]
    public async Task<IActionResult> Get(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _get.HandleAsync(new GetProductQuery(productId)
        {
            UserContext = User.GetUserContext(),
            IsGlobalTemplate = true
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Policy = "product-templates.manage")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        request.StoreId = null;
        request.KioskId = null;
        var result = await _create.HandleAsync(new CreateProductCommand
        {
            Scope = Scope(),
            Request = request,
            CreatedByAccountId = User.GetUserContext().AccountId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{productId:guid}")]
    [Authorize(Policy = "product-templates.manage")]
    public async Task<IActionResult> Update(Guid productId, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _update.HandleAsync(new UpdateProductCommand
        {
            Scope = Scope(),
            ProductId = productId,
            Request = request,
            UpdatedByAccountId = User.GetUserContext().AccountId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{productId:guid}/availability")]
    [Authorize(Policy = "product-templates.manage")]
    public async Task<IActionResult> SetAvailability(Guid productId, [FromBody] SetAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var result = await _setAvailability.HandleAsync(new SetProductAvailabilityCommand
        {
            Scope = Scope(),
            ProductId = productId,
            IsAvailable = request.IsAvailable,
            UpdatedByAccountId = User.GetUserContext().AccountId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{productId:guid}")]
    [Authorize(Policy = "product-templates.manage")]
    public async Task<IActionResult> Delete(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _delete.HandleAsync(new DeleteProductCommand
        {
            Scope = Scope(),
            ProductId = productId,
            DeletedByAccountId = User.GetUserContext().AccountId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{productId:guid}/image")]
    [Authorize(Policy = "product-templates.manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_500_000)]
    public async Task<IActionResult> ReplaceImage(Guid productId, [FromForm] CatalogImageUploadForm form,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        if (form.File is null) return BadRequest("An image file is required.");
        await using var stream = new MemoryStream();
        await form.File.CopyToAsync(stream, cancellationToken);
        var result = await _replaceImage.ReplaceProductAsync(Scope(), productId, form.ExpectedRevision, form.AltText,
            stream.ToArray(), form.File.FileName, form.File.ContentType, idempotencyKey, User.GetUserContext().AccountId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{productId:guid}/image")]
    [Authorize(Policy = "product-templates.manage")]
    public async Task<IActionResult> RemoveImage(Guid productId, [FromHeader(Name = "If-Match")] int expectedRevision,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await _removeImage.RemoveProductAsync(
            Scope(), productId, expectedRevision, idempotencyKey, User.GetUserContext().AccountId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{productId:guid}/variants")]
    [Authorize(Policy = "product-templates.manage")]
    public async Task<IActionResult> AddVariant(Guid productId, [FromBody] UpsertProductVariantRequest request, CancellationToken cancellationToken)
    {
        var result = await _addVariant.HandleAsync(new AddProductVariantCommand
        {
            Scope = Scope(),
            ProductId = productId,
            Request = request,
            CreatedByAccountId = User.GetUserContext().AccountId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{productId:guid}/variants/{variantId:guid}")]
    [Authorize(Policy = "product-templates.manage")]
    public async Task<IActionResult> UpdateVariant(Guid productId, Guid variantId, [FromBody] UpdateProductVariantRequest request, CancellationToken cancellationToken)
    {
        var result = await _updateVariant.HandleAsync(new UpdateProductVariantCommand
        {
            Scope = Scope(),
            ProductId = productId,
            VariantId = variantId,
            Request = request,
            UpdatedByAccountId = User.GetUserContext().AccountId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{productId:guid}/variants/{variantId:guid}/availability")]
    [Authorize(Policy = "product-templates.manage")]
    public async Task<IActionResult> SetVariantAvailability(Guid productId, Guid variantId, [FromBody] SetAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var result = await _setVariantAvailability.HandleAsync(new SetProductVariantAvailabilityCommand
        {
            Scope = Scope(),
            ProductId = productId,
            VariantId = variantId,
            IsAvailable = request.IsAvailable,
            UpdatedByAccountId = User.GetUserContext().AccountId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{productId:guid}/variants/{variantId:guid}")]
    [Authorize(Policy = "product-templates.manage")]
    public async Task<IActionResult> DeleteVariant(Guid productId, Guid variantId, CancellationToken cancellationToken)
    {
        var result = await _deleteVariant.HandleAsync(new DeleteProductVariantCommand
        {
            Scope = Scope(),
            ProductId = productId,
            VariantId = variantId,
            DeletedByAccountId = User.GetUserContext().AccountId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{productId:guid}/variants/{variantId:guid}/image")]
    [Authorize(Policy = "product-templates.manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_500_000)]
    public async Task<IActionResult> ReplaceVariantImage(Guid productId, Guid variantId, [FromForm] CatalogImageUploadForm form,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        if (form.File is null) return BadRequest("An image file is required.");
        await using var stream = new MemoryStream();
        await form.File.CopyToAsync(stream, cancellationToken);
        var result = await _replaceImage.ReplaceVariantAsync(Scope(), productId, variantId, form.ExpectedRevision,
            form.AltText, stream.ToArray(), form.File.FileName, form.File.ContentType, idempotencyKey,
            User.GetUserContext().AccountId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{productId:guid}/variants/{variantId:guid}/image")]
    [Authorize(Policy = "product-templates.manage")]
    public async Task<IActionResult> RemoveVariantImage(Guid productId, Guid variantId,
        [FromHeader(Name = "If-Match")] int expectedRevision,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _removeImage.RemoveVariantAsync(
            Scope(), productId, variantId, expectedRevision, idempotencyKey, User.GetUserContext().AccountId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private ProductManagementCommandScope Scope() => new(User.GetUserContext(), null, IsGlobalTemplate: true);
}
