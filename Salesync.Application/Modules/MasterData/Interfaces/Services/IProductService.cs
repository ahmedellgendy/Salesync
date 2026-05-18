using Salesync.Application.Modules.MasterData.Dtos.ProductDto;

namespace Salesync.Application.Modules.MasterData.Interfaces.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllAsync();
        Task<ProductDto> GetByIdAsync(int id);
        Task<ProductDto> CreateAsync(CreateProductDto createProductDto);
        Task<ProductDto> UpdateAsync(int id, UpdateProductDto updateProductDto);
        Task DeleteAsync(int id);
    }
}
