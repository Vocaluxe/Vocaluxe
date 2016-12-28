param(
    [Parameter(Mandatory=$true)]
    [String]$ProjectDir,
    [Parameter(Mandatory=$true)]
    [String]$Arch
)

if($Env:GIT_VERSION_TAG)
{
    $version = $Env:GIT_VERSION_TAG;
}
else
{
    $version = git describe --long;
}

$shortVersion = $version.Split('-')[0];

[System.Reflection.Assembly]::LoadWithPartialName("System.Web.Extensions")
$versionName = (New-Object System.Web.Script.Serialization.JavaScriptSerializer).Deserialize((Get-Content "$($ProjectDir)..\.build\versionNames.json"), `
    [System.Collections.Hashtable])[([regex]"[0-9]+\.[0-9]+\.").Match($shortVersion).Value]

$releaseType = switch -wildcard ($shortVersion.Split('.')[2]) 
{ 
    "alpha*" {"Alpha"} 
    "beta*" {"Beta"} 
    "rc*" {"RC"}         
    default {"Release"}
};

$fullVersionName = "Vocaluxe $(if($versionName) {"'"+$versionName+"' "} else {''})$shortVersion ($Arch) $(if($releaseType -ne "Release") {'$releaseType '} else {''})($version)"

(Get-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs") `
    | Foreach-Object {$_ `
        -replace '(?<=AssemblyInformationalVersion\(").*(?=")', $version `
        -replace '(?<=(AssemblyVersion|AssemblyFileVersion)\(").*(?=")', `
            ($version.Split('-')[0].replace('alpha','0').replace('beta','1').replace('rc','2') -replace "[a-zA-Z]+","")`
        -replace '(?<=AssemblyTitle\(").*(?=")', $fullVersionName `
        -replace '(?<=AssemblyCopyright\(".*)[0-9]+(?=")', (Get-Date).Year} `
    | Set-Content -Encoding UTF8 "$($ProjectDir)Properties\AssemblyInfo.cs"