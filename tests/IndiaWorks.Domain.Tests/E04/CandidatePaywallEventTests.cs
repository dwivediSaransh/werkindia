using IndiaWorks.Domain.E04;
using Xunit;

namespace IndiaWorks.Domain.Tests.E04;

public sealed class CandidatePaywallEventTests
{
    [Fact]
    public void Create_WithValidParameters_CapturesCommercialContext()
    {
        var organisationId = Guid.NewGuid();
        var candidateProfileId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var paywallEvent = CandidatePaywallEvent.Create(
            organisationId,
            candidateProfileId,
            " PAYWALL_REQUIRED ",
            " job:registered-nurse ",
            freeLimit: 5,
            freeUsed: 5,
            freeRemaining: 0,
            AccessSource.FreeAllowance,
            correlationId);

        Assert.NotEqual(Guid.Empty, paywallEvent.Id);
        Assert.Equal(organisationId, paywallEvent.OrganisationId);
        Assert.Equal(candidateProfileId, paywallEvent.CandidateProfileId);
        Assert.Equal("PAYWALL_REQUIRED", paywallEvent.Reason);
        Assert.Equal("job:registered-nurse", paywallEvent.JobContext);
        Assert.Equal(5, paywallEvent.FreeLimit);
        Assert.Equal(5, paywallEvent.FreeUsed);
        Assert.Equal(0, paywallEvent.FreeRemaining);
        Assert.Equal(AccessSource.FreeAllowance, paywallEvent.AccessSource);
        Assert.Equal(correlationId, paywallEvent.CorrelationId);
        Assert.True(paywallEvent.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithEmptyOrganisationId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateValid(organisationId: Guid.Empty));
        Assert.Equal("organisationId", ex.ParamName);
    }

    [Fact]
    public void Create_WithEmptyCandidateProfileId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateValid(candidateProfileId: Guid.Empty));
        Assert.Equal("candidateProfileId", ex.ParamName);
    }

    [Fact]
    public void Create_WithBlankReason_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(reason: " "));
    }

    [Fact]
    public void Create_WithBlankJobContext_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(jobContext: " "));
    }

    [Fact]
    public void Create_WhenFreeUsedExceedsLimit_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateValid(freeLimit: 5, freeUsed: 6, freeRemaining: 0));
        Assert.Equal("freeUsed", ex.ParamName);
    }

    [Fact]
    public void Create_WhenFreeRemainingDoesNotMatchUsage_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateValid(freeLimit: 5, freeUsed: 4, freeRemaining: 0));
        Assert.Equal("freeRemaining", ex.ParamName);
    }

    [Fact]
    public void Create_WithEmptyCorrelationId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateValid(correlationId: Guid.Empty));
        Assert.Equal("correlationId", ex.ParamName);
    }

    private static CandidatePaywallEvent CreateValid(
        Guid? organisationId = null,
        Guid? candidateProfileId = null,
        string reason = "PAYWALL_REQUIRED",
        string jobContext = "job:registered-nurse",
        int freeLimit = 5,
        int freeUsed = 5,
        int freeRemaining = 0,
        Guid? correlationId = null)
    {
        return CandidatePaywallEvent.Create(
            organisationId ?? Guid.NewGuid(),
            candidateProfileId ?? Guid.NewGuid(),
            reason,
            jobContext,
            freeLimit,
            freeUsed,
            freeRemaining,
            AccessSource.FreeAllowance,
            correlationId ?? Guid.NewGuid());
    }
}
