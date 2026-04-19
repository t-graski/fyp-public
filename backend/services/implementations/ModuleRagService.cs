using backend.data;
using backend.dtos;
using backend.errors;
using backend.helpers;
using backend.helpers.interfaces;
using backend.models;
using backend.models.enums;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class ModuleRagService(
    AppDbContext db,
    IOpenAiService ai,
    IPdfTextExtractor pdfTextExtractor,
    IFileStorage storage,
    IDatabaseGuards databaseGuards) : IModuleRagService
{
    public async Task IndexAsync(Guid moduleId, bool rebuild, CancellationToken ct)
    {
        await databaseGuards.EnsureTeachingStaffAsync(moduleId);
        
        if (rebuild)
        {
            var existing = db.ModuleContentChunks.Where(x => x.ModuleId == moduleId && !x.IsDeleted);
            db.ModuleContentChunks.RemoveRange(existing);
            await db.SaveChangesAsync(ct);
        }

        var elements = await db.ModuleElements
            .AsNoTracking()
            .Where(x => x.ModuleId == moduleId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

        foreach (var e in elements)
        {
            string? title;
            string? content;

            switch (e.Type)
            {
                case ModuleElementType.Headline:
                    title = ModuleElementOptions.GetHeadlineName(e) ?? "Headline";
                    content = title;
                    break;
                case ModuleElementType.Text:
                    title = "Text";
                    content = ModuleElementOptions.GetText(e);
                    break;
                case ModuleElementType.Link:
                    var linkTitle = ModuleElementOptions.GetLinkTitle(e) ?? "Link";
                    var url = ModuleElementOptions.GetLinkUrl(e);
                    title = linkTitle;
                    content = string.IsNullOrWhiteSpace(url) ? linkTitle : $"{linkTitle}\nURL: {url}";
                    break;
                default:
                    continue;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            foreach (var chunk in Chunker.ChunkByChar(content))
            {
                var emb = await ai.EmbedAsync(chunk, ct);

                db.ModuleContentChunks.Add(new ModuleContentChunk
                {
                    Id = Guid.NewGuid(),
                    ModuleId = moduleId,
                    ModuleElementId = e.Id,
                    SourceType = "module_element",
                    Title = title,
                    Content = chunk,
                    Embedding = emb
                });
            }
        }

        var fileUploadElements = elements
            .Where(x => x.Type == ModuleElementType.FileUpload)
            .Select(x => new { Element = x, FileName = ModuleElementOptions.GetFileName(x) })
            .Where(x => !string.IsNullOrWhiteSpace(x.FileName))
            .ToList();

        if (fileUploadElements.Count > 0)
        {
            var moduleFiles = await db.ModuleFiles
                .AsNoTracking()
                .Where(x => x.ModuleId == moduleId && !x.IsDeleted)
                .ToListAsync(ct);

            var byName = moduleFiles
                .GroupBy(f => f.OriginalFileName)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var fu in fileUploadElements)
            {
                if (!byName.TryGetValue(fu.FileName!, out var mf))
                {
                    continue;
                }

                var isPdf = mf.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                            || mf.OriginalFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

                if (!isPdf)
                {
                    continue;
                }

                var (pdfStream, _) = await storage.OpenReadAsync(mf.StorageKey, ct);
                await using (pdfStream)
                {
                    var pages = await pdfTextExtractor.ExtractPagesAsync(pdfStream, ct);
                    foreach (var (pageNumber, pageText) in pages)
                    {
                        if (string.IsNullOrWhiteSpace(pageText))
                        {
                            continue;
                        }

                        foreach (var chunk in Chunker.ChunkByChar(pageText))
                        {
                            var emb = await ai.EmbedAsync(chunk, ct);

                            var moduleContentChunk = new ModuleContentChunk
                            {
                                Id = Guid.NewGuid(),
                                ModuleId = moduleId,
                                ModuleElementId = fu.Element.Id,
                                SourceType = "pdf",
                                Title = mf.OriginalFileName,
                                PageNumber = pageNumber,
                                Content = chunk,
                                Embedding = emb
                            };

                            db.ModuleContentChunks.Add(moduleContentChunk);
                        }
                    }
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<RagChatResponse> ChatAsync(Guid moduleId, string message, int topK, CancellationToken ct)
    {
        var hasAny = await db.ModuleContentChunks.AnyAsync(x => x.ModuleId == moduleId && !x.IsDeleted, ct);

        if (!hasAny)
        {
            throw new AppException(409, "MODULE_NOT_INDEXED", "Module has not been indexed yet.");
        }

        var qEmb = await ai.EmbedAsync(message, ct);

        var chunks = await db.ModuleContentChunks
            .FromSqlRaw("""
                        SELECT *
                        FROM "module_content_chunks"
                        WHERE "ModuleId" = {0} AND "IsDeleted" = FALSE
                        ORDER BY "Embedding" <-> {1}
                        LIMIT {2}
                        """, moduleId, qEmb, topK)
            .AsNoTracking()
            .ToListAsync(ct);

        var context = string.Join("\n\n", chunks.Select(c =>
        {
            var src = c.SourceType == "pdf"
                ? $"PDF: {c.Title} (page {c.PageNumber})"
                : $"Element: {c.ModuleElementId} ({c.Title ?? "module content"})";

            return $"[{src}]\n{c.Content}";
        }));

        const string system = """
                              You are an academic assistant for a university module.
                              Use ONLY the provided context.
                              If you reference a module element, append exactly: (Element: <elementId>)
                              If you reference a PDF, append exactly: (PDF: <filename>, page <pageNumber>)
                              Do not invent Ids or pages. Only use what appears in the context labels.
                              If the answer is not in the context, say you cannot find it in the module materials.
                              UNDER NOT CIRCUMSTANCES CAN YOU ACCESS THE WWW.
                              """;

        var user = $"""
                    Context: 
                    {context}

                    Questions:
                    {message}
                    """;

        var answer = await ai.ChatAsync(system, user, ct);

        var sources = chunks.Select(c => new RagSourceDto(
            c.SourceType,
            c.ModuleElementId,
            c.ModuleFileId,
            c.Title,
            c.PageNumber,
            c.Content.Length > 220 ? c.Content[..220] + "..." : c.Content
        )).ToList();

        var extracted = RagAnswerParsing.TryExtractElementId(answer);
        Guid? highlight = null;

        if (extracted is { } id && sources.Any(s => s.ModuleElementId == id))
        {
            highlight = id;
        }

        return new RagChatResponse(answer, sources, highlight);
    }
}