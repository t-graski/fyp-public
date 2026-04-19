using System.Text.RegularExpressions;

namespace backend.helpers;

public static partial class RagAnswerParsing
{
    private static readonly Regex ElementIdRegex =
        InternalElementIdRegex();

    public static Guid? TryExtractElementId(string answer)
    {
        var m = ElementIdRegex.Match(answer);

        if (!m.Success)
        {
            return null;
        }

        return Guid.TryParse(m.Groups[1].Value, out var id) ? id : null;
    }

    [GeneratedRegex(
        @"\(Element:\s*([0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12})\s*\)",
        RegexOptions.Compiled)]
    private static partial Regex InternalElementIdRegex();
}