@ECHO OFF
REM NOTE: This file was auto-generated with `IB.exe prepare` from `IntelliFactory.Build`.

setlocal
set PATH=%PATH%;tools\NuGet
rd /s /q packages
rd /s /q build
nuget install IntelliFactory.Build -nocache -pre -ExcludeVersion -o tools\packages
nuget install WebSharper.Suave -nocache -pre -o packages
nuget install FSharp.Compiler.Tools -nocache -version 4.0.1.21 -excludeVersion -o tools/packages
tools\packages\FSharp.Compiler.Tools\tools\fsi.exe --exec build.fsx %*

build\net45\WebSharper.VisualStudio.exe
