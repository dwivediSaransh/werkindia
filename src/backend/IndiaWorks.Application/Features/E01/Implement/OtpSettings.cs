namespace IndiaWorks.Application.Features.E01.Implement;

public sealed record OtpSettings(
    int CodeLength = 6,
    int TtlSeconds = 300,
    int MaxAttempts = 5,
    int ResendCooldownSeconds = 30,
    int RateLimitMaxPerWindow = 5,
    int RateLimitWindowSeconds = 300)
{
    public TimeSpan Ttl => TimeSpan.FromSeconds(TtlSeconds);
    public TimeSpan ResendCooldown => TimeSpan.FromSeconds(ResendCooldownSeconds);
    public TimeSpan RateLimitWindow => TimeSpan.FromSeconds(RateLimitWindowSeconds);
}
