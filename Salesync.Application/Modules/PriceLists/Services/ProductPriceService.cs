using FluentValidation;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.MasterData.PriceLists.Dtos;
using Salesync.Application.Modules.MasterData.PriceLists.Interfaces;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Application.Modules.MasterData.PriceLists.Services;

public class ProductPriceService : IProductPriceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateProductPriceDto> _createValidator;
    private readonly IValidator<UpdateProductPriceDto> _updateValidator;

    public ProductPriceService(
        IUnitOfWork unitOfWork,
        IValidator<CreateProductPriceDto> createValidator,
        IValidator<UpdateProductPriceDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IEnumerable<ProductPriceResponse>> GetAllAsync(
        int? priceListId = null,
        int? productId = null)
    {
        var items =
            await _unitOfWork.ProductPrices.GetAllAsync();

        var query = items.AsEnumerable();

        if (priceListId.HasValue)
        {
            query = query.Where(x =>
                x.PriceListId == priceListId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x =>
                x.ProductId == productId.Value);
        }

        var result = new List<ProductPriceResponse>();

        foreach (var item in query)
        {
            result.Add(await MapAsync(item));
        }

        return result
            .OrderBy(x => x.PriceListName)
            .ThenBy(x => x.ProductName);
    }

    public async Task<ProductPriceResponse?> GetByIdAsync(int id)
    {
        var entity =
            await _unitOfWork.ProductPrices.GetByIdAsync(id);

        if (entity is null)
            return null;

        return await MapAsync(entity);
    }

    public async Task<ProductPriceResponse> CreateAsync(
        CreateProductPriceDto dto)
    {
        await _createValidator
            .ValidateAndThrowAsync(dto);

        var priceList =
            await _unitOfWork.PriceLists
                .GetByIdAsync(dto.PriceListId);

        if (priceList is null || !priceList.IsActive)
        {
            throw new InvalidOperationException(
                "Price list was not found or is inactive.");
        }

        var product =
            await _unitOfWork.Products
                .GetByIdAsync(dto.ProductId);

        if (product is null || !product.IsActive)
        {
            throw new InvalidOperationException(
                "Product was not found or is inactive.");
        }

        var existing =
            await _unitOfWork.ProductPrices.FindAsync(
                x =>
                    x.PriceListId == dto.PriceListId &&
                    x.ProductId == dto.ProductId);

        if (existing.Any())
        {
            throw new InvalidOperationException(
                "This product already has a price in this price list.");
        }

        var entity = new ProductPrice
        {
            PriceListId = dto.PriceListId,
            ProductId = dto.ProductId,
            UnitPrice = dto.UnitPrice,
            DiscountPercentage = dto.DiscountPercentage,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            IsActive = true
        };

        await _unitOfWork.ProductPrices
            .AddAsync(entity);

        await _unitOfWork.CompleteAsync();

        return await MapAsync(entity);
    }

    public async Task<ProductPriceResponse> UpdateAsync(
        int id,
        UpdateProductPriceDto dto)
    {
        await _updateValidator
            .ValidateAndThrowAsync(dto);

        var entity =
            await _unitOfWork.ProductPrices
                .GetByIdAsync(id);

        if (entity is null)
        {
            throw new KeyNotFoundException(
                $"Product price with id {id} was not found.");
        }

        entity.UnitPrice =
            dto.UnitPrice;

        entity.DiscountPercentage =
            dto.DiscountPercentage;

        entity.ValidFrom =
            dto.ValidFrom;

        entity.ValidTo =
            dto.ValidTo;

        entity.IsActive =
            dto.IsActive;

        await _unitOfWork.CompleteAsync();

        return await MapAsync(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity =
            await _unitOfWork.ProductPrices
                .GetByIdAsync(id);

        if (entity is null)
        {
            throw new KeyNotFoundException(
                $"Product price with id {id} was not found.");
        }

        entity.IsActive = false;

        await _unitOfWork.CompleteAsync();
    }

    private async Task<ProductPriceResponse> MapAsync(
        ProductPrice entity)
    {
        var priceList =
            await _unitOfWork.PriceLists
                .GetByIdAsync(entity.PriceListId);

        var product =
            await _unitOfWork.Products
                .GetByIdAsync(entity.ProductId);

        return new ProductPriceResponse
        {
            Id = entity.Id,

            PriceListId =
                entity.PriceListId,

            PriceListCode =
                priceList?.Code
                ?? string.Empty,

            PriceListName =
                priceList?.Name
                ?? string.Empty,

            ProductId =
                entity.ProductId,

            ItemCode =
                product?.ItemCode
                ?? string.Empty,

            ProductName =
                product?.Name
                ?? string.Empty,

            UnitPrice =
                entity.UnitPrice,

            DiscountPercentage =
                entity.DiscountPercentage,

            ValidFrom =
                entity.ValidFrom,

            ValidTo =
                entity.ValidTo,

            IsActive =
                entity.IsActive
        };
    }
}