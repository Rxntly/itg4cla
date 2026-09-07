using System.Collections.Concurrent;
using ITG_Cafeteria.Server.Models.DTOs;

namespace ITG_Cafeteria.Server.Services;

public interface IExcelImportService
{
    Task<ImportPreviewResponse> ParseAsync(Stream fileStream, string fileName);
    ImportPreviewResponse? GetPreview(string previewId);
    void RemovePreview(string previewId);
}

public class ExcelImportService : IExcelImportService
{
    private static readonly ConcurrentDictionary<string, ImportPreviewResponse> Previews = new();

    public async Task<ImportPreviewResponse> ParseAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // Offload the heavy file extraction processing to a background thread.
        // This keeps the secure HTTPS pipeline thread open and avoids lockouts under the debugger.
        var result = await Task.Run(() =>
        {
            return extension == ".pdf"
                ? HolcomMenuPdfParser.Parse(fileStream, fileName)
                : HolcomMenuExcelParser.Parse(fileStream, fileName);
        });

        if (result.Days.All(d => d.Nodes.Count == 0))
        {
            result.Warnings.Add("No menu content was detected. Ensure the file uses the Holcom grid format (see Menu/EXCEL_FORMAT.md).");
        }

        var preview = new ImportPreviewResponse
        {
            PreviewId = Guid.NewGuid().ToString("N"),
            DetectedWeekStart = result.WeekStartDate,
            DetectedWeekEnd = result.WeekEndDate,
            SheetName = result.SheetName,
            Format = extension == ".pdf" ? "HolcomGridPdf" : "HolcomGrid",
            Days = result.Days,
            Warnings = result.Warnings,
            Summary = result.Summary
        };

        Previews[preview.PreviewId] = preview;
        return preview;
    }

    public ImportPreviewResponse? GetPreview(string previewId) =>
        Previews.TryGetValue(previewId, out var preview) ? preview : null;

    public void RemovePreview(string previewId) => Previews.TryRemove(previewId, out _);
}
