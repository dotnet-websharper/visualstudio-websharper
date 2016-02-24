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
                r.NuGet("FsNuget").Reference()
                r.NuGet("IntelliFactory.Core").Version("0.2", true).Reference()
                r.NuGet("IntelliFactory.Build").Version("0.2", true).Reference()
            ])
        |> addZafirConstant

bt.Solution [
    main
]
|> bt.Dispatch
