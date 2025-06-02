param(
    [Parameter(Mandatory = $true)]
    [String]$ProjectDir,
    [Parameter(Mandatory = $true)]
    [String]$Arch
)

$Version = "0.0.*";

if ($Env:VOCALUXE_VERSION) {
    $Version = "$Env:VOCALUXE_VERSION";
}

$fullVersionName = "Vocaluxe $Version ($Arch)"

(Get-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs") `
| Foreach-Object { $_ `
        -replace '(?<=AssemblyInformationalVersion\(").*(?=")', $Version `
        -replace '(?<=(AssemblyVersion|AssemblyFileVersion)\(").*(?=")', `
    ($Version) `
        -replace '(?<=AssemblyTitle\(").*(?=")', $fullVersionName `
        -replace '(?<=AssemblyCopyright\(".*)[0-9]+(?=")', (Get-Date).Year } `
| Set-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs"