using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductManagementApi.Controllers;
using ProductManagementApi.Data;
using ProductManagementApi.DTO;
using ProductManagementApi.Models;
using Xunit;

namespace ProductManagementApi.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class ProductApiIntegrationTests(SqlServerFixture sqlServer)
{
    [SqlServerFact]
    public async Task ConcurrentCreates_UseDistinctSixDigitSequenceIds()
    {
        await using var api = await CreateApiAsync();
        const int createCount = 20;

        var responses = await Task.WhenAll(
            Enumerable.Range(0, createCount)
                .Select(index => api.Client.PostAsJsonAsync(
                    "/api/products",
                    new CreateProductRequest
                    {
                        Name = $"Concurrent product {index}",
                        Description = "Integration test product",
                        Price = 12.50m,
                        Stock = 1
                    })));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

        var products = await Task.WhenAll(
            responses.Select(response => response.Content.ReadFromJsonAsync<ProductResponse>()));
        var ids = products.Select(product => Assert.IsType<ProductResponse>(product).Id).ToArray();

        Assert.Equal(createCount, ids.Distinct().Count());
        Assert.All(ids, id => Assert.InRange(id, 100000, 999999));

        var sequence = await ReadSequenceMetadataAsync(api.DatabaseName);
        Assert.Equal(100000, sequence.StartValue);
        Assert.Equal(1, sequence.Increment);
        Assert.Equal(999999, sequence.MaximumValue);
        Assert.False(sequence.IsCyclic);
    }

    [SqlServerFact]
    public async Task DatabaseCheckConstraints_RejectInvalidProductIdsAndNegativeStock()
    {
        await using var api = await CreateApiAsync();
        await using var scope = api.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Products.Add(NewProduct(99999, "Invalid ID", stock: 0));
        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        dbContext.ChangeTracker.Clear();

        dbContext.Products.Add(NewProduct(1000000, "Invalid ID", stock: 0));
        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        dbContext.ChangeTracker.Clear();

        dbContext.Products.Add(NewProduct(500000, "Negative stock", stock: -1));
        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task ConcurrentDecrements_SucceedOnlyForAvailableStock()
    {
        await using var api = await CreateApiAsync();
        const int initialStock = 7;
        const int requestCount = 24;

        using var createResponse = await api.Client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest
            {
                Name = "Concurrent stock",
                Description = "Integration test product",
                Price = 10m,
                Stock = initialStock
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(created);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, requestCount)
                .Select(_ => api.Client.PostAsync(
                    $"/api/products/{created.Id}/decrement-stock/1",
                    content: null)));

        Assert.Equal(initialStock, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(requestCount - initialStock,
            responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

        await using var scope = api.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var finalStock = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == created.Id)
            .Select(product => product.Stock)
            .SingleAsync();
        Assert.Equal(0, finalStock);
    }

    private async Task<TestApi> CreateApiAsync()
    {
        await sqlServer.StartAsync();
        var databaseName = $"ProductManagementIntegration_{Guid.NewGuid():N}";
        await CreateDatabaseAsync(databaseName);

        var connectionStringBuilder = new SqlConnectionStringBuilder(sqlServer.ConnectionString)
        {
            InitialCatalog = databaseName,
            TrustServerCertificate = true
        };

        var factory = new WebApplicationFactory<ProductsController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = connectionStringBuilder.ConnectionString
                    }));
            });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        return new TestApi(factory, client, databaseName, sqlServer);
    }

    private async Task CreateDatabaseAsync(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(sqlServer.ConnectionString)
        {
            InitialCatalog = "master",
            TrustServerCertificate = true
        };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();
    }

    private async Task<(long StartValue, long Increment, long MaximumValue, bool IsCyclic)>
        ReadSequenceMetadataAsync(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(sqlServer.ConnectionString)
        {
            InitialCatalog = databaseName,
            TrustServerCertificate = true
        };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CAST(start_value AS bigint),
                   CAST(increment AS bigint),
                   CAST(maximum_value AS bigint),
                   is_cycling
            FROM sys.sequences
            WHERE name = N'ProductIdSequence'
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), "The product ID sequence was not created.");
        return (
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetBoolean(3));
    }

    private static Product NewProduct(int id, string name, int stock) =>
        new()
        {
            Id = id,
            Name = name,
            Description = "Integration test product",
            Price = 10m,
            Stock = stock,
            CreatedAt = DateTime.UtcNow
        };

    private sealed class TestApi(
        WebApplicationFactory<ProductsController> factory,
        HttpClient client,
        string databaseName,
        SqlServerFixture sqlServer) : IAsyncDisposable
    {
        public WebApplicationFactory<ProductsController> Factory { get; } = factory;

        public HttpClient Client { get; } = client;

        public string DatabaseName { get; } = databaseName;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Factory.DisposeAsync();

            var builder = new SqlConnectionStringBuilder(sqlServer.ConnectionString)
            {
                InitialCatalog = "master",
                TrustServerCertificate = true
            };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
                """;
            await command.ExecuteNonQueryAsync();
        }
    }
}
