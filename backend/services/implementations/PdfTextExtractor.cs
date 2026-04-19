using backend.services.interfaces;
using UglyToad.PdfPig;

namespace backend.services.implementations;

public class PdfTextExtractor : IPdfTextExtractor
{
    public async Task<List<(int PageNumber, string Text)>> ExtractPagesAsync(Stream pdfStream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms, ct);
        ms.Position = 0;

        var results = new List<(int PageNumber, string Text)>();

        using var pdf = PdfDocument.Open(ms);

        foreach (var page in pdf.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            results.Add((page.Number, page.Text));
        }

        return results;
    }
}