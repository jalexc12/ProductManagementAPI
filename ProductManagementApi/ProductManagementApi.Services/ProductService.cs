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

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
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

        return ToResponse(product);
    }

    public async Task<IEnumerable<ProductResponse>> GetAllAsync()
    {
        var products = await _dbContext.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        return products.Select(ToResponse);
    }

    public async Task<ProductResponse?> GetByIdAsync(int id)
    {
        var product = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? null : ToResponse(product);
    }

    public async Task<IEnumerable<ProductResponse>> GetByNameAsync(string name)
    {
        var products = await _dbContext.Products.AsNoTracking().Where(p => p.Name.Contains(name)).ToListAsync();
        return products.Select(ToResponse);
    }

    public async Task<IEnumerable<ProductResponse>> GetWithStockLevelAsync(int? minStock, int? maxStock)
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

        var products = await query.ToListAsync();
        return products.Select(ToResponse);
    }


    public async Task<ProductResponse?> UpdateByIdAsync(int id, UpdateProductRequest request)
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

        return ToResponse(product);
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

    public async Task<ProductResponse?> DecrementStockAsync(int id, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var affected = await _dbContext.Products
            .Where(p => p.Id == id && p.Stock >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - quantity));

        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return null;
        }

        if (affected == 0)
        {
            throw new InsufficientStockException("Insufficient stock.");
        }

        return ToResponse(product);
    }

    public async Task<ProductResponse?> IncrementStockAsync(int id, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }
        var affected = await _dbContext.Products
            .Where(p => p.Id == id && p.Stock <= int.MaxValue - quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + quantity));

        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return null;
        }

        if (affected == 0)
        {
            throw new StockLimitExceededException("Stock cannot exceed the maximum supported value.");
        }

        return ToResponse(product);
    }

    private static ProductResponse ToResponse(Product product) =>
        new()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            CreatedAt = product.CreatedAt
        };
}