using System.Collections.Concurrent;

namespace IndiaWorks.Application.Features.E01.Implement;

public sealed class InMemoryOtpRateLimiter : IOtpRateLimiter
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();

    public bool IsRateLimited(string key, int maxPerWindow, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var entry = _entries.GetOrAdd(key, _ => new RateLimitEntry());

        // Clean old entries
        var cutoff = now - window;
        entry.Records.RemoveAll(t => t < cutoff);

        return entry.Records.Count > maxPerWindow;
    }

    public void Record(string key)
    {
        var now = DateTime.UtcNow;
        var entry = _entries.GetOrAdd(key, _ => new RateLimitEntry());
        entry.Records.Add(now);
    }

    public void Clear() => _entries.Clear();

    private sealed class RateLimitEntry
    {
        public List<DateTime> Records { get; } = new();
    }
}
