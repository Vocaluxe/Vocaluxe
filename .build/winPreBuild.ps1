param(
    [Parameter(Mandatory=$true)]
    [String]$ProjectDir,
    [Parameter(Mandatory=$true)]
    [String]$Arch,
    [Parameter(Mandatory=$true)]
    [String]$Version
)
$commitHash = $Env:GITHUB_SHA;

$fullVersionName = "Vocaluxe $Version ($Arch)"

(Get-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs") `
    | Foreach-Object {$_ `
        -replace '(?<=AssemblyInformationalVersion\(").*(?=")', $Version `
        -replace '(?<=(AssemblyVersion|AssemblyFileVersion)\(").*(?=")', `
            ($Version) `
        -replace '(?<=AssemblyTitle\(").*(?=")', $fullVersionName `
        -replace '(?<=AssemblyCopyright\(".*)[0-9]+(?=")', (Get-Date).Year} `
    | Set-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs"