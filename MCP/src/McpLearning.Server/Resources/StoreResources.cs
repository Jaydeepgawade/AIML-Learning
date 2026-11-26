using System.ComponentModel;
using System.Text.Json;
using McpLearning.Server.Data;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpLearning.Server.Resources;

/// <summary>
/// RESOURCES = read-only data identified by a URI (like a GET endpoint or a file).
/// Difference from tools: the HOST app / user usually chooses which resources to attach
/// as context; tools are chosen by the AI model.
///
/// Two kinds:
///  1. Direct resource   -> fixed URI, e.g. store://catalog/categories
///  2. Resource template -> URI with parameters, e.g. store://products/{id}
/// </summary>
[McpServerResourceType]
public class StoreResources
{
    [McpServerResource(UriTemplate = "store://info", Name = "Store Info", MimeType = "text/markdown")]
    [Description("General information about the demo store and its business rules.")]
    public static string GetStoreInfo() =>
        """
        # Demo Store (MCP Learning)

        - Currency: INR (₹)
        - Order statuses: Pending → Shipped → Delivered, or Cancelled
        - Cancelled orders are excluded from revenue
        - Data is static and in-memory; it resets every time the server restarts
        """;

    [McpServerResource(UriTemplate = "store://catalog/categories", Name = "Product Categories", MimeType = "application/json")]
    [Description("List of all product categories.")]
    public static string GetCategories(StoreRepository repo) =>
        JsonSerializer.Serialize(repo.GetCategories(), Json.Options);

    [McpServerResource(UriTemplate = "store://schema", Name = "Data Schema", MimeType = "text/markdown")]
    [Description("Describes the data model (tables and columns), like a database schema.")]
    public static string GetSchema() =>
        """
        # Schema

        Product  (Id, Name, Category, Price, Stock)
        Customer (Id, Name, City, Email)
        Order    (Id, CustomerId -> Customer.Id, OrderDate, Status)
        OrderItem(ProductId -> Product.Id, Quantity, UnitPrice)
        """;

    // Resource TEMPLATE: {id} is filled in by the client.
    [McpServerResource(UriTemplate = "store://products/{id}", Name = "Product by ID", MimeType = "application/json")]
    [Description("Returns one product as JSON by its ID.")]
    public static TextResourceContents GetProduct(StoreRepository repo, int id)
    {
        var product = repo.GetProduct(id)
            ?? throw new McpException($"Product {id} not found.");

        return new TextResourceContents
        {
            Uri = $"store://products/{id}",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(product, Json.Options)
        };
    }
}
