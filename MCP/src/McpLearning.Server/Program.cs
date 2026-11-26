using McpLearning.Server.Data;
using McpLearning.Server.Prompts;
using McpLearning.Server.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ---------------------------------------------------------------
// MCP Learning Server (stdio transport)
//
// stdio = the host app (Claude Desktop, VS Code, our client) starts this
// program as a child process and talks to it via stdin/stdout using JSON-RPC.
//
// ⚠️ IMPORTANT: stdout is reserved for MCP messages.
// Never use Console.WriteLine() here — it corrupts the protocol.
// All logs must go to stderr (configured below).
// ---------------------------------------------------------------

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    // Send ALL log levels to stderr, keeping stdout clean for MCP.
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Our "database" — one instance shared by all tool calls.
builder.Services.AddSingleton<StoreRepository>();

builder.Services
    .AddMcpServer()                    // 1. register the MCP server
    .WithStdioServerTransport()        // 2. communicate over stdin/stdout
    .WithToolsFromAssembly()           // 3. find all [McpServerToolType] classes
    .WithResources<StoreResources>()   // 4. register resources
    .WithPrompts<StorePrompts>();      // 5. register prompts

await builder.Build().RunAsync();
