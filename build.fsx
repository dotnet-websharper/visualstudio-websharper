#load "tools/includes.fsx"
open IntelliFactory.Build
open System.IO

let bt =
    BuildTool().PackageId("WebSharper.VisualStudio", "3.0-alpha")

let templates =
    bt.WithFramework(bt.Framework.Net40)
        .FSharp.Library("IntelliFactory.WebSharper.Templates")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.Assembly("System.Xml")
                r.Assembly("System.Xml.Linq")
                r.NuGet("FsNuget").Reference()
                r.NuGet("SharpCompress").Reference()
            ])

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
    templates

    bt.PackageId("WebSharper.Templates").NuGet.CreatePackage()
        .Configure(fun c ->
            { c with
                Title = Some "WebSharper.Templates"
                LicenseUrl = Some "http://websharper.com/licensing"
                ProjectUrl = Some "https://github.com/intellifactory/websharper.visualstudio"
                Description = "WebSharper Project Templates"
                RequiresLicenseAcceptance = true })
        .Add(templates)
    |> Array.foldBack (fun f n -> n.AddFile(f)) (
        let templatesDir = DirectoryInfo("templates").FullName
        Directory.GetFiles(templatesDir, "*", SearchOption.AllDirectories)
        |> Array.map (fun fullPath ->
            fullPath, "templates/" + fullPath.[templatesDir.Length + 1 ..].Replace('\\', '/'))
    )
]
|> bt.Dispatch
