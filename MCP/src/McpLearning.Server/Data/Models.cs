namespace McpLearning.Server.Data;

// Simple domain models for a small online store.
// "record" types are immutable and serialize nicely to JSON.

public record Product(int Id, string Name, string Category, decimal Price, int Stock);

public record Customer(int Id, string Name, string City, string Email);

public record OrderItem(int ProductId, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

public record Order(int Id, int CustomerId, DateOnly OrderDate, string Status, List<OrderItem> Items)
{
    public decimal Total => Items.Sum(i => i.LineTotal);
}
