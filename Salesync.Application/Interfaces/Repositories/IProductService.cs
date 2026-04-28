using Salesync.Application.Dtos.ProductDto;

namespace Salesync.Application.Interfaces.Repositories
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllAsync();
        Task<ProductDto> GetByIdAsync(int id);
        Task<ProductDto> CreateAsync(CreateProductDto createProductDto);
        Task<ProductDto> UpdateAsync(UpdateProductDto updateProductDto);
        Task DeleteAsync(int id);
    }
}
