using System.ClientModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AskMyDocs.Api.Infrastructure.AI;

/// <summary>
/// Waits and retries when the AI provider answers 429 (rate limit), e.g. Gemini's free tier
/// limit of embedding tokens per minute. Used by background ingestion, where waiting a bit
/// is better than failing the whole document.
/// </summary>
public static partial class RateLimitRetry
{
    public const int MaxAttempts = 10;
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(90);

    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (ClientResultException ex) when (ex.Status == 429 && attempt < MaxAttempts)
            {
                var delay = GetDelay(ex.GetRawResponse()?.Headers.TryGetValue("Retry-After", out var header) == true ? header : null, ex.Message);

                logger.LogWarning(
                    "AI provider rate limit reached (attempt {Attempt}/{MaxAttempts}); waiting {Delay:0}s before retrying",
                    attempt, MaxAttempts, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Uses the Retry-After header or the "retry in 19.9s" hint in the error message,
    /// plus one second of margin; falls back to <see cref="DefaultDelay"/>.
    /// </summary>
    public static TimeSpan GetDelay(string? retryAfterHeader, string? message)
    {
        double? seconds = null;

        if (double.TryParse(retryAfterHeader, NumberStyles.Float, CultureInfo.InvariantCulture, out var headerSeconds))
        {
            seconds = headerSeconds;
        }
        else if (message is not null && RetryHint().Match(message) is { Success: true } match)
        {
            seconds = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        if (seconds is not > 0)
        {
            return DefaultDelay;
        }

        var delay = TimeSpan.FromSeconds(Math.Ceiling(seconds.Value) + 1);
        return delay > MaxDelay ? MaxDelay : delay;
    }

    [GeneratedRegex(@"retry in (\d+(?:\.\d+)?)s", RegexOptions.IgnoreCase)]
    private static partial Regex RetryHint();
}
