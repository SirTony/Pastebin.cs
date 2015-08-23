using System;

namespace Pastebin
{
    /// <summary>
    /// Thrown when the request rate limit has been met or exceeded for a given period.
    /// </summary>
    public sealed class PastebinRateLimitException : PastebinException
    {
        internal PastebinRateLimitException( int timeLeft )
            : base( String.Format( "Maximum number of requests has been exceeded this period. Please wait {0} seconds", timeLeft ) )
        { }
    }
}
