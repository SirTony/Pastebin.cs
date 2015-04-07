using System;

/// <summary>
/// The root namespace for all Pastebin API components.
/// </summary>
namespace Pastebin
{
    internal static class Utils
    {
        private static readonly DateTime UnixEpoch;

        static Utils()
        {
            UnixEpoch = new DateTime( 1970, 1, 1, 0, 0, 0, 0 );
        }

        public static DateTime FromUnixTime( int timestamp )
        {
            return UnixEpoch.AddSeconds( timestamp );
        }
    }
}