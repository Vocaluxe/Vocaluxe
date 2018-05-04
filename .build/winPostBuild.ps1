param(
    [Parameter(Mandatory=$true)]
    [String]$ProjectDir
)

if($Env:GIT_VERSION_TAG)
{
    $version = $Env:GIT_VERSION_TAG;
}
else
{
    $version = git describe --long;
}

(Get-Content -Encoding UTF8 "$($ProjectDir)..\Output\Website\js\main.js") `
    | Foreach-Object {$_ -replace '(?<=var *matchingServerVersion * = *\")\w*(?=\" *;)', "$version"} `
    | Set-Content -Encoding UTF8 "$($ProjectDir)..\Output\Website\js\main.js"

(Get-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs") `
    | Foreach-Object {$_ -replace '(?<=AssemblyInformationalVersion\(\").*(?=\")', '0.0.0-na-notversioned'} `
    | Foreach-Object {$_ -replace '(?<=(AssemblyVersion|AssemblyFileVersion)\(\").*(?=\")', '0.0.0'} `
    | Foreach-Object {$_ -replace '(?<=AssemblyTitle\(\").*(?=\")', 'Vocaluxe `'Not Versioned`' 0.0.0 (NA) (0.0.0-na-notversioned)'} `
    | Set-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs"