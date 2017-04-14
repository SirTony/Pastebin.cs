namespace Pastebin
{
    /// <summary>
    ///     Specifies how the library will handle request rate limiting.
    /// </summary>
    public enum RateLimitMode
    {
        /// <summary>
        ///     Do not do any special handling for rate limiting.
        ///     When the rate limit is reached an exception will be thrown.
        /// </summary>
        None,

        /// <summary>
        ///     Space out requests evenly over a period of 60 seconds by calling Thread.Sleep().
        ///     Only one request will be allowed every second.
        /// </summary>
        Pace,

        /// <summary>
        ///     Does not restrict the time between requests, but only allows 60 requests per minute.
        ///     Uses Thread.Sleep() to handle rate limiting.
        /// </summary>
        Burst
    }
}
