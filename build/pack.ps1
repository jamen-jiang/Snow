#requires -version 5.1
<#
.SYNOPSIS
    Packs every Rongia.* project into nupkg files.
.DESCRIPTION
    Packs each project one by one: dotnet pack accepts a single project per call.
    Output goes to <repo>/nupkg, previous nupkg files in that folder are removed first.
.EXAMPLE
    ./build/pack.ps1
.EXAMPLE
    ./build/pack.ps1 -Version 1.2.0 -Output D:\feed
#>
param(
    [string]$Configuration = "Release",
    [string]$Version = "",
    [string]$Output = ""
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $root "nupkg"
}

if (Test-Path $Output) {
    Get-ChildItem -Path $Output -Filter "*.nupkg" -File | Remove-Item -Force
}
else {
    New-Item -ItemType Directory -Path $Output -Force | Out-Null
}

# Only the framework projects at the repository root are published;
# test and sample projects live outside this pattern.
$projects = Get-ChildItem -Path $root -Directory -Filter "Rongia*" |
    Where-Object { Test-Path (Join-Path $_.FullName ($_.Name + ".csproj")) } |
    Sort-Object Name

if ($projects.Count -eq 0) {
    throw "No packable project found under $root"
}

Write-Host "Packing $($projects.Count) project(s) into $Output" -ForegroundColor Cyan

$failed = @()
foreach ($project in $projects) {
    $projectFile = Join-Path $project.FullName ($project.Name + ".csproj")
    Write-Host "  packing $($project.Name)"

    $arguments = @("pack", $projectFile, "-c", $Configuration, "-o", $Output, "--nologo")
    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        $arguments += "-p:Version=$Version"
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        $failed += $project.Name
    }
}

Write-Host ""
Get-ChildItem -Path $Output -Filter "*.nupkg" -File |
    Sort-Object Name |
    Format-Table Name, @{ Name = "KB"; Expression = { [math]::Round($_.Length / 1KB, 1) } } -AutoSize

if ($failed.Count -gt 0) {
    Write-Host "Pack failed for: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}

Write-Host "Done." -ForegroundColor Green
