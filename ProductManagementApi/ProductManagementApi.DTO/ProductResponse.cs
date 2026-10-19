namespace ProductManagementApi.DTO
{
    /// <summary>Represents a product returned by the API.</summary>
    public sealed record ProductResponse
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public int Stock { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}