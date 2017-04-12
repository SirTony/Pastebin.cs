namespace Pastebin
{
    /// <summary>
    ///     An enumeration defining paste expiration times.
    /// </summary>
    public enum PasteExpiration
    {
        /// <summary>
        ///     The paste will never expire.
        /// </summary>
        Never,

        /// <summary>
        ///     The paste will expire ten (10) minutes after being submitted.
        /// </summary>
        TenMinutes,

        /// <summary>
        ///     The paste will expire one (1) hour after being submitted.
        /// </summary>
        OneHour,

        /// <summary>
        ///     The paste will expire one (1) day after being submitted.
        /// </summary>
        OneDay,

        /// <summary>
        ///     The paste will expire one (1) week after being submitted.
        /// </summary>
        OneWeek,

        /// <summary>
        ///     The paste will expire two (2) weeks after being submitted.
        /// </summary>
        TwoWeeks,

        /// <summary>
        ///     The paste will expire one (1) month after being submitted.
        /// </summary>
        OneMonth
    }
}
