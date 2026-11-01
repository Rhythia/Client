using System;

public class RateLimitedException : Exception
{
    public TimeSpan? RetryAfter { get; }

    public RateLimitedException(TimeSpan? retryAfter)
        : base("Rate limited")
    {
        RetryAfter = retryAfter;
    }
}
