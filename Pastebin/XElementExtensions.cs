using System;
using System.Xml.Linq;

/// <summary>
/// The root namespace for all Pastebin API components.
/// </summary>
namespace Pastebin
{
    internal static class XElementExtensions
    {
        public static string Value( this XElement element, string name )
            => element.Element( name ).Value;

        public static string ValueOrDefault( this XElement element, string name )
        {
            if( element.Element( name ) == null )
                return default( string );

            return element.Element( name ).Value;
        }

        public static T Value<T>( this XElement element, string name, Func<string, T> extractor )
        {
            var text = element.Value( name );
            return extractor( text );
        }
    }
}