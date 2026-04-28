using AutoMapper;
using Salesync.Application.Dtos.ProductDto;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Domain.Entities;

namespace Salesync.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
            // Map from Dto to Product to save at db
            var product = _mapper.Map<Product>(createProductDto);

            // Save Product at Db
            await _unitOfWork.Products.CreateAsync(product);
            await _unitOfWork.CompleteAsync();

            // Return Created Product
            return _mapper.Map<ProductDto>(product);

        }
        public async Task<ProductDto> UpdateAsync(UpdateProductDto updateProductDto)
        {
            var product = _mapper.Map<Product>(updateProductDto);

            _unitOfWork.Products.UpdateAsync(product);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                throw new Exception($"Product with id {id} Not Found");

            _unitOfWork.Products.DeleteAsync(product);
            await _unitOfWork.CompleteAsync();

            return true;
        }


    }
}
