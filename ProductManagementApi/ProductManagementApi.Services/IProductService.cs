

using ProductManagementApi.DTO;
using ProductManagementApi.Models;

namespace ProductManagementApi.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<IEnumerable<Product>> GetByNameAsync(string name);
        Task<IEnumerable<Product>> GetWithStockLevelAsync(int? minStock, int? maxStock);
        Task<Product> CreateAsync(CreateProductRequest request);
        Task<Product?> UpdateByIdAsync(int id, UpdateProductRequest request);
        Task<Product?> DeleteByIdAsync(int id);
        Task<Product?> DecrementStockAsync(int id, int quantity);
        Task<Product?> IncrementStockAsync(int id, int quantity);
    }
}