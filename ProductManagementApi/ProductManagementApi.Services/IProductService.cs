

using ProductManagementApi.DTO;
using ProductManagementApi.Models;

namespace ProductManagementApi.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductResponse>> GetAllAsync();
        Task<ProductResponse?> GetByIdAsync(int id);
        Task<IEnumerable<ProductResponse>> GetByNameAsync(string name);
        Task<IEnumerable<ProductResponse>> GetWithStockLevelAsync(int? minStock, int? maxStock);
        Task<ProductResponse> CreateAsync(CreateProductRequest request);
        Task<ProductResponse?> UpdateByIdAsync(int id, UpdateProductRequest request);
        Task<Product?> DeleteByIdAsync(int id);
        Task<ProductResponse?> DecrementStockAsync(int id, int quantity);
        Task<ProductResponse?> IncrementStockAsync(int id, int quantity);
    }
}