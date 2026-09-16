using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;
using Microsoft.AspNetCore.Mvc;
using Rudoger.BuildingBlocks.Application;
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
    [ProducesResponseType<ProductView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductView>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        ProductView product = await service.CreateAsync(
            new CreateProductCommand(
                request.Sku,
                request.Name,
                request.BaseUomCode,
                request.BasePriceAmount,
                request.BasePriceCurrencyCode,
                request.Packagings.Select(ToInput).ToArray()),
            cancellationToken);
        return Created($"/api/v1/product/products/{product.Id}", product);
    }

    [HttpGet]
    [ProducesResponseType<Page<ProductView>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<Page<ProductView>>> ListAsync(
        [FromQuery] Guid[]? ids,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(ids, pageNumber, pageSize, cancellationToken));
    }

    [HttpGet("{productId:guid}")]
    [ProducesResponseType<ProductView>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductView>> GetAsync(Guid productId, CancellationToken cancellationToken)
    {
        return Ok(await service.GetAsync(productId, cancellationToken));
    }

    [HttpPatch("{productId:guid}")]
    [Consumes("application/json-patch+json")]
    [ProducesResponseType<ProductView>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductView>> PatchAsync(
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
        return Ok(await service.ChangeAsync(
            productId,
            new ChangeProductCommand(model.Sku, model.Name, model.BasePriceAmount, model.BasePriceCurrencyCode),
            cancellationToken));
    }

    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid productId, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(productId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{productId:guid}/packagings")]
    [ProducesResponseType<ProductPackagingView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductPackagingView>> AddPackagingAsync(
        Guid productId,
        ProductPackagingRequest request,
        CancellationToken cancellationToken)
    {
        ProductPackagingView packaging = await service.AddPackagingAsync(productId, ToInput(request), cancellationToken);
        return Created($"/api/v1/product/products/{productId}/packagings/{packaging.Id}", packaging);
    }

    [HttpGet("{productId:guid}/packagings")]
    public async Task<ActionResult<IReadOnlyList<ProductPackagingView>>> ListPackagingsAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        ProductView product = await service.GetAsync(productId, cancellationToken);
        return Ok(product.Packagings);
    }

    [HttpGet("{productId:guid}/packagings/{packagingId:guid}")]
    public async Task<ActionResult<ProductPackagingView>> GetPackagingAsync(
        Guid productId,
        Guid packagingId,
        CancellationToken cancellationToken)
    {
        ProductView product = await service.GetAsync(productId, cancellationToken);
        ProductPackagingView packaging = product.Packagings.SingleOrDefault(item => item.Id == packagingId)
            ?? throw new KeyNotFoundException($"Packaging '{packagingId}' was not found.");
        return Ok(packaging);
    }

    [HttpPatch("{productId:guid}/packagings/{packagingId:guid}")]
    [Consumes("application/json-patch+json")]
    public async Task<ActionResult<ProductPackagingView>> PatchPackagingAsync(
        Guid productId,
        Guid packagingId,
        JsonPatchDocument<PackagingPatchModel> patch,
        CancellationToken cancellationToken)
    {
        EnsurePatchIsSafe(patch, PackagingPatchPaths);
        ProductPackagingView current = (await service.GetAsync(productId, cancellationToken)).Packagings
            .SingleOrDefault(item => item.Id == packagingId)
            ?? throw new KeyNotFoundException($"Packaging '{packagingId}' was not found.");
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
        return Ok(await service.ChangePackagingAsync(
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
            cancellationToken));
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
        ArgumentNullException.ThrowIfNull(patch);
        if (patch.Operations.Count == 0
            || patch.Operations.Any(operation =>
                !allowedPaths.Contains(operation.path)
                || operation.OperationType is not (OperationType.Replace or OperationType.Test)))
        {
            throw new ArgumentException("The JSON Patch document contains an unsupported operation or path.", nameof(patch));
        }
    }
}
