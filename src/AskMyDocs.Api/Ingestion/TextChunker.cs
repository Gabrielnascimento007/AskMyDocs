using System.Text.RegularExpressions;

namespace AskMyDocs.Api.Ingestion;

public static partial class TextChunker
{
    public const int DefaultChunkSize = 1000;
    public const int DefaultOverlap = 200;

    /// <summary>
    /// Splits text into chunks of at most <paramref name="chunkSize"/> characters,
    /// breaking on whitespace and repeating <paramref name="overlap"/> characters
    /// between consecutive chunks so context is not lost at the boundaries.
    /// </summary>
    public static IEnumerable<string> Split(string text, int chunkSize = DefaultChunkSize, int overlap = DefaultOverlap)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkSize, 2);
        ArgumentOutOfRangeException.ThrowIfNegative(overlap);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(overlap, chunkSize / 2);

        var normalized = Normalize(text);
        if (normalized.Length == 0)
        {
            yield break;
        }

        var start = 0;
        while (start < normalized.Length)
        {
            var end = Math.Min(start + chunkSize, normalized.Length);

            if (end < normalized.Length)
            {
                // Prefer to cut at the last space in the second half of the window.
                var lastSpace = normalized.LastIndexOf(' ', end - 1, end - (start + chunkSize / 2));
                if (lastSpace > start)
                {
                    end = lastSpace;
                }
            }

            yield return normalized[start..end].Trim();

            if (end >= normalized.Length)
            {
                yield break;
            }

            // Step back by the overlap, then move forward to the start of a word.
            start = end - overlap;
            if (normalized[start - 1] != ' ')
            {
                var nextSpace = normalized.IndexOf(' ', start, end - start);
                if (nextSpace >= 0)
                {
                    start = nextSpace + 1;
                }
            }
        }
    }

    private static string Normalize(string text) =>
        // PostgreSQL text columns reject NUL characters, which some PDFs contain.
        Whitespace().Replace(text.Replace('\0', ' '), " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
