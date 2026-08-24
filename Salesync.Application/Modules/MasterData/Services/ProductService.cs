using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.MasterData.Dtos.ProductDto;
using Salesync.Application.Modules.MasterData.Interfaces.Services;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Application.Modules.MasterData.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateProductDto> _createValidator;
        private readonly IValidator<UpdateProductDto> _updateValidator;

        public ProductService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateProductDto> createValidator,
            IValidator<UpdateProductDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await _unitOfWork.Products
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Warehouse)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<ProductDto> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid product id.");

            var product = await _unitOfWork.Products
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (product == null)
            {
                throw new KeyNotFoundException(
                    $"Product with ID {id} not found.");
            }

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> CreateAsync(
            CreateProductDto createProductDto)
        {
            var validationResult =
                await _createValidator.ValidateAsync(
                    createProductDto);

            if (!validationResult.IsValid)
                throw new ValidationException(
                    validationResult.Errors);

            NormalizeCreateDto(createProductDto);

            await ValidateUniqueFieldsAsync(
                createProductDto.ItemCode,
                createProductDto.SKU,
                createProductDto.Barcode);

            Warehouse? warehouse = null;

            if (createProductDto.WarehouseId.HasValue)
            {
                warehouse = await _unitOfWork.Warehouses
                    .GetByIdAsync(
                        createProductDto.WarehouseId.Value);

                if (warehouse == null)
                {
                    throw new KeyNotFoundException(
                        $"Warehouse with ID {createProductDto.WarehouseId.Value} does not exist.");
                }
            }

            var product =
                _mapper.Map<Product>(createProductDto);

            if (warehouse != null)
            {
                product.Warehouse = warehouse;
            }

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> UpdateAsync(
            int id,
            UpdateProductDto updateProductDto)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid product id.");

            var validationResult =
                await _updateValidator.ValidateAsync(
                    updateProductDto);

            if (!validationResult.IsValid)
                throw new ValidationException(
                    validationResult.Errors);

            var existingProduct = await _unitOfWork.Products
                .GetQueryable()
                .Include(x => x.Warehouse)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (existingProduct == null)
            {
                throw new KeyNotFoundException(
                    $"Product with ID {id} not found.");
            }

            NormalizeUpdateDto(updateProductDto);

            await ValidateUniqueFieldsAsync(
                updateProductDto.ItemCode,
                updateProductDto.SKU,
                updateProductDto.Barcode,
                id);

            var finalUnitPrice =
                updateProductDto.UnitPrice
                ?? existingProduct.UnitPrice;

            var finalCostPrice =
                updateProductDto.CostPrice
                ?? existingProduct.CostPrice;

            if (finalCostPrice > finalUnitPrice)
            {
                throw new ValidationException(
                    "Cost price cannot be greater than unit price.");
            }

            var finalMinStock =
                updateProductDto.MinStockLevel
                ?? existingProduct.MinStockLevel;

            var finalMaxStock =
                updateProductDto.MaxStockLevel
                ?? existingProduct.MaxStockLevel;

            if (finalMaxStock > 0 &&
                finalMaxStock < finalMinStock)
            {
                throw new ValidationException(
                    "Max stock must be greater than or equal to min stock.");
            }

            Warehouse? warehouse = null;

            if (updateProductDto.WarehouseId.HasValue)
            {
                warehouse = await _unitOfWork.Warehouses
                    .GetByIdAsync(
                        updateProductDto.WarehouseId.Value);

                if (warehouse == null)
                {
                    throw new KeyNotFoundException(
                        $"Warehouse with ID {updateProductDto.WarehouseId.Value} does not exist.");
                }
            }

            _mapper.Map(
                updateProductDto,
                existingProduct);

            if (warehouse != null)
            {
                existingProduct.Warehouse = warehouse;
            }

            existingProduct.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Products.Update(existingProduct);

            await _unitOfWork.CompleteAsync();

            return _mapper.Map<ProductDto>(
                existingProduct);
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid product id.");

            var product = await _unitOfWork.Products
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (product == null)
            {
                throw new KeyNotFoundException(
                    $"Product with ID {id} not found.");
            }

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Products.Update(product);

            await _unitOfWork.CompleteAsync();
        }

        private async Task ValidateUniqueFieldsAsync(
            string? itemCode,
            string? sku,
            string? barcode,
            int? excludedProductId = null)
        {
            var query =
                _unitOfWork.Products.GetQueryable();

            if (!string.IsNullOrWhiteSpace(itemCode))
            {
                var exists = await query.AnyAsync(x =>
                    x.ItemCode == itemCode &&
                    (!excludedProductId.HasValue ||
                     x.Id != excludedProductId.Value));

                if (exists)
                {
                    throw new InvalidOperationException(
                        $"Product item code '{itemCode}' already exists.");
                }
            }

            if (!string.IsNullOrWhiteSpace(sku))
            {
                var exists = await query.AnyAsync(x =>
                    x.SKU == sku &&
                    (!excludedProductId.HasValue ||
                     x.Id != excludedProductId.Value));

                if (exists)
                {
                    throw new InvalidOperationException(
                        $"Product SKU '{sku}' already exists.");
                }
            }

            if (!string.IsNullOrWhiteSpace(barcode))
            {
                var exists = await query.AnyAsync(x =>
                    x.Barcode == barcode &&
                    (!excludedProductId.HasValue ||
                     x.Id != excludedProductId.Value));

                if (exists)
                {
                    throw new InvalidOperationException(
                        $"Product barcode '{barcode}' already exists.");
                }
            }
        }

        private static void NormalizeCreateDto(
            CreateProductDto dto)
        {
            dto.ItemCode = dto.ItemCode.Trim();
            dto.Name = dto.Name.Trim();

            dto.Description =
                NormalizeOptional(dto.Description);

            dto.SKU =
                NormalizeOptional(dto.SKU);

            dto.Barcode =
                NormalizeOptional(dto.Barcode);

            dto.Unit =
                NormalizeOptional(dto.Unit);

            dto.SmallUnit = dto.SmallUnit.Trim();
            dto.LargeUnit = dto.LargeUnit.Trim();
        }

        private static void NormalizeUpdateDto(
            UpdateProductDto dto)
        {
            dto.ItemCode =
                NormalizeOptional(dto.ItemCode);

            dto.Name =
                NormalizeOptional(dto.Name);

            dto.Description =
                NormalizeOptional(dto.Description);

            dto.SKU =
                NormalizeOptional(dto.SKU);

            dto.Barcode =
                NormalizeOptional(dto.Barcode);

            dto.Unit =
                NormalizeOptional(dto.Unit);

            dto.SmallUnit =
                NormalizeOptional(dto.SmallUnit);

            dto.LargeUnit =
                NormalizeOptional(dto.LargeUnit);
        }

        private static string? NormalizeOptional(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}