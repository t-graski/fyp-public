namespace backend.services.interfaces;

public interface IPdfTextExtractor
{
    Task<List<(int PageNumber, string Text)>> ExtractPagesAsync(Stream pdfStream, CancellationToken ct);
}