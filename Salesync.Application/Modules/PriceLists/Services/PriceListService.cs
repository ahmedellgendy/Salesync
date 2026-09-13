using FluentValidation;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.MasterData.PriceLists.Dtos;
using Salesync.Application.Modules.MasterData.PriceLists.Interfaces;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Application.Modules.MasterData.PriceLists.Services;

public class PriceListService : IPriceListService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePriceListDto> _createValidator;
    private readonly IValidator<UpdatePriceListDto> _updateValidator;

    public PriceListService(
        IUnitOfWork unitOfWork,
        IValidator<CreatePriceListDto> createValidator,
        IValidator<UpdatePriceListDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IEnumerable<PriceListResponse>> GetAllAsync()
    {
        var items =
            await _unitOfWork.PriceLists.GetAllAsync();

        return items
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .Select(Map);
    }

    public async Task<PriceListResponse?> GetByIdAsync(int id)
    {
        var entity =
            await _unitOfWork.PriceLists.GetByIdAsync(id);

        return entity is null
            ? null
            : Map(entity);
    }

    public async Task<PriceListResponse> CreateAsync(
        CreatePriceListDto dto)
    {
        await _createValidator
            .ValidateAndThrowAsync(dto);

        dto.Code = dto.Code.Trim().ToUpperInvariant();
        dto.Name = dto.Name.Trim();

        var existing =
            await _unitOfWork.PriceLists
                .FindAsync(x => x.Code == dto.Code);

        if (existing.Any())
        {
            throw new InvalidOperationException(
                $"Price list code '{dto.Code}' already exists.");
        }

        if (dto.IsDefault)
        {
            await ClearExistingDefaultAsync();
        }

        var entity = new PriceList
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description?.Trim(),
            IsDefault = dto.IsDefault,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            IsActive = true
        };

        await _unitOfWork.PriceLists.AddAsync(entity);

        await _unitOfWork.CompleteAsync();

        return Map(entity);
    }

    public async Task<PriceListResponse> UpdateAsync(
        int id,
        UpdatePriceListDto dto)
    {
        await _updateValidator
            .ValidateAndThrowAsync(dto);

        var entity =
            await _unitOfWork.PriceLists.GetByIdAsync(id);

        if (entity is null)
        {
            throw new KeyNotFoundException(
                $"Price list with id {id} was not found.");
        }

        dto.Code = dto.Code.Trim().ToUpperInvariant();
        dto.Name = dto.Name.Trim();

        var duplicate =
            await _unitOfWork.PriceLists.FindAsync(
                x =>
                    x.Code == dto.Code &&
                    x.Id != id);

        if (duplicate.Any())
        {
            throw new InvalidOperationException(
                $"Price list code '{dto.Code}' already exists.");
        }

        if (dto.IsDefault)
        {
            await ClearExistingDefaultAsync(id);
        }

        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.Description = dto.Description?.Trim();
        entity.IsDefault = dto.IsDefault;
        entity.ValidFrom = dto.ValidFrom;
        entity.ValidTo = dto.ValidTo;
        entity.IsActive = dto.IsActive;

        await _unitOfWork.CompleteAsync();

        return Map(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity =
            await _unitOfWork.PriceLists.GetByIdAsync(id);

        if (entity is null)
        {
            throw new KeyNotFoundException(
                $"Price list with id {id} was not found.");
        }

        entity.IsActive = false;
        entity.IsDefault = false;

        await _unitOfWork.CompleteAsync();
    }

    private async Task ClearExistingDefaultAsync(
        int? excludedId = null)
    {
        var currentDefaults =
            await _unitOfWork.PriceLists.FindAsync(
                x =>
                    x.IsDefault &&
                    (!excludedId.HasValue ||
                     x.Id != excludedId.Value));

        foreach (var item in currentDefaults)
        {
            item.IsDefault = false;
        }
    }

    private static PriceListResponse Map(
        PriceList entity)
    {
        return new PriceListResponse
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            IsDefault = entity.IsDefault,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            IsActive = entity.IsActive
        };
    }
}