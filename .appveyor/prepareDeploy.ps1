if($Env:APPVEYOR_REPO_TAG -eq "true") {
	$targetTag = $Env:APPVEYOR_REPO_TAG_NAME
	Write-Host "Deploy for tag $targetTag"	
}
else{
    Write-Host "No tag found: no preparation required"
	Return;
}

$getRelaseInfoParams = @{
	Uri = "https://api.github.com/repos/Vocaluxe/Vocaluxe/releases/tags/$targetTag";
	Method = 'GET';
	Headers = @{
		Authorization = 'Basic ' + [Convert]::ToBase64String(
		[Text.Encoding]::ASCII.GetBytes($Env:GitHubKey + ":x-oauth-basic"));
	}
}
try{
	$result = Invoke-RestMethod @getRelaseInfoParams 
}
catch [System.Net.WebException] 
{
	# No Release found for this tag
    $statusCode = [int]$_.Exception.Response.StatusCode
    $html = $_.Exception.Response.StatusDescription
	Write-Host "No Release found for this tag ($html): $($Env:APPVEYOR_REPO_TAG_NAME)"
	Return;
}

$assetId = ($result.assets | where {$_.name -EQ "Vocaluxe_$($Env:APPVEYOR_REPO_TAG_NAME)_Windows_$($Env:PLATFORM).zip" }  | Select-Object -first 1 ).id

if(!$assetId){
	# No matching asset found in this release
	Write-Host "No matching asset found in this release: Vocaluxe_$($Env:APPVEYOR_REPO_TAG_NAME)_Windows_$($Env:PLATFORM).zip"
	Return;
}

$deleteAssetParams = @{
	Uri = "https://api.github.com/repos/Vocaluxe/Vocaluxe/releases/assets/$assetId";
	Method = 'DELETE';
	Headers = @{
		Authorization = 'Basic ' + [Convert]::ToBase64String(
		[Text.Encoding]::ASCII.GetBytes($Env:GitHubKey + ":x-oauth-basic"));
	}
}
try{
	$result = Invoke-RestMethod @deleteAssetParams 
	Write-Host "Successfully deleted asset: Vocaluxe_$($Env:APPVEYOR_REPO_TAG_NAME)_Windows_$($Env:PLATFORM).zip"
}
catch [System.Net.WebException] 
{
	# Could not delete asset
    $statusCode = [int]$_.Exception.Response.StatusCode
    $html = $_.Exception.Response.StatusDescription
	Write-Host "Could not delete asset ($html): Vocaluxe_$($Env:APPVEYOR_REPO_TAG_NAME)_Windows_$($Env:PLATFORM).zip"
	Return;
}
 