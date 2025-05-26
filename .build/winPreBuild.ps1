param(
    [Parameter(Mandatory=$true)]
    [String]$ProjectDir,
    [Parameter(Mandatory=$true)]
    [String]$Arch
)
$commitHash = $Env:GITHUB_SHA;

$shortVersion = "0.0.0"
$unixTime = [DateTimeOffset]::Now.ToUnixTimeSeconds()
$version = "$($shortVersion).$($unixTime)"

$fullVersionName = "Vocaluxe $version ($Arch)"

(Get-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs") `
    | Foreach-Object {$_ `
        -replace '(?<=AssemblyInformationalVersion\(").*(?=")', $version `
        -replace '(?<=(AssemblyVersion|AssemblyFileVersion)\(").*(?=")', `
            ($version) `
        -replace '(?<=AssemblyTitle\(").*(?=")', $fullVersionName `
        -replace '(?<=AssemblyCopyright\(".*)[0-9]+(?=")', (Get-Date).Year} `
    | Set-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs"