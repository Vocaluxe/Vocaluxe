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
    | Foreach-Object {$_ -replace '(?<=AssemblyInformationalVersion\(\").*(?=\")', 'GITVERSION'} `
    | Foreach-Object {$_ -replace '(?<=(AssemblyVersion|AssemblyFileVersion)\(\").*(?=\")', 'SHORTVERSION'} `
    | Foreach-Object {$_ -replace '(?<=AssemblyTitle\(\").*(?=\")', 'FULLVERSION'} `
    | Set-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs"