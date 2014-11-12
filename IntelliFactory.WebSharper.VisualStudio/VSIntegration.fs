﻿// Copyright 2013 IntelliFactory
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

namespace IntelliFactory.WebSharper.VisualStudio

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
            RootPath : string
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

    let makeProjectTemplate com meta project =
        let identity = getIdentity ()
        let c = Content.ReadBinaryFile(com.Config.NuPkgPath)
        let vn = com.VersionInfo
        let pkg = NG.Package.Create(vn.PackageId, vn.FullVersion, c)
        let nuGet = VST.NuGetPackages.Create(identity, [pkg])
        VST.ProjectTemplate.Create(meta, project)
            .WithNuGetPackages(nuGet)

    let makeTemplateMetadata com name dpn desc =
        VST.TemplateData.Create(VST.ProjectType.FSharp,
            name = name,
            description = desc,
            icon = com.Icon)
            .WithDefaultProjectName(dpn)

    let getLibraryTemplate com =
        let dir = com.Config.RootPath +/ "templates" +/ "library"
        let meta =
            "Creates an F# library capable of containing WebSharper-compiled code."
            |> makeTemplateMetadata com "Library" "Library"
        let main = VST.ProjectItem.FromTextFile(dir +/ "Main.fs").ReplaceParameters()
        let project =
            VST.Project.FromFile(dir +/ "Library.fsproj",
                [VST.FolderElement.Nested main])
                .ReplaceParameters()
        makeProjectTemplate com meta project

    let getExtensionTempalte com =
        let dir = com.Config.RootPath +/ "templates" +/ "extension"
        let meta =
            "Creates a new WebSharper extension to existing JavaScript code using \
                the WebSharper Interface Generator (WIG) tool."
            |> makeTemplateMetadata com "Extension" "Extension"
        let main = VST.ProjectItem.FromTextFile(dir +/ "Main.fs").ReplaceParameters()
        let project =
            VST.Project.FromFile(dir +/ "Extension.fsproj",
                [VST.FolderElement.Nested(main)])
                .ReplaceParameters()
        makeProjectTemplate com meta project

    let getSiteletsWebsiteTemplate com =
        let dir = com.Config.RootPath +/ "templates" +/ "sitelets-website"
        let meta =
            "Creates a starter client-server web application based on sitelets."
            |> makeTemplateMetadata com "Client-Server Web Application" "Application"
        let file name =
            let i = VST.ProjectItem.FromTextFile(dir +/ name).ReplaceParameters()
            VST.FolderElement.Nested(i)
        let project =
            VST.Project.FromFile(dir +/ "Application.fsproj",
                [
                    file "Remoting.fs"
                    file "Client.fs"
                    file "Main.fs"
                    file "Web.config"
                    file "Global.asax"
                    file "Main.html"
                    file "Setup.fsx"
                ])
                .ReplaceParameters()
        makeProjectTemplate com meta project

    let getBundleSiteTemplate com =
        let dir = com.Config.RootPath +/ "templates" +/ "bundle-website"
        let meta =
            "Creates an empty single-page HTML application."
            |> makeTemplateMetadata com "Single-Page Application" "SinglePageApplication"
        let file name =
            let i = VST.ProjectItem.FromTextFile(dir +/ name).ReplaceParameters()
            VST.FolderElement.Nested(i)
        let project =
            VST.Project.FromFile(dir +/ "SinglePageApplication.fsproj",
                [
                    file "Client.fs"
                    file "Main.fs"
                    file "Web.config"
                    file "Global.asax"
                    file "index.html"
                ])
                .ReplaceParameters()
        makeProjectTemplate com meta project

    let getSiteletsHtmlTemplate com =
        let dir = com.Config.RootPath +/ "templates" +/ "sitelets-html"
        let meta =
            "Creates a starter sitelet-based HTML application."
            |> makeTemplateMetadata com "HTML Application" "HtmlApplication"
        let file repl name =
            let i = VST.ProjectItem.FromTextFile(dir +/ name).ReplaceParameters()
            VST.FolderElement.Nested(i)
        let project =
            VST.Project.FromFile(dir +/ "HtmlApplication.fsproj",
                [
                    file true "Client.fs"
                    file true "Main.fs"
                    file false "extra.files"
                    file false "Main.html"
                ])
                .ReplaceParameters()
        makeProjectTemplate com meta project

    let getSiteletsHostTemplate com =
        let dir = com.Config.RootPath +/ "templates" +/ "sitelets-host"
        let meta =
            "Creates a C#-based web project for hosting WebSharper sitelets in a web server."
            |> makeTemplateMetadata com "ASP.NET Container" "Web"
        let file name =
            VST.ProjectItem.FromTextFile(dir +/ name).ReplaceParameters()
            |> VST.FolderElement.Nested
        let folder name xs =
            let f = VST.Folder.Create(name, xs)
            VST.FolderElement.Folder(f)
        let project =
            VST.Project.FromFile(dir +/ "Web.csproj",
                [
                    folder "Properties" [
                        file "Properties/AssemblyInfo.cs"
                    ]
                    file "Main.html"
                    file "Web.config"
                ])
                .ReplaceParameters()
        makeProjectTemplate com meta project

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
                for v in ["10.0"; "11.0"; "12.0"] do
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
                    proj (getLibraryTemplate com)
                    proj (getExtensionTempalte com)
                    proj (getBundleSiteTemplate com)
                    proj (getSiteletsWebsiteTemplate com)
                    proj (getSiteletsHtmlTemplate com)
                    proj (getSiteletsHostTemplate com)
                ])
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
