using System.ComponentModel;
using System.Text.Json;
using McpLearning.Server.Data;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace McpLearning.Server.Tools;

/// <summary>
/// TOOLS = actions the AI model can decide to call.
/// [McpServerToolType] marks the class; [McpServerTool] marks each tool.
/// [Description] text is sent to the AI — it is how the model knows WHEN and HOW to use the tool.
/// Write descriptions like documentation for a new teammate.
/// </summary>
[McpServerToolType]
public static class ProductTools
{
    // StoreRepository is NOT sent by the AI. The SDK sees it is registered in DI
    // and injects it automatically. Only "normal" parameters appear in the tool's input schema.

    [McpServerTool(Name = "list_products", ReadOnly = true)]
    [Description("Lists products in the store. Optionally filter by category (e.g. Electronics, Books, Furniture, Robotics).")]
    public static string ListProducts(
        StoreRepository repo,
        [Description("Optional category name to filter by")] string? category = null)
    {
        var products = repo.GetProducts(category);
        return JsonSerializer.Serialize(products, Json.Options);
    }

    [McpServerTool(Name = "get_product", ReadOnly = true)]
    [Description("Gets full details of one product by its numeric ID.")]
    public static string GetProduct(
        StoreRepository repo,
        [Description("Product ID, e.g. 1")] int productId)
    {
        var product = repo.GetProduct(productId)
            // McpException message is returned to the AI as a tool error it can read and react to.
            ?? throw new McpException($"Product {productId} not found.");
        return JsonSerializer.Serialize(product, Json.Options);
    }

    [McpServerTool(Name = "search_products", ReadOnly = true)]
    [Description("Searches products by a keyword in the name, with an optional maximum price in INR.")]
    public static string SearchProducts(
        StoreRepository repo,
        [Description("Keyword to search in product name, e.g. 'laptop'")] string keyword,
        [Description("Optional maximum price in INR")] decimal? maxPrice = null)
    {
        var results = repo.SearchProducts(keyword, maxPrice);
        return results.Count == 0
            ? $"No products found for '{keyword}'."
            : JsonSerializer.Serialize(results, Json.Options);
    }

    [McpServerTool(Name = "check_stock", ReadOnly = true)]
    [Description("Returns products whose stock is at or below a threshold. Useful for low-stock / reorder reports.")]
    public static string CheckStock(
        StoreRepository repo,
        [Description("Stock threshold, default 10")] int threshold = 10)
    {
        var low = repo.GetProducts().Where(p => p.Stock <= threshold)
            .Select(p => new { p.Id, p.Name, p.Stock, OutOfStock = p.Stock == 0 });
        return JsonSerializer.Serialize(low, Json.Options);
    }
}
