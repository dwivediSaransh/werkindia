namespace IndiaWorks.Infrastructure.Persistence.Seed;

public sealed class SeedDataException : Exception
{
    public SeedDataException(string message) : base(message) { }
    public SeedDataException(string message, Exception inner) : base(message, inner) { }
}
