# MCP Learning Project (.NET 8)

A hands-on MCP server and client in C# using **static in-memory data** (a small online store).
No database needed — perfect for learning. Later, replace `StoreRepository` with SQL Server.

## Structure

```
src/
├── McpLearning.sln
├── McpLearning.Server/            ← MCP SERVER (stdio)
│   ├── Program.cs                 ← host + MCP registration
│   ├── Json.cs                    ← shared JSON options
│   ├── Data/
│   │   ├── Models.cs              ← Product, Customer, Order records
│   │   └── StoreRepository.cs     ← static data + queries (Singleton)
│   ├── Tools/                     ← actions the AI can call
│   │   ├── ProductTools.cs        list_products, get_product, search_products, check_stock
│   │   ├── CustomerTools.cs       list_customers, get_customer
│   │   └── OrderTools.cs          get_orders, get_order, sales_summary, place_order
│   ├── Resources/
│   │   └── StoreResources.cs      store://info, store://schema, store://catalog/categories, store://products/{id}
│   └── Prompts/
│       └── StorePrompts.cs        customer_report, low_stock_alert
└── McpLearning.Client/            ← MCP CLIENT console app (tests the server without an AI)
    └── Program.cs
```

## Prerequisites

- .NET 8 SDK or newer — `dotnet --version`
- Package: `ModelContextProtocol` 1.4.1 (restored automatically). Check NuGet for newer versions.

## 1. Build

```bash
cd MCP/src
dotnet build
```

## 2. Run the client (easiest way to see MCP working)

```bash
cd MCP/src/McpLearning.Client
dotnet run
```

The client starts the server, then shows discovery → tool calls → an error case → resource reads → a prompt.

## 3. Test with MCP Inspector (visual debugging UI)

Requires Node.js.

```bash
npx @modelcontextprotocol/inspector dotnet run --project MCP/src/McpLearning.Server
```

Open the URL it prints. You can list and call tools by hand and see the raw JSON-RPC messages.

## 4. Connect to Claude Desktop (real AI using your tools)

Edit `claude_desktop_config.json`:
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "store": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\AIML-Learning\\MCP\\src\\McpLearning.Server"]
    }
  }
}
```

Restart Claude Desktop, then try asking:
- "Which products are low in stock?"
- "Give me a sales summary by category."
- "Show me everything about customer 1 and their orders."
- "Place an order for 2 ESP32 boards for Priya." (watch it ask for approval)

## Common problems

| Problem | Cause / fix |
|--------|-------------|
| Client hangs or gets a JSON parse error | Something wrote to **stdout** in the server. Use `ILogger` (goes to stderr), never `Console.WriteLine`. |
| Tool not listed | Class missing `[McpServerToolType]` or method missing `[McpServerTool]`. |
| `dotnet` not found in Claude Desktop | Use the full path to dotnet in `"command"`. |
| Slow first start | `dotnet run` builds first. For speed, `dotnet publish` and point `command` at the `.exe`/dll. |

## Next steps (exercises)

See [../05-exercises-and-interview.md](../05-exercises-and-interview.md).
