#requires -version 5.1
<#
.SYNOPSIS
    Packs every Snow.* project into nupkg files.
.DESCRIPTION
    Packs each project one by one: dotnet pack accepts a single project per call.
    Output goes to <repo>/.nuget-packages, previous package files there are removed first.
.EXAMPLE
    ./build/pack.ps1
.EXAMPLE
    ./build/pack.ps1 -Version 1.2.0 -Output D:\feed
.NOTES
    If the script is blocked by the execution policy, run it as:
      powershell -ExecutionPolicy Bypass -File .\build\pack.ps1
#>
param(
    [string]$Configuration = "Release",
    [string]$Version = "",
    [string]$Output = ""
)

$ErrorActionPreference = "Stop"

# 让控制台按 UTF-8 输出, 避免中文路径乱码
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $OutputEncoding = [System.Text.Encoding]::UTF8
} catch {
    # 某些宿主不允许改编码, 忽略即可
}

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $root ".nuget-packages"
}

if (Test-Path $Output) {
    # 符号包也一起清掉, 否则旧版本的 snupkg 会留在目录里
    # 注意 -Include 只对带通配符的路径生效, 直接给目录要写成 Join-Path $Output "*"
    Get-ChildItem -Path (Join-Path $Output "*") -Include "*.nupkg", "*.snupkg" -File | Remove-Item -Force
}
else {
    New-Item -ItemType Directory -Path $Output -Force | Out-Null
}

# Only the framework projects at the repository root are published;
# test and sample projects live outside this pattern.
$projects = Get-ChildItem -Path $root -Directory -Filter "Snow*" |
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
