using System;

/// <summary>
/// The root namespace for all Pastebin API components.
/// </summary>
namespace Pastebin
{
    /// <summary>
    /// Thrown when a bad API request is made. Usually the result of an invalid API key, or when a request is made that requires authentication but no user is logged in.
    /// </summary>
    public class PastebinException : Exception
    {
        internal PastebinException( string message )
            : this( message, null )
        { }

        internal PastebinException( string message, Exception inner )
            : base( message, inner )
        { }
    }
}