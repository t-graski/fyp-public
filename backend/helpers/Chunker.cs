namespace backend.helpers;

public static class Chunker
{
    public static IEnumerable<string> ChunkByChar(string text, int maxChars = 3500)
    {
        text = text.Replace("\r", "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        for (var i = 0; i < text.Length; i += maxChars)
        {
            var len = Math.Min(maxChars, text.Length - i);
            var chunk = text.Substring(i, len).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                yield return chunk;
            }
        }
    }
}