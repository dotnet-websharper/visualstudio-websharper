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

    let getExtensionName () =
        "WebSharper"

    let getExtensionGuid () =
        Guid("371cf828-9e17-41cb-b014-496f3e9e7171")

    let getExtensionDecription () =
        "F#-to-JavaScript compiler and web application framework"

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

    let getIdentity () =
        VST.ExtensionIdentity.Create(getExtensionName (), getExtensionGuid ())

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

    let makeTemplateMetadata com name dpn desc =
        VST.TemplateData.Create(VST.ProjectType.FSharp,
            name = name,
            description = desc,
            icon = com.Icon)
            .WithDefaultProjectName(dpn)

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
        let meta = makeTemplateMetadata com def.Name def.DefaultProjectName def.Description
        let project =
            VST.Project.FromFile(dir +/ def.ProjectFile, def.Files file folder)
                .ReplaceParameters()
        let identity = getIdentity ()
        let extraPkgs =
            def.ExtraNuGetPackages
            |> List.map (fun x -> readNugetPackage (Map.find x com.Config.ExtraNuPkgPaths))
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
            ExtraNuGetPackages = []
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
            ExtraNuGetPackages = []
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
                    file "Main.fs"
                    file "Web.config"
                    file "Global.asax"
                    file "index.html"
                ]
            ExtraNuGetPackages = []
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
                    file "Main.html"
                    file "Setup.fsx"
                ]
            ExtraNuGetPackages = []
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
            ExtraNuGetPackages = []
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
            ExtraNuGetPackages = []
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
                [
                    "Microsoft.Owin"
                    "Microsoft.Owin.Diagnostics"
                    "Microsoft.Owin.FileSystems"
                    "Microsoft.Owin.Host.HttpListener"
                    "Microsoft.Owin.Hosting"
                    "Microsoft.Owin.StaticFiles"
                    "Mono.Cecil"
                    "Owin"
                    "WebSharper.Owin"
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
                    file "Main.fs"
                    file "Web.config"
                    file "Global.asax"
                    file "index.html"
                ]
            ExtraNuGetPackages = ["WebSharper.UI.Next"]
        }

    let getWebSharperExtension com =
        let desc = getExtensionDecription ()
        let editions =
            [
                VX.VSEdition.Premium
                VX.VSEdition.Pro
                VX.VSEdition.Ultimate
                VX.VSEdition.VWDExpress
            ]
        let products =
            [
                for v in ["10.0"; "11.0"; "12.0"; "14.0"] do
                    yield VX.VSProduct.Create(v, editions).AsSupportedProduct()
            ]
        let identifier =
            VX.Identifier.Create("IntelliFactory", getIdentity (), com.VersionInfo.PackageId, desc)
                .WithVersion(com.VersionInfo.NumericVersion)
                .WithProducts(products)
                .WithLicense(File.ReadAllText(Path.Combine(com.Config.RootPath, "LICENSE.md")))
        let category = ["WebSharper"]
        let proj x = VX.VsixContent.ProjectTemplate(category, x)
        let vsix =
            VX.Vsix.Create(identifier,
                [
                    libraryTemplate
                    extensionTemplate
                    bundleSiteTemplate
                    siteletsWebsiteTemplate
                    siteletsHtmlTemplate
                    siteletsHostTemplate
                    owinSelfHostTemplate
                    bundleUINextSiteTemplate
                ]
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
