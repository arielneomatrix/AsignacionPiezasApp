param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$proj = Join-Path $PSScriptRoot "..\AsignacionPiezasApp_Iter2.csproj"
dotnet restore $proj
dotnet publish $proj -c $Configuration -p:PublishProfile=..\Properties\PublishProfiles\FolderProfile.pubxml
Write-Host "Publicación lista en:" (Join-Path $PSScriptRoot "..\publish\win-x64")