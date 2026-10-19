using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using ProductManagementApi.Data;
using ProductManagementApi.DTO;
using ProductManagementApi.Exceptions;
using ProductManagementApi.Models;
using ProductManagementApi.Services;

namespace ProductManagementApi.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task CreateAsync_SavesProduct()
    {
        await using var context = CreateContext();
        var service = new ProductService(context);

        var product = await service.CreateAsync(new CreateProductRequest
        {
            Name = "Camera",
            Description = "Mirrorless camera",
            Price = 799.99m,
            Stock = 4
        });

        Assert.True(product.Id > 0);
        Assert.Equal("Camera", product.Name);
        Assert.Equal("Mirrorless camera", product.Description);
        Assert.Equal(799.99m, product.Price);
        Assert.Equal(4, product.Stock);
        Assert.NotEqual(default, product.CreatedAt);
        Assert.Equal(product.Id, (await context.Products.SingleAsync()).Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsProductsOrderedById()
    {
        await using var context = CreateContext();
        context.Products.AddRange(
            NewProduct(200, "Second"),
            NewProduct(100, "First"));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var products = await service.GetAllAsync();

        Assert.Equal(new[] { 100, 200 }, products.Select(product => product.Id));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProductOrNull()
    {
        await using var context = CreateContext();
        context.Products.Add(NewProduct(100, "Camera"));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        Assert.Equal("Camera", (await service.GetByIdAsync(100))?.Name);
        Assert.Null(await service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsAllPartialMatches()
    {
        await using var context = CreateContext();
        context.Products.AddRange(
            NewProduct(100, "Laptop"),
            NewProduct(101, "Lap desk"),
            NewProduct(102, "Camera"));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var results = await service.GetByNameAsync("Lap");

        Assert.Equal(2, results.Count());
        Assert.All(results, product => Assert.Contains("Lap", product.Name));
    }

    [Fact]
    public async Task GetWithStockLevelAsync_IncludesBothBounds()
    {
        await using var context = CreateContext();
        context.Products.AddRange(
            NewProduct(100, "Below", stock: 4),
            NewProduct(101, "At minimum", stock: 5),
            NewProduct(102, "In range", stock: 10),
            NewProduct(103, "At maximum", stock: 20),
            NewProduct(104, "Above", stock: 21));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var results = await service.GetWithStockLevelAsync(5, 20);

        Assert.Equal(new[] { 101, 102, 103 }, results.Select(product => product.Id).Order());
    }

    [Fact]
    public async Task GetWithStockLevelAsync_AppliesEitherOptionalBound()
    {
        await using var context = CreateContext();
        context.Products.AddRange(
            NewProduct(100, "Low", stock: 2),
            NewProduct(101, "Middle", stock: 10),
            NewProduct(102, "High", stock: 20));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var minimumResults = await service.GetWithStockLevelAsync(10, null);
        var maximumResults = await service.GetWithStockLevelAsync(null, 10);

        Assert.Equal(new[] { 101, 102 }, minimumResults.Select(product => product.Id).Order());
        Assert.Equal(new[] { 100, 101 }, maximumResults.Select(product => product.Id).Order());
    }

    [Fact]
    public async Task UpdateByIdAsync_UpdatesProductOrReturnsNull()
    {
        await using var context = CreateContext();
        context.Products.Add(NewProduct(100, "Old name"));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var updated = await service.UpdateByIdAsync(100, new UpdateProductRequest
        {
            Name = "New name",
            Description = "Updated",
            Price = 20m,
            Stock = 8
        });

        Assert.NotNull(updated);
        Assert.Equal("New name", updated.Name);
        Assert.Equal("Updated", updated.Description);
        Assert.Equal(20m, updated.Price);
        Assert.Equal(8, updated.Stock);
        Assert.Null(await service.UpdateByIdAsync(999, new UpdateProductRequest()));
    }

    [Fact]
    public async Task DeleteByIdAsync_RemovesProductOrReturnsNull()
    {
        await using var context = CreateContext();
        context.Products.Add(NewProduct(100, "Camera"));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var deleted = await service.DeleteByIdAsync(100);

        Assert.Equal(100, deleted?.Id);
        Assert.Empty(await context.Products.ToListAsync());
        Assert.Null(await service.DeleteByIdAsync(999));
    }

    [Fact]
    public async Task DecrementStockAsync_DecrementsStock()
    {
        await using var database = await CreateSqliteContextAsync();
        var context = database.Context;
        context.Products.Add(NewProduct(100, "Camera", stock: 5));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var updated = await service.DecrementStockAsync(100, 2);

        Assert.Equal(3, updated?.Stock);
        Assert.Equal(3, await context.Products
            .AsNoTracking()
            .Select(p => p.Stock)
            .SingleAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DecrementStockAsync_RejectsNonPositiveQuantity(int quantity)
    {
        await using var database = await CreateSqliteContextAsync();
        var context = database.Context;
        var service = new ProductService(context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.DecrementStockAsync(100, quantity));
    }

    [Fact]
    public async Task DecrementStockAsync_RejectsInsufficientStock()
    {
        await using var database = await CreateSqliteContextAsync();
        var context = database.Context;
        context.Products.Add(NewProduct(100, "Camera", stock: 2));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        await Assert.ThrowsAsync<InsufficientStockException>(
            () => service.DecrementStockAsync(100, 3));
        Assert.Equal(2, await context.Products
            .AsNoTracking()
            .Select(p => p.Stock)
            .SingleAsync());
    }

    [Fact]
    public async Task DecrementStockAsync_ReturnsNullWhenProductDoesNotExist()
    {
        await using var database = await CreateSqliteContextAsync();
        var context = database.Context;
        var service = new ProductService(context);

        Assert.Null(await service.DecrementStockAsync(999, 1));
    }

    [Fact]
    public async Task DecrementStockAsync_RepeatedRequestsDoNotOversell()
    {
        await using var database1 = await CreateSqliteContextAsync();
        await using var database2 = await CreateSqliteContextAsync(database1.Connection);
        database1.Context.Products.Add(NewProduct(100, "Camera", stock: 5));
        await database1.Context.SaveChangesAsync();

        var service1 = new ProductService(database1.Context);
        var service2 = new ProductService(database2.Context);

        var first = await service1.DecrementStockAsync(100, 3);
        await Assert.ThrowsAsync<InsufficientStockException>(
            () => service2.DecrementStockAsync(100, 3));

        Assert.Equal(2, first?.Stock);
        Assert.Equal(2, await database1.Context.Products
            .AsNoTracking()
            .Where(p => p.Id == 100)
            .Select(p => p.Stock)
            .SingleAsync());
    }

    [Fact]
    public async Task IncrementStockAsync_IncrementsStock()
    {
        await using var database = await CreateSqliteContextAsync();
        var context = database.Context;
        context.Products.Add(NewProduct(100, "Camera", stock: 5));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        var updated = await service.IncrementStockAsync(100, 2);

        Assert.Equal(7, updated?.Stock);
        Assert.Equal(7, await context.Products
            .AsNoTracking()
            .Select(p => p.Stock)
            .SingleAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IncrementStockAsync_RejectsNonPositiveQuantity(int quantity)
    {
        await using var database = await CreateSqliteContextAsync();
        var context = database.Context;
        var service = new ProductService(context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.IncrementStockAsync(100, quantity));
    }

    [Fact]
    public async Task IncrementStockAsync_RejectsIntegerOverflow()
    {
        await using var database = await CreateSqliteContextAsync();
        var context = database.Context;
        context.Products.Add(NewProduct(100, "Camera", stock: int.MaxValue));
        await context.SaveChangesAsync();
        var service = new ProductService(context);

        await Assert.ThrowsAsync<StockLimitExceededException>(
            () => service.IncrementStockAsync(100, 1));
    }

    [Fact]
    public async Task IncrementStockAsync_ReturnsNullWhenProductDoesNotExist()
    {
        await using var database = await CreateSqliteContextAsync();
        var context = database.Context;
        var service = new ProductService(context);

        Assert.Null(await service.IncrementStockAsync(999, 1));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<SqliteTestDatabase> CreateSqliteContextAsync(
        SqliteConnection? connection = null)
    {
        var ownsConnection = connection is null;
        connection ??= new SqliteConnection("Data Source=:memory:");
        if (ownsConnection)
        {
            await connection.OpenAsync();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        if (ownsConnection)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE Products (
                    Id INTEGER NOT NULL PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Price TEXT NOT NULL,
                    Stock INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL
                )
                """);
        }

        return new SqliteTestDatabase(context, connection, ownsConnection);
    }

    private sealed class SqliteTestDatabase(
        ApplicationDbContext context,
        SqliteConnection connection,
        bool ownsConnection) : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; } = context;

        public SqliteConnection Connection { get; } = connection;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            if (ownsConnection)
            {
                await Connection.DisposeAsync();
            }
        }
    }

    private static Product NewProduct(int id, string name, int stock = 0)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Description = $"{name} description",
            Price = 10m,
            Stock = stock,
            CreatedAt = DateTime.UtcNow
        };
    }
}
