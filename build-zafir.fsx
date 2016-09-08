#load "tools/includes.fsx"
open IntelliFactory.Build
open System.IO

let bt =
    BuildTool().PackageId("Zafir.VisualStudio")
        .VersionFrom("Zafir")

open IntelliFactory.Core.Parametrization
let addZafirConstant (p: FSharpProject) =
    let pp = p :> IParametric<_> 
    pp.Parameters.AppendCustom(FSharpConfig.OtherFlags, "--define:ZAFIR")
    |> pp.WithParameters

let main =
    bt.FSharp.ConsoleExecutable("WebSharper.VisualStudio")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.Assembly("System.Xml")
                r.Assembly("System.Xml.Linq")
                r.Assembly("System.IO.Compression")
                r.Assembly("System.IO.Compression.FileSystem")
                r.NuGet("IntelliFactory.Core").Version("0.2", true).Reference()
                r.NuGet("IntelliFactory.Build").Version("0.2", true).Reference()

                // fake references
                r.NuGet("IntelliFactory.Xml").Reference()
                r.NuGet("Zafir").Latest(true).Reference()
                r.NuGet("Zafir.FSharp").Latest(true).Reference()
                r.NuGet("Zafir.CSharp").Latest(true).Reference()
                r.NuGet("Zafir.Html").Latest(true).Reference()
                r.NuGet("Zafir.Owin").Latest(true).Reference()
                r.NuGet("Zafir.Suave").Latest(true).Reference()
                r.NuGet("Zafir.UI.Next").Latest(true).Reference()
                r.NuGet("Zafir").Latest(true).Reference()
                r.NuGet("Zafir.UI.Next").Latest(true).Reference()
                r.NuGet("Zafir.Templates").Latest(true).Reference()

                r.NuGet("FSharp.Core").Reference()
                r.NuGet("Owin").Reference()
                r.NuGet("Microsoft.Owin").Reference()
                r.NuGet("Microsoft.Owin.Diagnostics").Reference()
                r.NuGet("Microsoft.Owin.FileSystems").Reference()
                r.NuGet("Microsoft.Owin.Host.HttpListener").Reference()
                r.NuGet("Microsoft.Owin.Hosting").Reference()
                r.NuGet("Microsoft.Owin.StaticFiles").Reference()
                r.NuGet("Mono.Cecil").Reference()
                r.NuGet("Suave").Reference()

            ])
        |> addZafirConstant

bt.Solution [
    main
]
|> bt.Dispatch

File.Copy(
    Path.Combine(__SOURCE_DIRECTORY__, "WebSharper.VisualStudio", "App.config"),
    Path.Combine(__SOURCE_DIRECTORY__, "build", "net45",  "WebSharper.VisualStudio.exe.config"),
    true
    )