<#
.SYNOPSIS
    Builds the Vocaluxe Windows installer(s).

.DESCRIPTION
    For every requested platform the script
      1. downloads the Vocaluxe 2024 theme videos (once, if missing),
      2. removes Output\libs so no outdated libraries get packed (unless -KeepLibs),
      3. rebuilds Vocaluxe.sln in the "InstallerWin" configuration,
      4. compiles Installer\Vocaluxe.iss with Inno Setup.
    The setups are written to "Installer\Setup Files".

    Requirements:
      - Visual Studio 2026 or Build Tools with the .NET 10 SDK and the C++ workload (PitchTracker)
      - Node.js LTS (builds the WebApp)
      - Inno Setup 6.3 or newer (https://jrsoftware.org/isdl.php, or: winget install JRSoftware.InnoSetup)

.PARAMETER Platform
    Platforms to build: x64, x86 or both (default).

.PARAMETER SkipBuild
    Do not build the solution, only pack what is in Output (only one platform allowed).

.PARAMETER SkipThemeVideos
    Do not download the theme background videos.

.PARAMETER KeepLibs
    Do not delete Output\libs before building (saves the FFmpeg/GStreamer download, used by the CI).

.PARAMETER Iscc
    Path to ISCC.exe if Inno Setup is not found automatically.

.EXAMPLE
    .\Installer\build-installer.ps1

.EXAMPLE
    .\Installer\build-installer.ps1 -Platform x64 -SkipBuild
#>
param(
    [ValidateSet('x64', 'x86')]
    [string[]]$Platform = @('x64', 'x86'),
    [switch]$SkipBuild,
    [switch]$SkipThemeVideos,
    [switch]$KeepLibs,
    [string]$Iscc
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'Output'
$solution = Join-Path $root 'Vocaluxe.sln'
$script = Join-Path $PSScriptRoot 'Vocaluxe.iss'

function Find-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $path = & $vswhere -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($path) { return $path }
    }
    $cmd = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    throw 'MSBuild not found. Install Visual Studio 2026 or the Build Tools.'
}

function Find-Iscc {
    if ($Iscc) {
        if (Test-Path $Iscc) { return $Iscc }
        throw "ISCC.exe not found at '$Iscc'."
    }
    $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $local = Join-Path $PSScriptRoot 'ISCC.exe'
    if (Test-Path $local) { return $local }
    # "Inno Setup 6", "Inno Setup 7", ... - take the newest one
    $installed = @(${env:ProgramFiles(x86)}, $env:ProgramFiles, (Join-Path $env:LOCALAPPDATA 'Programs')) |
        Where-Object { $_ -and (Test-Path $_) } |
        ForEach-Object { Get-ChildItem -Path $_ -Directory -Filter 'Inno Setup *' -ErrorAction SilentlyContinue } |
        ForEach-Object { Join-Path $_.FullName 'ISCC.exe' } |
        Where-Object { Test-Path $_ } |
        Sort-Object -Descending |
        Select-Object -First 1
    if ($installed) { return $installed }
    throw 'Inno Setup (ISCC.exe) not found. Install Inno Setup 6.3+ or pass -Iscc <path>.'
}

function Get-ThemeVideos {
    if (Test-Path (Join-Path $output 'Themes\Vocaluxe 2024\BG_Video.mp4')) {
        Write-Output 'Theme videos already present'
        return
    }
    # part1 contains all videos of the Vocaluxe 2024 theme (there is no part2 anymore)
    $url = 'https://github.com/Vocaluxe/VocaluxeDependencies/raw/master/zips/themes/vocaluxe.themes.vocaluxe-2024.video-part1.zip'
    $zip = Join-Path ([System.IO.Path]::GetTempPath()) 'vocaluxe-theme-video.zip'
    Write-Output 'Downloading theme videos'
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    Expand-Archive -Path $zip -DestinationPath $output -Force
    Remove-Item $zip
}

if ($SkipBuild -and $Platform.Count -gt 1) {
    throw '-SkipBuild packs the current Output folder, which only contains one platform. Pass -Platform x64 or -Platform x86.'
}

$isccPath = Find-Iscc
Write-Host "Inno Setup: $isccPath"
if (-not $SkipBuild) {
    $msbuild = Find-MSBuild
    Write-Host "MSBuild:    $msbuild"
}
if (-not $SkipThemeVideos) {
    Get-ThemeVideos
}

foreach ($arch in $Platform) {
    if (-not $SkipBuild) {
        Write-Host ''
        Write-Host "=== Building InstallerWin|$arch ==="
        $libs = Join-Path $output 'libs'
        if (-not $KeepLibs -and (Test-Path $libs)) {
            Remove-Item $libs -Recurse -Force
        }
        & $msbuild $solution /restore /t:Rebuild /m /nologo /verbosity:minimal "/p:Configuration=InstallerWin" "/p:Platform=$arch"
        if ($LASTEXITCODE -ne 0) { throw "Build of InstallerWin|$arch failed (exit code $LASTEXITCODE)." }
    }

    Write-Host ''
    Write-Host "=== Creating $arch setup ==="
    & $isccPath "/DArch=$arch" "/DSourceDir=$output" $script
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed for $arch (exit code $LASTEXITCODE)." }
}

Write-Host ''
Write-Host "Done. Setups: $(Join-Path $PSScriptRoot 'Setup Files')"
