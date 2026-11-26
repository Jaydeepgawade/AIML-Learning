using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace McpLearning.Server.Prompts;

/// <summary>
/// PROMPTS = reusable prompt templates the USER picks (often shown as slash commands in the host).
/// The server packages "the right way to ask" so every user gets consistent results.
/// </summary>
[McpServerPromptType]
public class StorePrompts
{
    [McpServerPrompt(Name = "customer_report")]
    [Description("Creates a prompt asking the AI to write a report for one customer.")]
    public static ChatMessage CustomerReport(
        [Description("Customer ID to report on")] int customerId) =>
        new(ChatRole.User,
            $"""
            Use the store tools to build a short report for customer {customerId}:
            1. Call get_customer for profile and total spend.
            2. Call get_orders for that customer and list each order with status and total.
            3. End with one suggestion to increase their next purchase.
            Keep it under 200 words.
            """);

    [McpServerPrompt(Name = "low_stock_alert")]
    [Description("Creates a prompt asking the AI to prepare a reorder email for low-stock products.")]
    public static IEnumerable<ChatMessage> LowStockAlert(
        [Description("Stock threshold, e.g. 10")] int threshold) =>
    [
        new(ChatRole.User,
            $"Call check_stock with threshold {threshold}. Then draft a short email to the purchasing team listing items to reorder. Mark out-of-stock items as URGENT."),
        new(ChatRole.Assistant,
            "Understood. I'll check stock levels first, then draft the email.")
    ];
}
