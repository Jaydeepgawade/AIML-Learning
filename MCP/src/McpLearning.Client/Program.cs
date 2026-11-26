using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

// ---------------------------------------------------------------
// MCP Learning Client
//
// This plays the role of the HOST + CLIENT (what Claude Desktop does).
// It starts the server as a child process (stdio), then:
//   1. discovers tools, resources, prompts
//   2. calls tools, reads resources, gets prompts
// No AI model is involved here — WE decide which tool to call.
// In a real AI app, the LLM makes that decision.
//
// Run from this folder:  dotnet run
// ---------------------------------------------------------------

var serverProject = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "McpLearning.Server"));

Console.WriteLine($"Starting MCP server from: {serverProject}\n");

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "McpLearningServer",
    Command = "dotnet",
    Arguments = ["run", "--project", serverProject],
});

// CreateAsync performs the "initialize" handshake (version + capability negotiation).
await using var client = await McpClient.CreateAsync(transport);

Section("1. DISCOVERY — tools/list");
foreach (var tool in await client.ListToolsAsync())
    Console.WriteLine($"  🔧 {tool.Name,-16} {tool.Description}");

Section("2. DISCOVERY — resources/list + resources/templates/list");
foreach (var r in await client.ListResourcesAsync())
    Console.WriteLine($"  📄 {r.Uri,-28} {r.Name}");
foreach (var t in await client.ListResourceTemplatesAsync())
    Console.WriteLine($"  🧩 {t.UriTemplate,-28} {t.Name} (template)");

Section("3. DISCOVERY — prompts/list");
foreach (var p in await client.ListPromptsAsync())
    Console.WriteLine($"  💬 {p.Name,-16} {p.Description}");

Section("4. tools/call — search_products(keyword: 'book')");
await CallTool("search_products", new() { ["keyword"] = "book" });

Section("5. tools/call — get_customer(customerId: 1)");
await CallTool("get_customer", new() { ["customerId"] = 1 });

Section("6. tools/call — sales_summary()");
await CallTool("sales_summary", new());

Section("7. tools/call — place_order (write tool)");
await CallTool("place_order", new() { ["customerId"] = 2, ["productId"] = 10, ["quantity"] = 3 });

Section("8. tools/call — ERROR case: order out-of-stock product 7");
await CallTool("place_order", new() { ["customerId"] = 2, ["productId"] = 7, ["quantity"] = 1 });

Section("9. resources/read — store://info and store://products/9");
await ReadResource("store://info");
await ReadResource("store://products/9");

Section("10. prompts/get — customer_report(customerId: 3)");
var prompt = await client.GetPromptAsync("customer_report",
    new Dictionary<string, object?> { ["customerId"] = 3 });
foreach (var msg in prompt.Messages)
    if (msg.Content is TextContentBlock text)
        Console.WriteLine($"  [{msg.Role}] {text.Text}");

Console.WriteLine("\n✅ Done.");

// ---------------- helpers ----------------

async Task CallTool(string name, Dictionary<string, object?> arguments)
{
    var result = await client.CallToolAsync(name, arguments);
    var text = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
    Console.WriteLine(result.IsError == true ? $"  ❌ Tool error: {text}" : Indent(text));
}

async Task ReadResource(string uri)
{
    var result = await client.ReadResourceAsync(uri);
    foreach (var content in result.Contents.OfType<TextResourceContents>())
        Console.WriteLine($"  [{content.Uri}]\n{Indent(content.Text)}");
}

static void Section(string title) =>
    Console.WriteLine($"\n==================== {title} ====================");

static string Indent(string s) =>
    string.Join("\n", s.Split('\n').Select(l => "  " + l));
