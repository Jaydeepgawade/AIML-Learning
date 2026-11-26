# 01 — What is MCP (Model Context Protocol)?

**MCP (Model Context Protocol)** is an open standard, created by Anthropic, for connecting AI models (like Claude) to external tools and data — databases, files, GitHub, Slack, and more. It defines one common way for an AI app to discover what a tool can do and to call it.

---

## Why it exists

Before MCP, every AI app needed a custom integration for every tool.
10 AI apps × 20 tools = up to **200 separate integrations**.

**Analogy:** MCP is like **USB-C**. Build a tool's server once, and any MCP-compatible AI app can use it.

```
Without MCP:  N apps × M tools = N×M integrations
With MCP:     N apps + M tools = N+M implementations
```

---

## Architecture

```
[ Host app ]        e.g. Claude Desktop, VS Code, your .NET app
     │
[ MCP Client ]      ── JSON-RPC (stdio or HTTP) ──
     │
[ MCP Server ]      e.g. a SQL Server MCP server
     │
[ Real resource ]   e.g. your SQL Server database
```

| Part | Role |
|------|------|
| **Host** | The app the user works in |
| **Client** | Lives inside the host; keeps a 1:1 connection with one server |
| **Server** | Small program that exposes capabilities |

**Transports:**
- **stdio** — server runs as a local process (good for local tools)
- **HTTP** — server runs remotely (good for shared / cloud tools)

---

## What a server exposes

1. **Tools** — actions the AI can call (`run_query`, `list_tables`). Model-controlled.
2. **Resources** — read-only data (a table schema, a file). Application-controlled.
3. **Prompts** — reusable prompt templates. User-controlled.

---

## How a request flows

User asks: *"How many orders came in yesterday?"*

1. The AI sees a `run_query` tool exists, along with its input schema.
2. It writes the SQL and asks the client to call the tool.
3. The server runs the query against SQL Server and returns rows.
4. The AI answers using real data.

---

## .NET example

There's an official C# SDK — NuGet package `ModelContextProtocol`.

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class SqlTools
{
    [McpServerTool, Description("Count rows in a table")]
    public static async Task<int> CountRows(string table)
    {
        // ADO.NET / Dapper query here (validate 'table' against a whitelist!)
        throw new NotImplementedException();
    }
}
```

The attributes describe each tool and its inputs, so the AI knows when and how to call it.

---

## Advantages vs disadvantages

| ✅ Advantages | ❌ Disadvantages |
|--------------|-----------------|
| Build once, reuse everywhere | New security surface — AI can take real actions |
| Works across many AI apps | Extra servers to maintain |
| Clean separation of concerns | Standard is still evolving |

---

## When to use / when not to

**Use it when:**
- You want an AI assistant to read or act on your own systems (DB, APIs, files)
- The same integration should work in several AI apps

**Avoid / be careful when:**
- A plain API call in your own code is enough (no AI in the loop)
- The tool can do destructive actions without guardrails

---

## Security tip (SQL Server)

Start with a **read-only** DB login and tools that only run `SELECT`.
Never give the AI a login that can run `DELETE` or `DROP`.

---

## Practice questions

1. How is MCP different from calling a REST API directly from your code?
2. Why should a SQL MCP server expose `get_schema` as a **resource** but `run_query` as a **tool**?
3. **Design question:** the AI asks your server to run `DELETE FROM Orders`. Which layers should stop it?

---

**Next:** Build a SQL Server MCP server in .NET, step by step.
