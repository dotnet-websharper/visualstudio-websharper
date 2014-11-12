#load "tools/includes.fsx"
open IntelliFactory.Build

let bt =
    BuildTool().PackageId("WebSharper.VisualStudio", "2.5-alpha")

let main =
    bt.FSharp.ConsoleExecutable("IntelliFactory.WebSharper.VisualStudio")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.Assembly("System.Xml")
                r.Assembly("System.Xml.Linq")
                r.Assembly("System.IO.Compression")
                r.Assembly("System.IO.Compression.FileSystem")
                r.NuGet("FsNuget").Reference()
                r.NuGet("IntelliFactory.Core").Reference()
                r.NuGet("IntelliFactory.Build").Reference()
            ])

bt.Solution [
    main
]
|> bt.Dispatch
