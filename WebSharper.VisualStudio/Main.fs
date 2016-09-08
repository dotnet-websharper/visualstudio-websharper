module WebSharper.VisualStudio.Main

open System.IO
module VSI = WebSharper.VisualStudio.VSIntegration

let root =
    Path.Combine(__SOURCE_DIRECTORY__, "..")
    |> Path.GetFullPath

let configureVSI wsNupkgPath extraNupkgPaths wsTemplatesNupkgPath isCSharp defaultOutDir : VSI.Config =
    let vsixPath =
        match System.Environment.GetEnvironmentVariable "NuGetPackageOutputPath" with
        | null -> Path.Combine(defaultOutDir, Path.GetFileNameWithoutExtension(wsNupkgPath) + ".vsix")
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

let findPackage (id) =
    printf "Searching for %s nupkg... " id

    let dirs = 
        Directory.GetDirectories("packages", id + ".*")
        |> Array.filter (fun dir -> 
            let dn = Path.GetFileName(dir)
            let c = dn.[id.Length + 1]
            System.Char.IsNumber c
        )

    if Array.isEmpty dirs then
        failwithf "failed to find package %s" id

    let maxDir =
        dirs
        |> Seq.maxBy (fun dir ->
            let dn = Path.GetFileName(dir)
            let version = dn.[id.Length + 1 .. ].Split('-').[0]
            printfn "version: %s" version
            System.Version.Parse(dn.[id.Length + 1 .. ].Split('-').[0])
        )
    let nupkgPath = Path.Combine(maxDir, Path.GetFileName(maxDir) + ".nupkg")
    printfn "found: %s" (Path.GetFileName(nupkgPath))
    id, nupkgPath

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
        let _, wsTemplatesDir = findPackage(wsName + ".Templates")
        let extra =
            [
                "IntelliFactory.Xml"
                wsName
                wsName + ".Html"
                wsName + ".Owin"
                wsName + ".Suave"
                wsName + ".UI.Next"
#if ZAFIR
                (if isCSharp then "Zafir.CSharp" else "Zafir.FSharp")
                "FSharp.Core"
#endif
                "Owin"
                "Microsoft.Owin"
                "Microsoft.Owin.Diagnostics"
                "Microsoft.Owin.FileSystems"
                "Microsoft.Owin.Host.HttpListener"
                "Microsoft.Owin.Hosting"
                "Microsoft.Owin.StaticFiles"
                "Mono.Cecil"
                "Suave"
            ]
            |> List.map findPackage
            |> Map.ofList
        let ws = extra.[wsName]
        let defaultOutDir = Path.Combine(System.Environment.CurrentDirectory, "build")
        configureVSI ws extra wsTemplatesDir isCSharp defaultOutDir
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
