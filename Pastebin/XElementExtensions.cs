using System;
using System.Xml.Linq;

/// <summary>
/// The root namespace for all Pastebin API components.
/// </summary>
namespace Pastebin
{
    internal static class XElementExtensions
    {
        public static string Get( this XElement element, string name )
        {
            return element.Element( name ).Value;
        }

        public static bool HasValueFor( this XElement element, string name )
        {
            return element.Element( name ) != null;
        }

        public static T Get<T, U>( this XElement element, string name, Func<string, U> extractor )
        {
            var text = element.Get( name );
            var value = (object)extractor( text );

            return (T)value;
        }

        public static T Get<T>( this XElement element, string name, Func<string, T> extractor )
        {
            var text = element.Get( name );
            return extractor( text );
        }
    }
}