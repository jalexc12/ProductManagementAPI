using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductManagementApi.Controllers;
using ProductManagementApi.DTO;
using ProductManagementApi.Exceptions;
using ProductManagementApi.Middleware;
using ProductManagementApi.Models;
using ProductManagementApi.Services;

namespace ProductManagementApi.Tests;

public class ApiErrorHandlingTests
{
    [Fact]
    public async Task ApiExceptionHandler_ReturnsGenericProblemDetailsAndLogsException()
    {
        var logger = new RecordingLogger<ApiExceptionHandler>();
        using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        var handler = new ApiExceptionHandler(
            logger,
            serviceProvider.GetRequiredService<IProblemDetailsService>());
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/products";
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var exception = new InvalidOperationException("Sensitive database details");

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Same(exception, logger.Exception);

        responseBody.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(responseBody);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.Equal("An unexpected error occurred while processing the request.", problem.Detail);
        Assert.Equal("/api/products", problem.Instance);
        Assert.DoesNotContain("Sensitive database details", await ReadBodyAsync(responseBody));
    }

    [Fact]
    public async Task DecrementStock_ReturnsConflictProblemDetailsWhenStockIsInsufficient()
    {
        var result = await InvokeControllerAsync(
            new InsufficientStockException("Insufficient stock."),
            incrementException: null,
            decrement: true);

        AssertProblem(result, StatusCodes.Status409Conflict, "Insufficient stock.");
    }

    [Fact]
    public async Task DecrementStock_ReturnsBadRequestProblemDetailsForInvalidQuantity()
    {
        var result = await InvokeControllerAsync(
            new ArgumentOutOfRangeException("quantity", "Quantity must be greater than zero."),
            incrementException: null,
            decrement: true);

        AssertProblem(result, StatusCodes.Status400BadRequest, "Quantity must be greater than zero.");
    }

    [Fact]
    public async Task AddToStock_ReturnsConflictProblemDetailsWhenStockLimitIsExceeded()
    {
        var result = await InvokeControllerAsync(
            decrementException: null,
            incrementException: new StockLimitExceededException("Stock cannot exceed the maximum supported value."),
            decrement: false);

        AssertProblem(result, StatusCodes.Status409Conflict, "Stock cannot exceed the maximum supported value.");
    }

    [Fact]
    public async Task AddToStock_ReturnsBadRequestProblemDetailsForInvalidQuantity()
    {
        var result = await InvokeControllerAsync(
            decrementException: null,
            incrementException: new ArgumentOutOfRangeException("quantity", "Quantity must be greater than zero."),
            decrement: false);

        AssertProblem(result, StatusCodes.Status400BadRequest, "Quantity must be greater than zero.");
    }

    private static async Task<ActionResult<ProductResponse>> InvokeControllerAsync(
        Exception? decrementException,
        Exception? incrementException,
        bool decrement)
    {
        using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddControllers()
            .Services
            .BuildServiceProvider();
        var controller = new ProductsController(
            new ThrowingProductService(decrementException, incrementException))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProvider
                }
            }
        };

        return decrement
            ? await controller.DecrementStock(100, 1)
            : await controller.AddToStock(100, 1);
    }

    private static void AssertProblem(
        ActionResult<ProductResponse> actionResult,
        int expectedStatus,
        string expectedDetail)
    {
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal(expectedDetail, problem.Detail);
    }

    private static async Task<string> ReadBodyAsync(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private sealed class ThrowingProductService(
        Exception? decrementException,
        Exception? incrementException) : IProductService
    {
        public Task<ProductResponse?> DecrementStockAsync(int id, int quantity) =>
            Raise(decrementException);

        public Task<ProductResponse?> IncrementStockAsync(int id, int quantity) =>
            Raise(incrementException);

        private static Task<ProductResponse?> Raise(Exception? exception) =>
            Task.FromException<ProductResponse?>(
                exception ?? new InvalidOperationException("No exception configured for this operation."));

        public Task<ProductResponse> CreateAsync(CreateProductRequest request) =>
            throw new NotSupportedException();

        public Task<Product?> DeleteByIdAsync(int id) =>
            throw new NotSupportedException();

        public Task<IEnumerable<ProductResponse>> GetAllAsync() =>
            throw new NotSupportedException();

        public Task<ProductResponse?> GetByIdAsync(int id) =>
            throw new NotSupportedException();

        public Task<IEnumerable<ProductResponse>> GetByNameAsync(string name) =>
            throw new NotSupportedException();

        public Task<IEnumerable<ProductResponse>> GetWithStockLevelAsync(int? minStock, int? maxStock) =>
            throw new NotSupportedException();

        public Task<ProductResponse?> UpdateByIdAsync(int id, UpdateProductRequest request) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public Exception? Exception { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Exception = exception;
        }
    }
}
