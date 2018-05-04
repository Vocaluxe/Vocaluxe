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
    | Foreach-Object {$_ -replace '(?<=AssemblyInformationalVersion\(\").*(?=\")', '0.0.0-na-notversioned'} `
    | Foreach-Object {$_ -replace '(?<=(AssemblyVersion|AssemblyFileVersion)\(\").*(?=\")', '0.0.0'} `
    | Foreach-Object {$_ -replace '(?<=AssemblyTitle\(\").*(?=\")', "Vocaluxe 'Not Versioned' 0.0.0 (NA) (0.0.0-na-notversioned)"} `
    | Set-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs"