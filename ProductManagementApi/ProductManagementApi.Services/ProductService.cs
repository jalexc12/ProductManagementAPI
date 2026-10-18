using ProductManagementApi.Data;
using ProductManagementApi.DTO;
using ProductManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using ProductManagementApi.Exceptions;

namespace ProductManagementApi.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _dbContext;

    public ProductService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return product;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        var products = await _dbContext.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        return products;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var product = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product;
    }

    public async Task<IEnumerable<Product>> GetByNameAsync(string name)
    {
        var products = await _dbContext.Products.AsNoTracking().Where(p => p.Name.Contains(name)).ToListAsync();
        return products;
    }

    public async Task<IEnumerable<Product>> GetWithStockLevelAsync(int? minStock, int? maxStock)
    {
        var query = _dbContext.Products.AsNoTracking().AsQueryable();

        if (minStock.HasValue)
        {
            query = query.Where(p => p.Stock >= minStock.Value);
        }

        if (maxStock.HasValue)
        {
            query = query.Where(p => p.Stock <= maxStock.Value);
        }

        return await query.ToListAsync();
    }


    public async Task<Product?> UpdateByIdAsync(int id, UpdateProductRequest request)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return null;
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;

        await _dbContext.SaveChangesAsync();

        return product;
    }

    public async Task<Product?> DeleteByIdAsync(int id)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return null;
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        return product;
    }

    public async Task<Product?> DecrementStockAsync(int id, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return null;
        }

        if (product.Stock < quantity)
        {
            throw new InsufficientStockException("Insufficient stock.");
        }

        product.Stock -= quantity;

        await _dbContext.SaveChangesAsync();

        return product;
    }

    public async Task<Product?> IncrementStockAsync(int id, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return null;
        }

        if (quantity > int.MaxValue - product.Stock)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Stock cannot exceed the maximum supported value.");
        }

        product.Stock += quantity;

        await _dbContext.SaveChangesAsync();

        return product;
    }
}