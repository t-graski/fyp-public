using System.Text.Json;
using backend.models;

namespace backend.helpers;

public static class ModuleElementOptions
{
    public static string? GetHeadlineName(ModuleElement element) => element.Options.GetString("name");
    public static string? GetText(ModuleElement element) => element.Options.GetString("text");

    public static string? GetLinkTitle(ModuleElement element) =>
        element.Options.GetString("title") ?? element.Options.GetString("name");

    public static string? GetLinkUrl(ModuleElement element) => element.Options.GetString("url");
    public static string? GetFileName(ModuleElement element) => element.Options.GetString("fileName");

    private static string? GetString(this JsonDocument doc, string prop)
    {
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
        if (!doc.RootElement.TryGetProperty(prop, out var element)) return null;
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }
}