// Copyright 2013 IntelliFactory
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.

namespace WebSharper.VisualStudio

/// Defines VisualStudio templates for WebSharper.
module VSIntegration =
    open System
    open System.IO
    open System.Text.RegularExpressions
    module X = IntelliFactory.Core.XmlTools
    module NG = NuGet
    module VST = Templates
    module VX = Extensions
    type Content = Utils.Content

#if ZAFIR
    let wsName, fsharpTools =
        "Zafir", ["Zafir.FSharp"]

    let getExtensionId isCSharp =
        "Zafir." + if isCSharp then "CSharp" else "FSharp" 

    let getExtensionName isCSharp =
        "WebSharper for " + (if isCSharp then "C#" else "F#") + " (Zafir)"

    let getExtensionGuid isCSharp =
        if isCSharp then
            Guid("ade77674-5840-4766-8a37-c7228d459ab1")
        else
            Guid("99c562e0-fcfc-4b8c-bcea-976bd67d0275")

    let getExtensionDecription isCSharp =
        if isCSharp then "C#" else "F#"
        + "-to-JavaScript compiler and web application framework"
#else
    let wsName, fsharpTools =
        "WebSharper", []

    let getExtensionId _ =
        "WebSharper"

    let getExtensionName _ =
        "WebSharper"

    let getExtensionGuid _ =
        Guid("371cf828-9e17-41cb-b014-496f3e9e7171")

    let getExtensionDecription _ =
        "F#-to-JavaScript compiler and web application framework"
#endif

    let pattern = Regex(@"(\d+(\.\d+)*)(\-(\w+))?$")

    type VersionInfo =
        {
            FullVersion : string
            NumericVersion : Version
            PackageId : string
            VersionSuffix : option<string>
        }

        static member FromFileName(package: string) =
            let fn = Path.GetFileNameWithoutExtension(package)
            let m = pattern.Match(fn)
            if m.Success then
                Some {
                    FullVersion = m.Groups.[0].Value
                    NumericVersion = Version(m.Groups.[1].Value)
                    PackageId = fn.Substring(0, m.Groups.[1].Index - 1)
                    VersionSuffix =
                        match m.Groups.[4].Value with
                        | "" -> None
                        | v -> Some v
                }
            else None

    type Config =
        {
            NuPkgPath : string
            ExtraNuPkgPaths : Map<string, string>
            RootPath : string
            TemplatesPath : string
            VsixPath : string
            IsCSharp : bool
        }

    let ( +/ ) a b =
        Path.Combine(a, b)

    type Common =
        {
            Config : Config
            Icon : VST.Icon
            VersionInfo : VersionInfo
        }

        static member Create(cfg) =
            let iconPath = cfg.RootPath +/ "tools" +/ "WebSharper.png"
            let icon = VST.Icon.FromFile(iconPath)
            {
                Config = cfg
                Icon = icon
                VersionInfo = VersionInfo.FromFileName(cfg.NuPkgPath).Value
            }

    let getIdentity isCSharp =
        VST.ExtensionIdentity.Create(getExtensionId isCSharp, getExtensionGuid isCSharp)

    type TemplateDef =
        {
            PathName : string
            Name : string
            DefaultProjectName : string
            Description : string
            ProjectFile : string
            Files :
                (string -> Templates.FolderElement)
                -> (string -> Templates.FolderElement list -> Templates.FolderElement)
                -> Templates.FolderElement list
            ExtraNuGetPackages : string list
        }

    let makeTemplateMetadata com def =
        VST.TemplateData.Create(
#if ZAFIR
            (if def.ProjectFile.EndsWith ".fsproj" then VST.ProjectType.FSharp else VST.ProjectType.CSharp),
#else
            VST.ProjectType.FSharp,
#endif
            name = def.Name,
            description = def.Description,
            icon = com.Icon)
            .WithDefaultProjectName(def.DefaultProjectName)

    let readNugetPackage path =
        let c = Content.ReadBinaryFile(path)
        let vn = VersionInfo.FromFileName(path).Value
        NG.Package.Create(vn.PackageId, vn.FullVersion, c)

    let makeProjectTemplate com def =
        let dir = com.Config.TemplatesPath +/ def.PathName
        let file name =
            VST.ProjectItem.FromTextFile(dir +/ name).ReplaceParameters()
            |> VST.FolderElement.Nested
        let folder name xs =
            VST.Folder.Create(name, xs)
            |> VST.FolderElement.Folder
        let meta = makeTemplateMetadata com def
        let project =
            VST.Project.FromFile(dir +/ def.ProjectFile, def.Files file folder)
                .ReplaceParameters()
        let identity = getIdentity com.Config.IsCSharp
        let findNugetPackage x =
            match Map.tryFind x com.Config.ExtraNuPkgPaths with
            | Some p -> readNugetPackage p
            | _ -> failwithf "Cannot find NuGet package for template '%s': %s" def.Name x
        let extraPkgs = def.ExtraNuGetPackages |> List.map findNugetPackage
        let pkgs = readNugetPackage com.Config.NuPkgPath :: extraPkgs
        let nuGet = VST.NuGetPackages.Create(identity, pkgs)
        VST.ProjectTemplate.Create(meta, project)
            .WithNuGetPackages(nuGet)

    let libraryTemplate =
        {
            Name = "Library"
            PathName = "library"
            DefaultProjectName = "Library"
            Description =
                "Creates an F# library capable of containing WebSharper-compiled code."
            ProjectFile = "Library.fsproj"
            Files = fun file folder -> [file "Main.fs"]
            ExtraNuGetPackages = fsharpTools 
        }

    let extensionTemplate =
        {
            Name = "Extension"
            PathName = "extension"
            DefaultProjectName = "Extension"
            Description =
                "Creates a new WebSharper extension to existing JavaScript code using \
                    the WebSharper Interface Generator (WIG) tool."
            ProjectFile = "Extension.fsproj"
            Files = fun file folder -> [file "Main.fs"]
            ExtraNuGetPackages = fsharpTools
        }

    let bundleSiteTemplate =
        {
            Name = "Single-Page Application"
            PathName = "bundle-website"
            DefaultProjectName = "SinglePageApplication"
            Description =
                "Creates an empty single-page HTML application."
            ProjectFile = "SinglePageApplication.fsproj"
            Files = fun file folder ->
                [
                    file "Client.fs"
                    file "Web.config"
                    file "Global.asax"
                    file "Global.asax.fs"
                    file "index.html"
                    file "Setup.fsx"
                ]
            ExtraNuGetPackages = fsharpTools @ [wsName + ".Html"; "IntelliFactory.Xml"]
        }

    let siteletsWebsiteTemplate =
        {
            Name = "Client-Server Web Application"
            PathName = "sitelets-website"
            DefaultProjectName = "Application"
            Description =
                "Creates a starter client-server web application based on sitelets."
            ProjectFile = "Application.fsproj"
            Files = fun file folder ->
                [
                    file "Remoting.fs"
                    file "Client.fs"
                    file "Main.fs"
                    file "Web.config"
                    file "Global.asax"
                    file "Global.asax.fs"
                    file "Main.html"
                    file "Setup.fsx"
                ]
            ExtraNuGetPackages = fsharpTools @ [wsName + ".Html"; "IntelliFactory.Xml"]
        }

    let siteletsHtmlTemplate =
        {
            Name = "HTML Application"
            PathName = "sitelets-html"
            DefaultProjectName = "HtmlApplication"
            Description =
                "Creates a starter sitelet-based HTML application."
            ProjectFile = "HtmlApplication.fsproj"
            Files = fun file folder ->
                [
                    file "Client.fs"
                    file "Main.fs"
                    file "extra.files"
                    file "Main.html"
                ]
            ExtraNuGetPackages = fsharpTools @ [wsName + ".Html"; "IntelliFactory.Xml"]
        }

    let siteletsHostTemplate =
        {
            Name = "ASP.NET Container"
            PathName = "sitelets-host"
            DefaultProjectName = "Web"
            Description =
               "Creates a C#-based web project for hosting WebSharper sitelets in a web server."
            ProjectFile = "Web.csproj"
            Files = fun file folder ->
                [
                    folder "Properties" [
                        file "Properties/AssemblyInfo.cs"
                    ]
                    file "Main.html"
                    file "Web.config"
                ]
#if ZAFIR
            ExtraNuGetPackages = ["Zafir.CSharp"]
#else
            ExtraNuGetPackages = fsharpTools
#endif
        }

    let owinSelfHostTemplate =
        {
            Name = "Self-Hosted Client-Server Application"
            PathName = "owin-selfhost"
            DefaultProjectName = "Application"
            Description =
                "Creates a starter client-server web application based on sitelets, \
                running as a dedicated executable using an OWIN host."
            ProjectFile = "SelfHostApplication.fsproj"
            Files = fun file folder ->
                [
                    file "Remoting.fs"
                    file "Client.fs"
                    file "Main.fs"
                    file "App.config"
                    file "Main.html"
                ]
            ExtraNuGetPackages =
                fsharpTools @ [
                    "Microsoft.Owin"
                    "Microsoft.Owin.Diagnostics"
                    "Microsoft.Owin.FileSystems"
                    "Microsoft.Owin.Host.HttpListener"
                    "Microsoft.Owin.Hosting"
                    "Microsoft.Owin.StaticFiles"
                    "Mono.Cecil"
                    "Owin"
                    wsName + ".Owin"
                    wsName + ".Html"
                    "IntelliFactory.Xml"
                ]
        }

    let bundleUINextSiteTemplate =
        {
            Name = "UI.Next Single-Page Application"
            PathName = "bundle-uinext"
            DefaultProjectName = "UINextApplication"
            Description =
                "Creates a single-page HTML application using WebSharper UI.Next."
            ProjectFile = "UINextApplication.fsproj"
            Files = fun file folder ->
                [
                    file "Client.fs"
                    file "Web.config"
                    file "Global.asax"
                    file "Global.asax.fs"
                    file "index.html"
                    file "Setup.fsx"
                ]
            ExtraNuGetPackages = fsharpTools @ [wsName + ".UI.Next"]
        }

    let siteletsUINextTemplate =
        {
            Name = "UI.Next Client-Server Application"
            PathName = "sitelets-uinext"
            DefaultProjectName = "Application"
            Description =
                "Creates a starter client-server application based on sitelets and UI.Next."
            ProjectFile = "UI.Next.Application.fsproj"
            Files = fun file folder ->
                [
                    file "Remoting.fs"
                    file "Client.fs"
                    file "Main.fs"
                    file "Web.config"
                    file "Global.asax"
                    file "Global.asax.fs"
                    file "Main.html"
                    file "Setup.fsx"
                ]
            ExtraNuGetPackages = fsharpTools @ [wsName + ".UI.Next"]
        }

    let siteletsUINextSuaveTemplate =
        {
            Name = "UI.Next Client-Server Application with Suave"
            PathName = "sitelets-uinext-suave"
            DefaultProjectName = "Application"
            Description =
                "Creates a starter client-server application based on sitelets and UI.Next running on Suave."
            ProjectFile = "UI.Next.Application.Suave.fsproj"
            Files = fun file folder ->
                [
                    file "Remoting.fs"
                    file "Client.fs"
                    file "Main.fs"
                    file "Main.html"
                    file "App.config"
                ]
            ExtraNuGetPackages =
                fsharpTools @ [
                    "Mono.Cecil"
                    wsName + ".UI.Next"
                    wsName + ".Suave"
                    "Suave"
                    wsName + ".Owin"
                    "Owin"
                    "Microsoft.Owin"
                ]
        }

#if ZAFIR
    let libraryCSharpTemplate =
        {
            Name = "Library"
            PathName = "library-csharp"
            DefaultProjectName = "Library"
            Description =
                "Creates a C# library capable of containing WebSharper-compiled code."
            ProjectFile = "Library.csproj"
            Files = fun file folder ->
                [
                    file "Class1.cs"
                ]
            ExtraNuGetPackages = ["Zafir.CSharp"]
        }

    let bundleSiteCSharpTemplate =
        {
            Name = "Single-Page Application"
            PathName = "bundle-website-csharp"
            DefaultProjectName = "SinglePageApplication"
            Description =
                "Creates an empty single-page HTML application."
            ProjectFile = "SinglePageApplication.csproj"
            Files = fun file folder ->
                [
                    file "Client.cs"
                    file "Web.config"
                    file "index.html"
                ]
            ExtraNuGetPackages = ["Zafir.CSharp"; "Zafir.Html"]
        }

    let bundleUINextSiteCSharpTemplate =
        {
            Name = "UI.Next Single-Page Application"
            PathName = "bundle-uinext-csharp"
            DefaultProjectName = "UINextApplication"
            Description =
                "Creates a single-page HTML application using WebSharper UI.Next."
            ProjectFile = "UINextApplication.csproj"
            Files = fun file folder ->
                [
                    file "Client.cs"
                    file "Web.config"
                    file "index.html"
                ]
            ExtraNuGetPackages = ["Zafir.CSharp"; "Zafir.UI.Next"]
        }

    let bundleUINextSiteCSharpTemplTemplate =
        {
            Name = "UI.Next Single-Page Application With Templating"
            PathName = "bundle-uinext-csharp-templ"
            DefaultProjectName = "UINextApplication"
            Description =
                "Creates a single-page HTML application using WebSharper UI.Next using template code generation."
            ProjectFile = "UINextApplication.csproj"
            Files = fun file folder ->
                [
                    file "Client.cs"
                    file "Web.config"
                    file "index.html"
                    file "index.tt"
                ]
            ExtraNuGetPackages = ["Zafir.CSharp"; "Zafir.UI.Next"]
        }

    let siteletUINextSiteCSharpTemplate =
        {
            Name = "UI.Next Client-Server Application"
            PathName = "sitelets-uinext-csharp"
            DefaultProjectName = "UINextApplication"
            Description =
                "Creates a starter client-server application based on sitelets and UI.Next."
            ProjectFile = "UINextApplication.csproj"
            Files = fun file folder ->
                [
                    file "Client.cs"
                    file "Remoting.cs"
                    file "Server.cs"
                    file "Web.Debug.config"
                    file "Web.Release.config"
                    file "Web.config"
                ]
            ExtraNuGetPackages = ["Zafir.CSharp"; "Zafir.UI.Next"]
        }
#endif

    let getWebSharperExtension com =
        let isCSharp = com.Config.IsCSharp
        let desc = getExtensionDecription isCSharp
        let name = getExtensionName isCSharp
        let editions =
            [
                VX.VSEdition.Premium
                VX.VSEdition.Pro
                VX.VSEdition.Enterprise
                VX.VSEdition.Ultimate
                VX.VSEdition.VWDExpress
            ]
        let products =
            [
                for v in ["10.0"; "11.0"; "12.0"; "14.0"; "15.0"] do
                    yield VX.VSProduct.Create(v, editions).AsSupportedProduct()
            ]
        let identifier =
            VX.Identifier.Create("IntelliFactory", getIdentity isCSharp, name, desc)
                .WithVersion(com.VersionInfo.NumericVersion)
                .WithProducts(products)
                .WithLicense(File.ReadAllText(Path.Combine(com.Config.RootPath, "LICENSE.md")))
        let category = [wsName]
        let proj x = VX.VsixContent.ProjectTemplate(category, x)
        let vsix =
            VX.Vsix.Create(identifier,
#if ZAFIR
                (
                    if isCSharp then
                        [
                            siteletsHostTemplate
                            libraryCSharpTemplate
                            bundleSiteCSharpTemplate
                            bundleUINextSiteCSharpTemplate
                            bundleUINextSiteCSharpTemplTemplate
                            siteletUINextSiteCSharpTemplate
                        ]
                    else
                        [
                            libraryTemplate
                            extensionTemplate
                            bundleSiteTemplate
                            siteletsWebsiteTemplate
                            siteletsHtmlTemplate
                            owinSelfHostTemplate
                            bundleUINextSiteTemplate
                            siteletsUINextTemplate
                            siteletsUINextSuaveTemplate
                        ]
                )
#else
                [
                    libraryTemplate
                    extensionTemplate
                    bundleSiteTemplate
                    siteletsWebsiteTemplate
                    siteletsHtmlTemplate
                    siteletsHostTemplate
                    owinSelfHostTemplate
                    bundleUINextSiteTemplate
                    siteletsUINextTemplate
                    siteletsUINextSuaveTemplate
                ]
#endif
                |> List.map (makeProjectTemplate com >> proj)
            )
        VX.VsixFile.Create(Path.GetFileName(com.Config.VsixPath), vsix)

    let BuildVsixFile cfg =
        let com = Common.Create(cfg)
        let ext = getWebSharperExtension com
        ext.WriteToDirectory(Path.GetDirectoryName(cfg.VsixPath))

    let BuildContents cfg =
        seq {
            let sourcePath = cfg.RootPath +/ "msbuild" +/ "WebSharper.targets"
            let targetPath = "build/WebSharper.targets"
            yield IntelliFactory.Build.NuGetFile.Local(sourcePath, targetPath)
        }
