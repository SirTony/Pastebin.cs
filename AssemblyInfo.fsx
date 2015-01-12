#r "Tools/FakeLib.dll"

open System
open System.Text
open System.Reflection

open Microsoft.CSharp
open System.CodeDom.Compiler

open Fake
open Fake.Git
open Fake.Git.CommandHelper
open Fake.AssemblyInfoFile

[<Literal>]
let AssemblyInfoFile = "..\\..\\..\\Pastebin\\Properties\\AssemblyInfo.cs"

let getCurrentAssemblyInfo = 
    let provider   = new CSharpCodeProvider()
    let parameters = new CompilerParameters()
    parameters.GenerateExecutable <- false
    parameters.GenerateInMemory <- true
    parameters.IncludeDebugInformation <- false
    
    let results = provider.CompileAssemblyFromFile( parameters, AssemblyInfoFile )

    if results.Errors.HasErrors then
        let builder = new StringBuilder()

        for e in results.Errors do
            if not e.IsWarning then
                 builder.AppendFormat ( "({0}) {1} [{2}:{3}] :: {4}", e.ErrorNumber, e.FileName, e.Line, e.Column, e.ErrorText ) |> ignore
                 builder.AppendLine() |> ignore

        failwithf "Could not load assembly info\n\n%s" ( builder.ToString() )

    let asm = results.CompiledAssembly
    let fileVersion = results.CompiledAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()
    
    if fileVersion = null then
        failwith "Must have AssemblyFileVersionAttribute"

    ( fileVersion.Version.Split '.' ) |> Array.map ( fun x -> int x )

let generateAssemblyInfo isDebug =
    let ( ok, output, _ ) = runGitCommand "." "rev-list HEAD --count"
    let version = getCurrentAssemblyInfo
    let mutable verStr = ""

    if not isDebug then
        version.[2] <- version.[2] + 1

    if not ok then
        verStr <- sprintf "%u.%u.*" version.[0] version.[1]
    else
        version.[3] <- int output.[0]
        verStr <- version |> Array.map ( fun x -> string x ) |> String.concat "."

    CreateCSharpAssemblyInfo AssemblyInfoFile
        [
            Attribute.Title "Pastebin"
            Attribute.Description "A C# wrapper around the Pastebin API."
            Attribute.Configuration ( if isDebug then "Debug" else "Release" )
            Attribute.Company "Tony Montana"
            Attribute.Product "Pastebin.cs"
            Attribute.Copyright "Copyright \u00A9 Tony Montana 2015"
            Attribute.Trademark ""
            Attribute.Culture ""
            Attribute.Guid "32AA516A-0206-4BFE-97F3-73522408E1E4"
            Attribute.FileVersion verStr
            Attribute.Version ( sprintf "%u.%u" version.[0] version.[1] )
            Attribute.InformationalVersion ( sprintf "%u.%u" version.[0] version.[1] )
        ]

let argv = Environment.GetCommandLineArgs()

generateAssemblyInfo ( if argv.Length < 3 then true else argv.[2] = "Debug" )