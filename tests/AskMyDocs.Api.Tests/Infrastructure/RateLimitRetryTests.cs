using AskMyDocs.Api.Infrastructure.AI;

namespace AskMyDocs.Api.Tests.Infrastructure;

public class RateLimitRetryTests
{
    [Fact]
    public void GetDelay_UsesTheRetryHintFromGeminiMessage()
    {
        const string message = "Quota exceeded for metric: embed_content_free_tier_tokens. Please retry in 19.940539451s.";

        Assert.Equal(TimeSpan.FromSeconds(21), RateLimitRetry.GetDelay(null, message));
    }

    [Fact]
    public void GetDelay_PrefersTheRetryAfterHeader()
    {
        Assert.Equal(TimeSpan.FromSeconds(6), RateLimitRetry.GetDelay("5", "Please retry in 40s."));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "Too many requests")]
    [InlineData("not-a-number", "")]
    public void GetDelay_FallsBackToTheDefault_WhenThereIsNoHint(string? header, string? message)
    {
        Assert.Equal(RateLimitRetry.DefaultDelay, RateLimitRetry.GetDelay(header, message));
    }

    [Fact]
    public void GetDelay_IsCapped()
    {
        Assert.Equal(RateLimitRetry.MaxDelay, RateLimitRetry.GetDelay(null, "Please retry in 3600s."));
    }
}
