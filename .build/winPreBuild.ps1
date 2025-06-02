param(
    [Parameter(Mandatory = $true)]
    [String]$ProjectDir,
    [Parameter(Mandatory = $true)]
    [String]$Arch
)

# Calculate version components as ever-increasing uint16 values
$date = (Get-Date).ToUniversalTime()
# Use days since January 1, 2000 for the first version component
$baseDate = (Get-Date "2000-01-01").ToUniversalTime()
$versionDays = [uint16](($date - $baseDate).Days)
# Use seconds since midnight divided by 2 for the second version component (86400/2 < 65535)
$versionSeconds = [uint16]([math]::Floor($date.TimeOfDay.TotalSeconds / 2))
$Version = "0.0.$versionDays.$versionSeconds"

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