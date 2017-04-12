namespace Pastebin
{
    /// <summary>
    ///     An enumeration defining paste protection levels.
    /// </summary>
    public enum PasteExposure
    {
        /// <summary>
        ///     The paste is visible to everyone and will appear in search results and the newest pastes listing.
        /// </summary>
        Public = 0,

        /// <summary>
        ///     The paste is visible only to thos with the link; it will not appear in search results or the newest pastes listing.
        /// </summary>
        Unlisted = 1,

        /// <summary>
        ///     The paste is visible only to the user who created it. Must be logged in to use.
        /// </summary>
        Private = 2
    }
}
