using System.Text.Json;

namespace backend.helpers;

public static class JsonHelpers
{
    public static JsonDocument ToDocument(JsonElement element)
        => JsonDocument.Parse(element.GetRawText());
}