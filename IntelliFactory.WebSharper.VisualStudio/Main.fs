module IntelliFactory.WebSharper.VisualStudio.Main

open System.IO
module VSI = IntelliFactory.WebSharper.VisualStudio.VSIntegration

let root =
    Path.Combine(__SOURCE_DIRECTORY__, "..")
    |> Path.GetFullPath

let configureVSI nupkgPath : VSI.Config =
    let vsixPath = Path.ChangeExtension(nupkgPath, ".vsix")
    {
        NuPkgPath = nupkgPath
        RootPath = root
        VsixPath = vsixPath
    }

let downloadWebSharperPackage () =
    printf "Downloading WebSharper nupkg..."
    let pkg = FsNuGet.Package.GetLatest("WebSharper")
    let path = sprintf "build/WebSharper.%s.nupkg" pkg.Version
    pkg.SaveToFile(Path.Combine(Directory.GetCurrentDirectory(), path))
    printfn " Got %s." path
    path

[<EntryPoint>]
let main argv =
    let vsixConfig =
        downloadWebSharperPackage ()
        |> configureVSI
    printf "Generating vsix installer..."
    VSI.BuildVsixFile vsixConfig
    printfn " Created %s." vsixConfig.VsixPath
    0
