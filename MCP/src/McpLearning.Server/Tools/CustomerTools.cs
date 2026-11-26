using System.ComponentModel;
using System.Text.Json;
using McpLearning.Server.Data;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace McpLearning.Server.Tools;

[McpServerToolType]
public static class CustomerTools
{
    [McpServerTool(Name = "list_customers", ReadOnly = true)]
    [Description("Lists customers. Optionally filter by city (e.g. Mumbai, Pune).")]
    public static string ListCustomers(
        StoreRepository repo,
        [Description("Optional city to filter by")] string? city = null)
    {
        return JsonSerializer.Serialize(repo.GetCustomers(city), Json.Options);
    }

    [McpServerTool(Name = "get_customer", ReadOnly = true)]
    [Description("Gets one customer by ID, including their order count and total spend (excluding cancelled orders).")]
    public static string GetCustomer(
        StoreRepository repo,
        [Description("Customer ID, e.g. 1")] int customerId)
    {
        var customer = repo.GetCustomer(customerId)
            ?? throw new McpException($"Customer {customerId} not found.");

        var orders = repo.GetOrders(customerId)
            .Where(o => o.Status != "Cancelled")
            .ToList();

        var result = new
        {
            customer.Id,
            customer.Name,
            customer.City,
            customer.Email,
            OrderCount = orders.Count,
            TotalSpend = orders.Sum(o => o.Total)
        };
        return JsonSerializer.Serialize(result, Json.Options);
    }
}
