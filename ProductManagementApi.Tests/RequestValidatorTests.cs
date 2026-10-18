using ProductManagementApi.DTO;
using ProductManagementApi.Validators;

namespace ProductManagementApi.Tests;

public class RequestValidatorTests
{
    [Fact]
    public void CreateProductRequestValidator_RejectsInvalidFields()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest
        {
            Name = "",
            Description = "",
            Price = 0,
            Stock = -1
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Name));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Description));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Price));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Stock));
    }

    [Fact]
    public void UpdateProductRequestValidator_RejectsInvalidFields()
    {
        var validator = new UpdateProductRequestValidator();
        var request = new UpdateProductRequest
        {
            Name = "",
            Description = "",
            Price = -1,
            Stock = -1
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Name));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Description));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Price));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Stock));
    }

    [Fact]
    public void SearchProductsRequestValidator_RejectsEmptyOrTooLongName()
    {
        var validator = new SearchProductsRequestValidator();

        Assert.False(validator.Validate(new SearchProductsRequest { Name = "" }).IsValid);
        Assert.False(validator.Validate(new SearchProductsRequest { Name = new string('a', 257) }).IsValid);
        Assert.True(validator.Validate(new SearchProductsRequest { Name = "camera" }).IsValid);
    }

    [Fact]
    public void StockLevelRequestValidator_RequiresNonNegativeOrderedBounds()
    {
        var validator = new StockLevelRequestValidator();

        Assert.False(validator.Validate(new StockLevelRequest()).IsValid);
        Assert.False(validator.Validate(new StockLevelRequest { Min = -1, Max = 10 }).IsValid);
        Assert.False(validator.Validate(new StockLevelRequest { Min = 11, Max = 10 }).IsValid);
        Assert.True(validator.Validate(new StockLevelRequest { Min = 5, Max = 10 }).IsValid);
    }
}
