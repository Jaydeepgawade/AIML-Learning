using System.ComponentModel;
using System.Text.Json;
using McpLearning.Server.Data;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace McpLearning.Server.Tools;

[McpServerToolType]
public static class OrderTools
{
    [McpServerTool(Name = "get_orders", ReadOnly = true)]
    [Description("Lists orders. Optionally filter by customer ID and/or status (Pending, Shipped, Delivered, Cancelled).")]
    public static string GetOrders(
        StoreRepository repo,
        [Description("Optional customer ID")] int? customerId = null,
        [Description("Optional status filter")] string? status = null)
    {
        var orders = repo.GetOrders(customerId, status)
            .Select(o => new { o.Id, o.CustomerId, o.OrderDate, o.Status, ItemCount = o.Items.Count, o.Total });
        return JsonSerializer.Serialize(orders, Json.Options);
    }

    [McpServerTool(Name = "get_order", ReadOnly = true)]
    [Description("Gets one order by ID with all line items and product names.")]
    public static string GetOrder(
        StoreRepository repo,
        [Description("Order ID, e.g. 1001")] int orderId)
    {
        var order = repo.GetOrder(orderId)
            ?? throw new McpException($"Order {orderId} not found.");

        var result = new
        {
            order.Id,
            Customer = repo.GetCustomer(order.CustomerId)?.Name,
            order.OrderDate,
            order.Status,
            Items = order.Items.Select(i => new
            {
                i.ProductId,
                Product = repo.GetProduct(i.ProductId)?.Name,
                i.Quantity,
                i.UnitPrice,
                i.LineTotal
            }),
            order.Total
        };
        return JsonSerializer.Serialize(result, Json.Options);
    }

    [McpServerTool(Name = "sales_summary", ReadOnly = true)]
    [Description("Returns a sales summary: total revenue (excluding cancelled orders), order count by status, and revenue by category.")]
    public static string SalesSummary(StoreRepository repo)
    {
        var allOrders = repo.GetOrders();
        var valid = allOrders.Where(o => o.Status != "Cancelled").ToList();

        var revenueByCategory = valid
            .SelectMany(o => o.Items)
            .GroupBy(i => repo.GetProduct(i.ProductId)?.Category ?? "Unknown")
            .Select(g => new { Category = g.Key, Revenue = g.Sum(i => i.LineTotal) })
            .OrderByDescending(x => x.Revenue);

        var result = new
        {
            TotalRevenue = valid.Sum(o => o.Total),
            OrdersByStatus = allOrders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count()),
            RevenueByCategory = revenueByCategory
        };
        return JsonSerializer.Serialize(result, Json.Options);
    }

    /// <summary>
    /// A WRITE tool. Note: ReadOnly = false and Destructive = false (it adds data, doesn't delete).
    /// These "annotations" are hints that MCP hosts use, e.g. to ask the user for confirmation.
    /// The ILogger parameter is also injected from DI. Logs go to stderr (see Program.cs).
    /// </summary>
    [McpServerTool(Name = "place_order", ReadOnly = false, Destructive = false, Idempotent = false)]
    [Description("Places a new order for one product. Reduces stock. Fails if customer/product doesn't exist or stock is insufficient.")]
    public static string PlaceOrder(
        StoreRepository repo,
        ILogger<StoreRepository> logger,
        [Description("Customer ID placing the order")] int customerId,
        [Description("Product ID to order")] int productId,
        [Description("Quantity, must be 1 or more")] int quantity)
    {
        try
        {
            var order = repo.PlaceOrder(customerId, productId, quantity);
            logger.LogInformation("Order {OrderId} placed for customer {CustomerId}", order.Id, customerId);
            return $"Order {order.Id} placed successfully. Total: ₹{order.Total:N2}. Status: {order.Status}.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            // Convert business errors to McpException so the AI sees a clear message.
            throw new McpException(ex.Message);
        }
    }
}
