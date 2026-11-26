namespace McpLearning.Server.Data;

/// <summary>
/// In-memory "database" with static seed data — for learning only.
/// Registered as a Singleton in DI, so every tool call sees the same data.
/// Later you can swap this for a SQL Server implementation (Dapper / EF Core)
/// without changing the MCP tools — that is the Repository pattern.
/// </summary>
public class StoreRepository
{
    private readonly object _lock = new();

    private readonly List<Product> _products =
    [
        new(1, "Dell Latitude 5440 Laptop", "Electronics", 72000m, 15),
        new(2, "Logitech MX Master 3S Mouse", "Electronics", 8500m, 40),
        new(3, "Samsung 27\" Monitor", "Electronics", 18500m, 10),
        new(4, "Clean Architecture (Book)", "Books", 650m, 100),
        new(5, "Designing Data-Intensive Applications (Book)", "Books", 1100m, 60),
        new(6, "Office Chair Ergonomic", "Furniture", 12500m, 8),
        new(7, "Standing Desk", "Furniture", 28000m, 0),
        new(8, "Arduino Uno R3 Kit", "Robotics", 2400m, 25),
        new(9, "Raspberry Pi 5 (8GB)", "Robotics", 8900m, 12),
        new(10, "ESP32 Dev Board", "Robotics", 550m, 75),
    ];

    private readonly List<Customer> _customers =
    [
        new(1, "Amit Sharma", "Mumbai", "amit@example.com"),
        new(2, "Priya Patil", "Pune", "priya@example.com"),
        new(3, "Rahul Verma", "Delhi", "rahul@example.com"),
        new(4, "Sneha Iyer", "Bengaluru", "sneha@example.com"),
        new(5, "Karan Mehta", "Mumbai", "karan@example.com"),
    ];

    private readonly List<Order> _orders =
    [
        new(1001, 1, new DateOnly(2026, 11, 1), "Delivered", [new(1, 1, 72000m), new(2, 1, 8500m)]),
        new(1002, 2, new DateOnly(2026, 11, 3), "Delivered", [new(4, 2, 650m), new(5, 1, 1100m)]),
        new(1003, 3, new DateOnly(2026, 11, 10), "Shipped",  [new(8, 2, 2400m), new(10, 4, 550m)]),
        new(1004, 1, new DateOnly(2026, 11, 15), "Pending",  [new(9, 1, 8900m)]),
        new(1005, 4, new DateOnly(2026, 11, 20), "Cancelled",[new(6, 1, 12500m)]),
        new(1006, 5, new DateOnly(2026, 11, 22), "Pending",  [new(3, 2, 18500m), new(2, 2, 8500m)]),
    ];

    // ---------- Products ----------
    public IReadOnlyList<Product> GetProducts(string? category = null)
    {
        lock (_lock)
        {
            return _products
                .Where(p => category is null || p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public Product? GetProduct(int id)
    {
        lock (_lock) return _products.FirstOrDefault(p => p.Id == id);
    }

    public IReadOnlyList<Product> SearchProducts(string keyword, decimal? maxPrice)
    {
        lock (_lock)
        {
            return _products
                .Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Where(p => maxPrice is null || p.Price <= maxPrice)
                .ToList();
        }
    }

    public IReadOnlyList<string> GetCategories()
    {
        lock (_lock) return _products.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
    }

    // ---------- Customers ----------
    public IReadOnlyList<Customer> GetCustomers(string? city = null)
    {
        lock (_lock)
        {
            return _customers
                .Where(c => city is null || c.City.Equals(city, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public Customer? GetCustomer(int id)
    {
        lock (_lock) return _customers.FirstOrDefault(c => c.Id == id);
    }

    // ---------- Orders ----------
    public IReadOnlyList<Order> GetOrders(int? customerId = null, string? status = null)
    {
        lock (_lock)
        {
            return _orders
                .Where(o => customerId is null || o.CustomerId == customerId)
                .Where(o => status is null || o.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public Order? GetOrder(int id)
    {
        lock (_lock) return _orders.FirstOrDefault(o => o.Id == id);
    }

    /// <summary>
    /// A "write" operation — changes in-memory state.
    /// Validates input and reduces stock. Returns the new order.
    /// </summary>
    public Order PlaceOrder(int customerId, int productId, int quantity)
    {
        lock (_lock)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            var customer = _customers.FirstOrDefault(c => c.Id == customerId)
                ?? throw new ArgumentException($"Customer {customerId} not found.");

            var product = _products.FirstOrDefault(p => p.Id == productId)
                ?? throw new ArgumentException($"Product {productId} not found.");

            if (product.Stock < quantity)
                throw new InvalidOperationException(
                    $"Only {product.Stock} unit(s) of '{product.Name}' in stock.");

            // Reduce stock (records are immutable, so replace the item)
            var index = _products.IndexOf(product);
            _products[index] = product with { Stock = product.Stock - quantity };

            var order = new Order(
                Id: _orders.Max(o => o.Id) + 1,
                CustomerId: customer.Id,
                OrderDate: DateOnly.FromDateTime(DateTime.Today),
                Status: "Pending",
                Items: [new OrderItem(product.Id, quantity, product.Price)]);

            _orders.Add(order);
            return order;
        }
    }
}
