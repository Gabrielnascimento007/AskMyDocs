using AskMyDocs.Api.Ingestion;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace AskMyDocs.Api.Tests.Ingestion;

public class PdfTextExtractorTests
{
    [Fact]
    public void ExtractPages_ReturnsTextWithPageNumbers()
    {
        var pdf = BuildPdf("First page content", "Second page content");

        var pages = PdfTextExtractor.ExtractPages(pdf);

        Assert.Collection(pages,
            page =>
            {
                Assert.Equal(1, page.Number);
                Assert.Contains("First page content", page.Text);
            },
            page =>
            {
                Assert.Equal(2, page.Number);
                Assert.Contains("Second page content", page.Text);
            });
    }

    [Fact]
    public void ExtractPages_Throws_ForInvalidPdf()
    {
        Assert.ThrowsAny<Exception>(() => PdfTextExtractor.ExtractPages("%PDF-not really a pdf"u8.ToArray()));
    }

    private static byte[] BuildPdf(params string[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var text in pageTexts)
        {
            var page = builder.AddPage(PageSize.A4);
            page.AddText(text, 12, new PdfPoint(25, 700), font);
        }

        return builder.Build();
    }
}
