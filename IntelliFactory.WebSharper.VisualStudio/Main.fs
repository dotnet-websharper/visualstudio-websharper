module IntelliFactory.WebSharper.VisualStudio.Main

open System.IO
module VSI = IntelliFactory.WebSharper.VisualStudio.VSIntegration

let root =
    Path.Combine(__SOURCE_DIRECTORY__, "..")
    |> Path.GetFullPath

let configureVSI wsNupkgPath extraNupkgPaths : VSI.Config =
    let vsixPath = Path.ChangeExtension(wsNupkgPath, ".vsix")
    {
        NuPkgPath = wsNupkgPath
        ExtraNuPkgPaths = extraNupkgPaths
        RootPath = root
        VsixPath = vsixPath
    }

let downloadPackage (source, id) =
    printf "Downloading %s nupkg..." id
    let pkg = FsNuGet.Package.GetLatest(id, ?source = source)
    let path = sprintf "build/%s.%s.nupkg" pkg.Id pkg.Version
    let fullPath = Path.Combine(Directory.GetCurrentDirectory(), path)
    pkg.SaveToFile(fullPath)
    printfn " Got %s." path
    pkg.Id, fullPath

[<EntryPoint>]
let main argv =
    let vsixConfig =
        let localPath = System.Environment.GetEnvironmentVariable("LocalNuget")
        let local = Some (FsNuGet.FileSystem localPath)
        let online = None
        let _, ws = downloadPackage (local, "WebSharper")
        let extra =
            [
                local, "WebSharper.Owin"
                online, "Owin"
                online, "Microsoft.Owin"
                online, "Microsoft.Owin.Diagnostics"
                online, "Microsoft.Owin.FileSystems"
                online, "Microsoft.Owin.Host.HttpListener"
                online, "Microsoft.Owin.Hosting"
                online, "Microsoft.Owin.SelfHost"
                online, "Microsoft.Owin.StaticFiles"
                online, "Mono.Cecil"
            ]
            |> List.map downloadPackage
            |> Map.ofList
        configureVSI ws extra
    printf "Generating vsix installer..."
    VSI.BuildVsixFile vsixConfig
    printfn " Created %s." vsixConfig.VsixPath
    0
