# 02 — MCP Core Concepts (Beginner → Intermediate)

> Prerequisite: [01 — What is MCP?](01-what-is-mcp.md)

---

## 1. Key terms

| Term | Meaning |
|------|---------|
| **LLM** | Large Language Model — the AI (e.g. Claude). It can only read and write text; it cannot touch your systems by itself. |
| **Tool calling / function calling** | The LLM's ability to say "please call function X with these arguments". Your code does the actual call. |
| **Protocol** | A set of rules for how two programs talk (like HTTP for browsers). |
| **JSON-RPC 2.0** | A simple protocol: send a JSON "request" with a method name + params, receive a JSON "response". MCP is built on it. |
| **Transport** | *How* the messages travel: stdio (pipes) or HTTP. |
| **Capability** | A feature a side supports (tools, resources, prompts, sampling…). Declared during the handshake. |

---

## 2. MCP vs normal function calling

Without MCP, in your own .NET app you would:

1. Describe your functions to the LLM API (JSON schema).
2. Receive a "call function X" response.
3. Run X yourself, send the result back.

This works, but the integration lives **inside one app**.

MCP moves the functions into a **separate server** with a **standard interface**:

```
Plain function calling:   [Your App + your functions] ──► LLM API

MCP:                      [Any Host] ──► LLM API
                               │
                         MCP protocol
                               │
                      [MCP Server: your functions]   ← reusable by any host
```

**Analogy:** Plain function calling = wiring a lamp straight into the wall.
MCP = putting a standard socket in the wall; any lamp plugs in.

---

## 3. The three roles (again, deeper)

```
┌──────────────────────── HOST (e.g. Claude Desktop) ───────────────────────┐
│   LLM  ◄──►  Host logic  ──►  MCP Client A  ──► ── stdio ──► Server: Store │
│                          ──►  MCP Client B  ──► ── HTTP  ──► Server: GitHub│
└───────────────────────────────────────────────────────────────────────────┘
```

- One host can connect to **many servers**.
- Each connection gets its **own client** (1 client : 1 server).
- Servers are **isolated** — the Store server cannot see the GitHub server.

---

## 4. Server capabilities ("primitives")

| Primitive | Who controls it | Example | Web API analogy |
|-----------|-----------------|---------|-----------------|
| **Tools** | The **model** decides to call | `place_order`, `run_query` | POST endpoint |
| **Resources** | The **app/user** attaches as context | `store://schema` | GET endpoint / file |
| **Prompts** | The **user** picks (slash command) | `/customer_report` | Saved template |

**Rule of thumb:**
- Does it *do* something or need the AI to decide? → **Tool**
- Is it *data to read* that the user/app selects? → **Resource**
- Is it *a good way to ask* that users repeat? → **Prompt**

## 5. Client capabilities (server → client direction)

MCP is two-way. A server can also ask the client for things:

| Capability | What it lets the server do |
|-----------|----------------------------|
| **Sampling** | Ask the host's LLM to generate text ("summarize this for me") — the server needs no API key of its own |
| **Elicitation** | Ask the **user** a question mid-task ("Which warehouse?") |
| **Roots** | Learn which folders/URIs the server is allowed to work in |

These are covered in [04 — Advanced](04-mcp-advanced.md).

---

## 6. Lifecycle — what happens on connect

```
Client                                   Server
  │ ── initialize (version, capabilities, clientInfo) ──►│
  │ ◄── result (version, capabilities, serverInfo) ──────│
  │ ── notifications/initialized ───────────────────────►│
  │                                                      │
  │ ── tools/list ──────────────────────────────────────►│   normal operation
  │ ◄── [list of tools + JSON schemas] ──────────────────│
  │ ── tools/call {name, arguments} ────────────────────►│
  │ ◄── {content: [...], isError: false} ────────────────│
  │                                                      │
  │ ── (close stdin / end HTTP session) ────────────────►│   shutdown
```

1. **Initialize** — both sides agree on the protocol version and say what they support.
2. **Operation** — list and call things.
3. **Shutdown** — close the connection.

---

## 7. Raw messages (what actually goes over the wire)

**Request: list tools**
```json
{ "jsonrpc": "2.0", "id": 2, "method": "tools/list" }
```

**Response** (generated from your C# `[Description]` attributes and parameters!)
```json
{
  "jsonrpc": "2.0", "id": 2,
  "result": {
    "tools": [{
      "name": "get_product",
      "description": "Gets full details of one product by its numeric ID.",
      "inputSchema": {
        "type": "object",
        "properties": { "productId": { "type": "integer", "description": "Product ID, e.g. 1" } },
        "required": ["productId"]
      },
      "annotations": { "readOnlyHint": true }
    }]
  }
}
```

**Request: call a tool**
```json
{ "jsonrpc": "2.0", "id": 3, "method": "tools/call",
  "params": { "name": "get_product", "arguments": { "productId": 9 } } }
```

**Response**
```json
{ "jsonrpc": "2.0", "id": 3,
  "result": { "content": [{ "type": "text", "text": "{ \"id\": 9, \"name\": \"Raspberry Pi 5 (8GB)\", ... }" }],
              "isError": false } }
```

**Notification** (no `id` → no response expected)
```json
{ "jsonrpc": "2.0", "method": "notifications/tools/list_changed" }
```

### Method cheat-sheet

| Method | Direction | Purpose |
|--------|-----------|---------|
| `initialize` | C → S | Handshake |
| `ping` | both | Health check |
| `tools/list`, `tools/call` | C → S | Tools |
| `resources/list`, `resources/read`, `resources/templates/list` | C → S | Resources |
| `resources/subscribe` | C → S | Get notified when a resource changes |
| `prompts/list`, `prompts/get` | C → S | Prompts |
| `sampling/createMessage` | S → C | Ask host LLM |
| `elicitation/create` | S → C | Ask user |
| `roots/list` | S → C | Allowed folders |
| `notifications/progress` | both | Progress updates for long tasks |
| `notifications/cancelled` | both | Cancel a request |

---

## 8. Two kinds of errors

| Type | When | How it looks |
|------|------|--------------|
| **Protocol error** | Unknown method, invalid JSON, bad params | JSON-RPC `error` object with a code (e.g. `-32602`) |
| **Tool execution error** | Tool ran but failed (e.g. "out of stock") | Normal result with `isError: true` and a message |

Why it matters: a **tool error is shown to the LLM**, so it can fix its mistake ("product 99 not found → let me search first"). In C# you throw `McpException` to produce one.

---

## 9. Transports

| | **stdio** | **Streamable HTTP** |
|--|-----------|---------------------|
| How | Host launches server as a child process; JSON over stdin/stdout | Server is a web endpoint (POST for requests, optional streaming via SSE) |
| Where | Local machine | Local or remote / cloud |
| Users | One | Many |
| Auth | Not needed (runs as you) | Needed (OAuth 2.1) |
| .NET | `WithStdioServerTransport()` | `ModelContextProtocol.AspNetCore` + `WithHttpTransport()` + `app.MapMcp()` |
| Use for | Dev tools, local files, your own DB | Shared company services, SaaS integrations |

> Golden rule for stdio: **stdout belongs to the protocol**. Logs → stderr.

---

## 10. Where MCP fits in an AI agent

```
User: "Which customers in Mumbai spent the most?"
  │
  ▼
LLM thinks → needs data → picks tool list_customers(city: "Mumbai")
  │
  ▼
Host → MCP client → server → result
  │
  ▼
LLM thinks → needs spend → calls get_customer(1), get_customer(5)
  │
  ▼
LLM writes final answer
```

This loop (think → call tool → observe → repeat) is the **agent loop**. MCP provides the "call tool" part in a standard way.

---

## Revision questions

1. Name the three server primitives and who controls each.
2. What is the difference between a JSON-RPC request and a notification?
3. Why does `isError: true` exist instead of just returning a JSON-RPC error?
4. When would you choose stdio over HTTP?

**Next:** [03 — Building an MCP server in .NET](03-building-mcp-server-dotnet.md)
