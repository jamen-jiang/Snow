#requires -version 5.1
<#
.SYNOPSIS
    Pushes every nupkg (then its .snupkg symbols) from the package folder to a NuGet source.
.DESCRIPTION
    - Skips versions already present on the source (--skip-duplicate).
    - Rate limits pushes with a sliding one hour window (nuget.org rejects bursts).
    - Retries on HTTP 4xx with the "retry after" delay, at most three attempts.
    - Writes failed package names to failed-packages.txt and exits with code 1.
.EXAMPLE
    ./build/push.ps1 -ApiKey $env:NUGET_API_KEY
.EXAMPLE
    ./build/push.ps1 -ApiKey xxx -Source https://api.nuget.org/v3/index.json -Packages D:\feed
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ApiKey,

    [string]$Source = "https://api.nuget.org/v3/index.json",
    [string]$Packages = "",
    [int]$MaxPushesPerHour = 250
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($Packages)) {
    # 与 build/pack.ps1 的默认输出目录保持一致
    $Packages = Join-Path $root ".nuget-packages"
}

if (-not (Test-Path $Packages)) {
    throw "Package folder not found: $Packages"
}

# 先 nupkg 再 snupkg: 符号包必须跟在自己的包之后推送
$nupkgs = Get-ChildItem -Path $Packages -Filter "*.nupkg" -File | Sort-Object Name
$snupkgs = Get-ChildItem -Path $Packages -Filter "*.snupkg" -File | Sort-Object Name
$files = @($nupkgs) + @($snupkgs)
if ($files.Count -eq 0) {
    throw "No package file (*.nupkg / *.snupkg) found in $Packages"
}

$failedFile = Join-Path $root "failed-packages.txt"
if (Test-Path $failedFile) {
    Remove-Item $failedFile -Force
}

$pushed = New-Object System.Collections.Generic.List[datetime]
$failed = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
    # sliding window rate limit
    while ($pushed.Count -ge $MaxPushesPerHour) {
        $oldest = $pushed[0]
        $elapsed = ((Get-Date) - $oldest).TotalSeconds
        if ($elapsed -lt 3600) {
            $wait = [int](3600 - $elapsed) + 1
            Write-Host "Rate limit reached, waiting $wait second(s)" -ForegroundColor Yellow
            Start-Sleep -Seconds $wait
        }
        $pushed.RemoveAt(0)
    }

    Write-Host "pushing $($file.Name)"
    $delays = @(30, 60, 120)
    $ok = $false
    $lastOutput = ""

    for ($attempt = 0; $attempt -lt 3; $attempt++) {
        $lastOutput = & dotnet nuget push $file.FullName --skip-duplicate --source $Source --api-key $ApiKey 2>&1 | Out-String
        if ($LASTEXITCODE -eq 0) {
            $ok = $true
            break
        }

        $match = [regex]::Match($lastOutput, "retry after:\s*(\d+)\s*s", "IgnoreCase")
        $delay = if ($match.Success) { [int]$match.Groups[1].Value } else { $delays[$attempt] }
        Write-Host "  attempt $($attempt + 1) failed, retrying in $delay second(s)" -ForegroundColor Yellow
        Start-Sleep -Seconds $delay
    }

    if (-not $ok) {
        Write-Host $lastOutput -ForegroundColor Red
        $failed.Add($file.Name)
    }
    else {
        $pushed.Add((Get-Date))
    }
}

if ($failed.Count -gt 0) {
    $failed | Set-Content -Path $failedFile -Encoding UTF8
    Write-Host "Failed: $($failed -join ', '), see $failedFile" -ForegroundColor Red
    exit 1
}

Write-Host "Pushed $($files.Count) package(s) to $Source" -ForegroundColor Green
