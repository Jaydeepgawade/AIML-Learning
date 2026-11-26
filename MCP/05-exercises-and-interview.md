# 05 — Exercises, Projects & Interview Questions

Work through these in order. Use the [`src/`](src/) project as your starting point.

---

## Level 1 — Beginner (understand)

1. Run the client (`MCP/src/McpLearning.Client`). For each of the 10 sections, write one line on which MCP method was used.
2. Open MCP Inspector, call `get_product` with `productId = 99`. What does the error look like? Is it a protocol error or a tool error?
3. Change the description of `check_stock` to something vague like `"stock"`. In Claude Desktop, ask "what's running low?". Does it still pick the tool? Restore it.

## Level 2 — Intermediate (build)

4. Add a tool `get_top_customers(int count)` that returns customers ranked by total spend (excluding cancelled orders).
5. Add a tool `cancel_order(int orderId)`:
   - Only `Pending` orders can be cancelled; otherwise return a clear `McpException`.
   - Restore product stock.
   - Mark it `Destructive = true`. Why?
6. Add a resource template `store://customers/{id}/orders`.
7. Add a prompt `monthly_sales_review(string month)` that tells the AI which tools to call.
8. Add `IStoreRepository` and make tools depend on the interface (Dependency Inversion).

## Level 3 — Advanced (integrate)

9. Implement `SqlStoreRepository` using Dapper and SQL Server. Create the tables, seed the same data, and switch via `appsettings.json` (`"DataSource": "Static" | "Sql"`).
10. Create `McpLearning.HttpServer` with `ModelContextProtocol.AspNetCore`, reusing the same tools (hint: move tools + data into a class library `McpLearning.Core`).
11. Add JWT authentication to the HTTP server. Add a claim `vertical` and make `get_orders` return only that vertical's data.
12. Add a long-running `generate_sales_report` tool with progress notifications and cancellation.
13. Connect the client to a real LLM through `IChatClient` (Ollama runs locally for free) and build a console chat agent.

## Level 4 — Project ideas

- **SQL Server MCP for your own DB**: read-only tools `list_tables`, `describe_table`, `run_select` (SELECT-only, TOP 100, timeout).
- **Log analyzer MCP**: tools to search application logs by date, level, correlation ID.
- **Robotics MCP**: an MCP server that controls a simulated robot (`move_forward`, `turn`, `read_sensor`) — links to the robotics learning track (later on a Raspberry Pi or ESP32).

---

## Interview questions (with short answers)

**Q1. What is MCP and why was it created?**
An open protocol that standardizes how AI applications connect to tools and data. It turns N×M custom integrations into N+M, like USB-C for AI.

**Q2. Host vs client vs server?**
Host = the user-facing AI app. Client = a connector inside the host, one per server. Server = exposes tools, resources and prompts.

**Q3. Tools vs resources vs prompts?**
Tools are model-controlled actions; resources are app-controlled, read-only data by URI; prompts are user-controlled templates.

**Q4. What protocol is MCP built on?**
JSON-RPC 2.0, over stdio or Streamable HTTP.

**Q5. Why can't you write to stdout in a stdio server?**
stdout carries the JSON-RPC messages; extra text corrupts the stream. Log to stderr.

**Q6. How does an LLM know what a tool does?**
From the name, description and JSON input schema returned by `tools/list`.

**Q7. What is sampling?**
The server asks the client's LLM to generate text, so the server needs no model access of its own and the user stays in control.

**Q8. How do you secure a remote MCP server?**
OAuth 2.1 with audience-validated tokens, per-user authorization, no token passthrough, least-privilege backend credentials, rate limits, audit logs, human approval for destructive tools.

**Q9. What is prompt injection in the context of MCP?**
Malicious instructions hidden in data returned by tools (or in tool descriptions) that try to make the AI misuse other tools.

**Q10. MCP vs REST API?**
REST is app-to-app; MCP is AI-to-tool with built-in discovery and two-way features. MCP servers often wrap REST APIs.

---

## System design question (practice)

> **Design an MCP platform that lets employees ask an AI assistant about orders, incentives and IPO data in your company's portal.**

Think about:
1. One MCP server per domain or one big server? (Hint: bounded contexts, like microservices.)
2. stdio or HTTP? Stateless or stateful?
3. How users authenticate, and how vertical/role-based access reaches each tool.
4. How to stop the AI from changing payouts without approval.
5. Logging, monitoring, and cost control (token usage).
6. How to roll out a new tool version without breaking existing prompts.

Write your answer as an HLD diagram plus a list of components, then compare it with [04 — section 10](04-mcp-advanced.md#10-production-architecture).
