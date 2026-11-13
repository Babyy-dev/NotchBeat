# Builds NotchBeat.exe into .\publish
#
#   .\build.ps1                 Small exe; needs the .NET 8 Desktop Runtime installed.
#   .\build.ps1 -SelfContained  Larger exe that runs on any Windows 10/11 PC as is.
param(
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

dotnet publish NotchBeat\NotchBeat.csproj `
    -c Release `
    -r win-x64 `
    --self-contained:$($SelfContained.IsPresent) `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish

Write-Host "Built $PSScriptRoot\publish\NotchBeat.exe"
