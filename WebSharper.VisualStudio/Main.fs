module WebSharper.VisualStudio.Main

open System.IO
module VSI = WebSharper.VisualStudio.VSIntegration

let root =
    Path.Combine(__SOURCE_DIRECTORY__, "..")
    |> Path.GetFullPath

let configureVSI wsNupkgPath extraNupkgPaths wsTemplatesNupkgPath isCSharp : VSI.Config =
    let vsixPath =
        match System.Environment.GetEnvironmentVariable "NuGetPackageOutputPath" with
        | null -> Path.ChangeExtension(wsNupkgPath, ".vsix")
        | dir -> Path.Combine(dir, Path.GetFileNameWithoutExtension(wsNupkgPath) + ".vsix")
#if ZAFIR
    let vsixPath =
        Path.Combine(Path.GetDirectoryName vsixPath, 
            (Path.GetFileName vsixPath).Replace("Zafir", if isCSharp then "Zafir.CSharp" else "Zafir.FSharp"))
#endif
    let wsTemplatesPath =
        Path.Combine(
            Path.GetDirectoryName(wsTemplatesNupkgPath),
            Path.GetFileNameWithoutExtension(wsTemplatesNupkgPath))
    if Directory.Exists wsTemplatesPath then Directory.Delete(wsTemplatesPath, true)
    Directory.CreateDirectory(wsTemplatesPath) |> ignore
    Compression.ZipFile.ExtractToDirectory(wsTemplatesNupkgPath, wsTemplatesPath)
    {
        NuPkgPath = wsNupkgPath
        ExtraNuPkgPaths = extraNupkgPaths
        RootPath = root
        TemplatesPath = Path.Combine(wsTemplatesPath, "templates")
        VsixPath = vsixPath
        IsCSharp = isCSharp
    }

let downloadPackage (source, id, version) =
    printf "Downloading %s nupkg..." id
    let pkg = 
        match version with
        | None -> FsNuGet.Package.GetLatest(id, ?source = source)
        | Some v -> FsNuGet.Package.GetAtVersion(id, v, ?source = source)
    let path = Path.Combine("build", sprintf "%s.%s.nupkg" pkg.Id pkg.Version)
    let fullPath = Path.Combine(Directory.GetCurrentDirectory(), path)
    pkg.SaveToFile(fullPath)
    printfn " Got %s." path
    pkg.Id, fullPath

let wsName =
#if ZAFIR
    "Zafir"
#else
    "WebSharper"
#endif

[<EntryPoint>]
let main argv =
    let getVsixConfig isCSharp =
        let online = None
        let local =
            match System.Environment.GetEnvironmentVariable("LocalNuget") with
            | null ->
                eprintfn "Warning: LocalNuget variable not set, using online repository."
                online
            | localPath -> Some (FsNuGet.FileSystem localPath)
        let _, wsTemplatesDir = downloadPackage(local, wsName + ".Templates")
        let extra =
            [
                local, "IntelliFactory.Xml", None
                local, wsName, None
                local, wsName + ".Html", None
                local, wsName + ".Owin", None
                local, wsName + ".Suave", None
                local, wsName + ".UI.Next", None
#if ZAFIR
                local, (if isCSharp then "Zafir.CSharp" else "Zafir.FSharp"), None
                online, "FSharp.Core", Some "4.0.0.1"
#endif
                online, "Owin", None
                online, "Microsoft.Owin", None
                online, "Microsoft.Owin.Diagnostics", None
                online, "Microsoft.Owin.FileSystems", None
                online, "Microsoft.Owin.Host.HttpListener", None
                online, "Microsoft.Owin.Hosting", None
                online, "Microsoft.Owin.StaticFiles", None
                online, "Mono.Cecil", None
                local, "Suave", None
            ]
            |> List.map downloadPackage
            |> Map.ofList
        let ws = extra.[wsName]
        configureVSI ws extra wsTemplatesDir isCSharp
#if ZAFIR
    printf "Generating F# vsix installer..."
    let vsixConfig = getVsixConfig false
    VSI.BuildVsixFile vsixConfig
    printfn " Created %s." vsixConfig.VsixPath
    printf "Generating C# vsix installer..."
    let vsixConfig = getVsixConfig true
    VSI.BuildVsixFile vsixConfig
    printfn " Created %s." vsixConfig.VsixPath
#else
    printf "Generating vsix installer..."
    let vsixConfig = getVsixConfig false
    VSI.BuildVsixFile vsixConfig
    printfn " Created %s." vsixConfig.VsixPath
#endif
    0
