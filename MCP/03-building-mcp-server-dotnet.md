# 03 — Building an MCP Server in .NET (Intermediate)

> Code: [`src/`](src/) · Run instructions: [`src/README.md`](src/README.md)

We build a **store MCP server** with static data: products, customers, orders.

---

## Step 0 — Create the project from scratch (to do it yourself)

```bash
dotnet new console -n McpLearning.Server
cd McpLearning.Server
dotnet add package ModelContextProtocol
dotnet add package Microsoft.Extensions.Hosting
```

| Package | Purpose |
|---------|---------|
| `ModelContextProtocol.Core` | Low-level client/server APIs, fewest dependencies |
| `ModelContextProtocol` | + Hosting and DI extensions (**use this for stdio servers**) |
| `ModelContextProtocol.AspNetCore` | + HTTP transport (`MapMcp`) for web servers |

---

## Step 1 — The data layer (Repository pattern)

`Data/StoreRepository.cs` holds static `List<T>` data and query methods.

```csharp
public class StoreRepository
{
    private readonly object _lock = new();
    private readonly List<Product> _products = [ new(1, "Dell Latitude 5440 Laptop", "Electronics", 72000m, 15), ... ];

    public Product? GetProduct(int id)
    {
        lock (_lock) return _products.FirstOrDefault(p => p.Id == id);
    }
}
```

**Why a repository instead of putting data in the tools?**
- Tools stay thin: they only translate between MCP and business logic.
- You can later replace the static lists with **SQL Server (Dapper/EF Core)** without touching any tool. This is the same layering as Clean Architecture: *tools = presentation layer*.

**Why `lock`?** The server can handle requests concurrently; `place_order` changes the lists.

---

## Step 2 — Program.cs (hosting)

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace); // logs → stderr

builder.Services.AddSingleton<StoreRepository>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResources<StoreResources>()
    .WithPrompts<StorePrompts>();

await builder.Build().RunAsync();
```

It's the same Generic Host you use in ASP.NET Core / Worker Services: DI, logging, configuration all work normally.

---

## Step 3 — Tools

```csharp
[McpServerToolType]
public static class ProductTools
{
    [McpServerTool(Name = "get_product", ReadOnly = true)]
    [Description("Gets full details of one product by its numeric ID.")]
    public static string GetProduct(
        StoreRepository repo,                                  // ← injected from DI (hidden from AI)
        [Description("Product ID, e.g. 1")] int productId)    // ← becomes the JSON schema input
    {
        var product = repo.GetProduct(productId)
            ?? throw new McpException($"Product {productId} not found.");
        return JsonSerializer.Serialize(product, Json.Options);
    }
}
```

What the SDK does for you:
1. Reflection finds `[McpServerToolType]` classes.
2. Builds a **JSON Schema** from parameters (`int productId` → `"type": "integer"`, required because no default).
3. Optional parameters (`string? category = null`) → not required.
4. Parameters registered in DI (`StoreRepository`, `ILogger<T>`, `HttpClient`, `CancellationToken`) are **injected** and excluded from the schema.
5. Return value → text content. `McpException` → `isError: true` with your message.

### Writing good tool descriptions (very important)

The LLM only sees the name, description and schema. Bad descriptions = wrong tool choices.

| ❌ Bad | ✅ Good |
|--------|---------|
| `"Gets data"` | `"Lists orders. Optionally filter by customer ID and/or status (Pending, Shipped, Delivered, Cancelled)."` |
| parameter `id` with no description | `[Description("Order ID, e.g. 1001")] int orderId` |

Tips: say **when** to use it, list **allowed values**, give **examples**, mention **units** (INR).

### Tool annotations (hints for the host)

```csharp
[McpServerTool(Name = "place_order", ReadOnly = false, Destructive = false, Idempotent = false)]
```

| Hint | Meaning |
|------|---------|
| `ReadOnly` | Doesn't change anything |
| `Destructive` | May delete or overwrite data |
| `Idempotent` | Calling twice = same effect as once |
| `OpenWorld` | Talks to external systems (internet) |

Hosts use these to decide e.g. "ask the user before running". They are **hints, not security** — never trust them for safety.

---

## Step 4 — Resources

```csharp
[McpServerResourceType]
public class StoreResources
{
    // Direct resource — fixed URI
    [McpServerResource(UriTemplate = "store://schema", Name = "Data Schema", MimeType = "text/markdown")]
    [Description("Describes the data model.")]
    public static string GetSchema() => "...";

    // Resource template — {id} parameter
    [McpServerResource(UriTemplate = "store://products/{id}", Name = "Product by ID", MimeType = "application/json")]
    public static TextResourceContents GetProduct(StoreRepository repo, int id) => new() { Uri = ..., Text = ... };
}
```

- Direct resources appear in `resources/list`.
- Templates appear in `resources/templates/list` (URI templates follow RFC 6570).
- Return `string`, `TextResourceContents`, or `BlobResourceContents` (binary such as images).

---

## Step 5 — Prompts

```csharp
[McpServerPromptType]
public class StorePrompts
{
    [McpServerPrompt(Name = "customer_report")]
    [Description("Creates a prompt asking the AI to write a report for one customer.")]
    public static ChatMessage CustomerReport([Description("Customer ID")] int customerId) =>
        new(ChatRole.User, $"Use the store tools to build a short report for customer {customerId}: ...");
}
```

`ChatMessage` comes from `Microsoft.Extensions.AI` (installed with the MCP package). Return one message or `IEnumerable<ChatMessage>` for a conversation.

---

## Step 6 — The client

```csharp
var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "McpLearningServer",
    Command = "dotnet",
    Arguments = ["run", "--project", serverProject],
});

await using var client = await McpClient.CreateAsync(transport);   // initialize handshake

var tools  = await client.ListToolsAsync();
var result = await client.CallToolAsync("get_customer", new() { ["customerId"] = 1 });
var res    = await client.ReadResourceAsync("store://info");
var prompt = await client.GetPromptAsync("customer_report", new Dictionary<string, object?> { ["customerId"] = 3 });
```

### Connecting the client to a real LLM

`McpClientTool` implements `AIFunction` from `Microsoft.Extensions.AI`, so you can pass MCP tools straight to any `IChatClient` (OpenAI, Azure OpenAI, Anthropic, Ollama):

```csharp
IList<McpClientTool> tools = await client.ListToolsAsync();
IChatClient chatClient = /* your provider's IChatClient, with .UseFunctionInvocation() */;

var response = await chatClient.GetResponseAsync(
    "Which products are low in stock?",
    new ChatOptions { Tools = [.. tools] });
```

Now **the LLM** decides which tools to call. That's a mini AI agent in .NET.

---

## Step 7 — Moving from static data to SQL Server (your next project)

1. Create interface `IStoreRepository` with the same methods.
2. `StaticStoreRepository : IStoreRepository` (current code).
3. `SqlStoreRepository : IStoreRepository` using Dapper + `Microsoft.Data.SqlClient`.
4. Register: `builder.Services.AddSingleton<IStoreRepository, SqlStoreRepository>();`
5. Change tool parameters from `StoreRepository` to `IStoreRepository`.

**Security for SQL:**
- Use a dedicated **read-only SQL login** (`db_datareader` only) for read tools.
- Prefer specific tools (`get_order`) over a generic `run_sql` tool.
- If you do add `run_sql`: allow only `SELECT`, parameterize, set `CommandTimeout`, limit row count (`TOP 100`).
- Connection string in **user-secrets / environment variables**, never in code.

---

## Revision questions

1. How does the SDK know `StoreRepository` should be injected instead of sent by the AI?
2. What happens if you write `Console.WriteLine("debug")` inside a tool on a stdio server?
3. Why return a `McpException` instead of letting a `NullReferenceException` happen?
4. Which design pattern lets you swap static data for SQL Server without changing tools?

**Next:** [04 — Advanced MCP](04-mcp-advanced.md)
