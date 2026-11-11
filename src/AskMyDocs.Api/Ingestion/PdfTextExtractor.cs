using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace AskMyDocs.Api.Ingestion;

public sealed record PdfPage(int Number, string Text);

public static class PdfTextExtractor
{
    public static IReadOnlyList<PdfPage> ExtractPages(byte[] pdfBytes)
    {
        using var pdf = PdfDocument.Open(pdfBytes);

        return pdf.GetPages()
            .Select(page => new PdfPage(page.Number, ContentOrderTextExtractor.GetText(page)))
            .ToList();
    }
}
