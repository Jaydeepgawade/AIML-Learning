using System.Text.Json;

namespace McpLearning.Server;

/// <summary>Shared JSON settings so every tool returns the same, readable format.</summary>
internal static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
