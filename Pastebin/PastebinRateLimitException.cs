using System;

namespace Pastebin
{
    /// <summary>
    ///     Thrown when the request rate limit has been met or exceeded for a given period.
    /// </summary>
    public sealed class PastebinRateLimitException : PastebinException
    {
        /// <summary>
        ///     The amount of time left before more requests may be made.
        /// </summary>
        public TimeSpan WaitTimeLeft { get; }

        internal PastebinRateLimitException( TimeSpan timeLeft )
            : base(
                "Maximum number of requests has been exceeded  this period. " +
                $"Please wait {(int)timeLeft.TotalSeconds} seconds" ) => this.WaitTimeLeft = timeLeft;
    }
}
