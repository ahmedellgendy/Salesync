using AutoMapper;
using FluentValidation;
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

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CreateProductDto> createValidator, IValidator<UpdateProductDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        public async Task<ProductDto> GetByIdAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            return _mapper.Map<ProductDto>(product);
        }
        public async Task<ProductDto> CreateAsync(CreateProductDto createProductDto)
        {
            // FluentValidation - Validate data format
            var validationResult = await _createValidator.ValidateAsync(createProductDto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // Map from Dto to Product 
            var product = _mapper.Map<Product>(createProductDto);

            // Save Product at Db
            await _unitOfWork.Products.CreateAsync(product);
            await _unitOfWork.CompleteAsync();

            // Return Created Product
            return _mapper.Map<ProductDto>(product);

        }
        public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto updateProductDto)
        {

            // FluentValidation - Validate data format
            var validationResult = await _updateValidator.ValidateAsync(updateProductDto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var existingProduct = await _unitOfWork.Products.GetByIdAsync(id);
            if (existingProduct == null)
                throw new KeyNotFoundException($"Product with id {id} Not Found");

            _mapper.Map(updateProductDto, existingProduct);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<ProductDto>(existingProduct);
        }
        public async Task DeleteAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                throw new KeyNotFoundException($"Product with id {id} Not Found");

            _unitOfWork.Products.Delete(product);
            await _unitOfWork.CompleteAsync();

        }
    }
}
