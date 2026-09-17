using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;
using Microsoft.AspNetCore.Mvc;
using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.BuildingBlocks.Presentation;
using Rudoger.Modules.Product.Application;

namespace Rudoger.Modules.Product.Presentation;

[ApiController]
[Authorize]
[Route("api/v1/product/products")]
public sealed class ProductsController(ProductApplicationService service) : ControllerBase
{
    private static readonly HashSet<string> ProductPatchPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/sku",
        "/name",
        "/basePriceAmount",
        "/basePriceCurrencyCode",
    };

    private static readonly HashSet<string> PackagingPatchPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/level",
        "/uomCode",
        "/conversionFactor",
        "/barcode",
        "/weightInKg",
        "/lengthInMm",
        "/widthInMm",
        "/heightInMm",
    };

    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductResponse>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ProductView product = await service.CreateAsync(
            new CreateProductCommand(
                request.Sku,
                request.Name,
                request.BaseUomCode,
                request.BasePriceAmount,
                request.BasePriceCurrencyCode,
                request.Packagings.Select(ToInput).ToArray()),
            cancellationToken);
        return Created($"/api/v1/product/products/{product.Id}", ProductResponse.From(product));
    }

    [HttpGet]
    [ProducesResponseType<PageResponse<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<ProductResponse>>> ListAsync(
        [FromQuery] Guid[]? ids,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        Page<ProductView> page = await service.ListAsync(ids, pageNumber, pageSize, cancellationToken);
        return Ok(PageResponseFactory.From(page, ProductResponse.From));
    }

    [HttpGet("{productId:guid}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponse>> GetAsync(Guid productId, CancellationToken cancellationToken)
    {
        return Ok(ProductResponse.From(await service.GetAsync(productId, cancellationToken)));
    }

    [HttpPatch("{productId:guid}")]
    [Consumes("application/json-patch+json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponse>> PatchAsync(
        Guid productId,
        JsonPatchDocument<ProductPatchModel> patch,
        CancellationToken cancellationToken)
    {
        EnsurePatchIsSafe(patch, ProductPatchPaths);
        ProductView current = await service.GetAsync(productId, cancellationToken);
        var model = new ProductPatchModel
        {
            Sku = current.Sku,
            Name = current.Name,
            BasePriceAmount = current.BasePriceAmount,
            BasePriceCurrencyCode = current.BasePriceCurrencyCode,
        };
        patch.ApplyTo(model);
        ProductView product = await service.ChangeAsync(
            productId,
            new ChangeProductCommand(model.Sku, model.Name, model.BasePriceAmount, model.BasePriceCurrencyCode),
            cancellationToken);
        return Ok(ProductResponse.From(product));
    }

    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid productId, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(productId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{productId:guid}/packagings")]
    [ProducesResponseType<ProductPackagingResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductPackagingResponse>> AddPackagingAsync(
        Guid productId,
        ProductPackagingRequest request,
        CancellationToken cancellationToken)
    {
        ProductPackagingView packaging = await service.AddPackagingAsync(productId, ToInput(request), cancellationToken);
        return Created(
            $"/api/v1/product/products/{productId}/packagings/{packaging.Id}",
            ProductPackagingResponse.From(packaging));
    }

    [HttpGet("{productId:guid}/packagings")]
    public async Task<ActionResult<IReadOnlyList<ProductPackagingResponse>>> ListPackagingsAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        ProductView product = await service.GetAsync(productId, cancellationToken);
        return Ok(product.Packagings.Select(ProductPackagingResponse.From).ToArray());
    }

    [HttpGet("{productId:guid}/packagings/{packagingId:guid}")]
    public async Task<ActionResult<ProductPackagingResponse>> GetPackagingAsync(
        Guid productId,
        Guid packagingId,
        CancellationToken cancellationToken)
    {
        ProductView product = await service.GetAsync(productId, cancellationToken);
        ProductPackagingView packaging = product.Packagings.SingleOrDefault(item => item.Id == packagingId)
            ?? throw new NotFoundException(
                ProductApiFailureCode.PackagingNotFound,
                $"Packaging '{packagingId}' was not found.");
        return Ok(ProductPackagingResponse.From(packaging));
    }

    [HttpPatch("{productId:guid}/packagings/{packagingId:guid}")]
    [Consumes("application/json-patch+json")]
    public async Task<ActionResult<ProductPackagingResponse>> PatchPackagingAsync(
        Guid productId,
        Guid packagingId,
        JsonPatchDocument<PackagingPatchModel> patch,
        CancellationToken cancellationToken)
    {
        EnsurePatchIsSafe(patch, PackagingPatchPaths);
        ProductPackagingView current = (await service.GetAsync(productId, cancellationToken)).Packagings
            .SingleOrDefault(item => item.Id == packagingId)
            ?? throw new NotFoundException(
                ProductApiFailureCode.PackagingNotFound,
                $"Packaging '{packagingId}' was not found.");
        var model = new PackagingPatchModel
        {
            Level = current.Level,
            UomCode = current.UomCode,
            ConversionFactor = current.ConversionFactor,
            Barcode = current.Barcode,
            WeightInKg = current.WeightInKg,
            LengthInMm = current.LengthInMm,
            WidthInMm = current.WidthInMm,
            HeightInMm = current.HeightInMm,
        };
        patch.ApplyTo(model);
        ProductPackagingView packaging = await service.ChangePackagingAsync(
            productId,
            packagingId,
            new ChangePackagingCommand(
                model.Level,
                model.UomCode,
                model.ConversionFactor,
                model.Barcode,
                model.WeightInKg,
                model.LengthInMm,
                model.WidthInMm,
                model.HeightInMm),
            cancellationToken);
        return Ok(ProductPackagingResponse.From(packaging));
    }

    [HttpDelete("{productId:guid}/packagings/{packagingId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePackagingAsync(
        Guid productId,
        Guid packagingId,
        CancellationToken cancellationToken)
    {
        await service.RemovePackagingAsync(productId, packagingId, cancellationToken);
        return NoContent();
    }

    private static PackagingInput ToInput(ProductPackagingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new PackagingInput(
            request.Id,
            request.Level,
            request.UomCode,
            request.ConversionFactor,
            request.Barcode,
            request.WeightInKg,
            request.LengthInMm,
            request.WidthInMm,
            request.HeightInMm);
    }

    private static void EnsurePatchIsSafe<T>(JsonPatchDocument<T> patch, HashSet<string> allowedPaths)
        where T : class
    {
        if (patch is null)
        {
            throw new ValidationException("patch-document-required", "A JSON Patch document is required.");
        }

        if (patch.Operations.Count == 0
            || patch.Operations.Any(operation =>
                !allowedPaths.Contains(operation.path)
                || operation.OperationType is not (OperationType.Replace or OperationType.Test)))
        {
            throw new ValidationException(
                "patch-operation-unsupported",
                "The JSON Patch document contains an unsupported operation or path.");
        }
    }
}
