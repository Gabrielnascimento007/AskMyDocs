using AskMyDocs.Api.Ingestion;

namespace AskMyDocs.Api.Tests.Ingestion;

public class TextChunkerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   \n\t  ")]
    public void Split_ReturnsNothing_ForBlankText(string text)
    {
        Assert.Empty(TextChunker.Split(text));
    }

    [Fact]
    public void Split_ReturnsSingleNormalizedChunk_ForShortText()
    {
        var chunks = TextChunker.Split("  Hello\n\nworld\t again  ").ToList();

        Assert.Equal(["Hello world again"], chunks);
    }

    [Fact]
    public void Split_RemovesNulCharacters()
    {
        var chunk = Assert.Single(TextChunker.Split("abc\0def"));

        Assert.DoesNotContain('\0', chunk);
    }

    [Fact]
    public void Split_RespectsChunkSize_AndNeverBreaksWords()
    {
        var words = Enumerable.Range(0, 500).Select(i => $"word{i}").ToList();
        var text = string.Join(' ', words);

        var chunks = TextChunker.Split(text, chunkSize: 200, overlap: 40).ToList();

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk =>
        {
            Assert.True(chunk.Length <= 200, $"Chunk too long: {chunk.Length}");
            Assert.All(chunk.Split(' '), word => Assert.Contains(word, words));
        });
    }

    [Fact]
    public void Split_CoversEveryWord_WithOverlapBetweenChunks()
    {
        var words = Enumerable.Range(0, 300).Select(i => $"w{i}").ToList();
        var text = string.Join(' ', words);

        var chunks = TextChunker.Split(text, chunkSize: 100, overlap: 30).ToList();

        var covered = chunks.SelectMany(c => c.Split(' ')).ToHashSet();
        Assert.Equal(words.Count, covered.Count);

        for (var i = 1; i < chunks.Count; i++)
        {
            var previousLastWord = chunks[i - 1].Split(' ')[^1];
            Assert.Contains(previousLastWord, chunks[i].Split(' '));
        }
    }

    [Fact]
    public void Split_HandlesTextWithoutSpaces()
    {
        var text = new string('x', 450);

        var chunks = TextChunker.Split(text, chunkSize: 100, overlap: 20).ToList();

        Assert.All(chunks, chunk => Assert.True(chunk.Length <= 100));
        Assert.Equal(text.Length, chunks.Sum(c => c.Length) - (chunks.Count - 1) * 20);
    }

    [Fact]
    public void Split_Throws_WhenOverlapIsTooLarge()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TextChunker.Split("text", chunkSize: 100, overlap: 50).ToList());
    }
}
