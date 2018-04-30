param(
    [Parameter(Mandatory=$true)]
    [String]$ProjectDir
)

if($Env:VOCALUXE_SKIP_POSTBUILD)
{
    Write-Host "Skipping post build script (as requested)"
    exit
}

(Get-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs") `
    | Foreach-Object {$_ -replace '(?<=AssemblyInformationalVersion\(\").*(?=\")', 'GITVERSION'} `
    | Foreach-Object {$_ -replace '(?<=(AssemblyVersion|AssemblyFileVersion)\(\").*(?=\")', 'SHORTVERSION'} `
    | Foreach-Object {$_ -replace '(?<=AssemblyTitle\(\").*(?=\")', 'FULLVERSION'} `
    | Set-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs"