#load "tools/includes.fsx"
open IntelliFactory.Build
open System.IO

let bt =
    BuildTool().PackageId("WebSharper.VisualStudio")
        .VersionFrom("WebSharper")

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
                r.NuGet("WebSharper").Latest(true).Reference()
                r.NuGet("WebSharper.Html").Latest(true).Reference()
                r.NuGet("WebSharper.Owin").Latest(true).Reference()
                r.NuGet("WebSharper.UI.Next").Latest(true).Reference()
                r.NuGet("WebSharper").Latest(true).Reference()
                r.NuGet("WebSharper.UI.Next").Latest(true).Reference()
                r.NuGet("WebSharper.Templates").Latest(true).Reference()

                r.NuGet("FSharp.Core").Version("[4.0.0.1]").Reference()
                r.NuGet("Owin").Reference()
                r.NuGet("Microsoft.Owin").Reference()
                r.NuGet("Microsoft.Owin.Diagnostics").Reference()
                r.NuGet("Microsoft.Owin.FileSystems").Reference()
                r.NuGet("Microsoft.Owin.Host.HttpListener").Reference()
                r.NuGet("Microsoft.Owin.Hosting").Reference()
                r.NuGet("Microsoft.Owin.StaticFiles").Reference()
                r.NuGet("Mono.Cecil").Reference()
            ])

bt.Solution [
    main
]
|> bt.Dispatch

File.Copy(
    Path.Combine(__SOURCE_DIRECTORY__, "WebSharper.VisualStudio", "App.config"),
    Path.Combine(__SOURCE_DIRECTORY__, "build", "net45",  "WebSharper.VisualStudio.exe.config"),
    true
    )