using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle( "Pastebin" )]
[assembly: AssemblyDescription( "A C# wrapper around the Pastebin API" )]
[assembly: AssemblyConfiguration( "Debug" )]
[assembly: AssemblyCompany( "Tony Montana" )]
[assembly: AssemblyProduct( "Pastebin.cs" )]
[assembly: AssemblyCopyright( "Copyright © Tony Montana 2015" )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]
[assembly: Guid( "32AA516A-0206-4BFE-97F3-73522408E1E4" )]
[assembly: AssemblyFileVersion( Pastebin.AssemblyVersion.FileVersion )]
[assembly: AssemblyVersion( Pastebin.AssemblyVersion.Version )]
[assembly: AssemblyInformationalVersion( Pastebin.AssemblyVersion.FileVersion )]

namespace Pastebin
{
    internal static class AssemblyVersion
    {
        public const string Version = "1.0";
        public const string FileVersion = "1.0.3.10";
    }
}