if($Env:APPVEYOR_REPO_TAG -eq "true") {
	$targetTag = $Env:APPVEYOR_REPO_TAG_NAME
	Write-Host "Deploy for tag $targetTag"	
}
else{
    Write-Host "No tag found: no preparation required"
	Return;
}

$Env:IS_BETA_RELEASE = ($Env:APPVEYOR_REPO_TAG_NAME -match "beta")

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
	Write-Host "No Release found for this tag ($html): $targetTag"
	Return;
}

$releaseId = $result.id

if(!$releaseId){
	# No matching release found in this release
	Write-Host "No matching release found"
	Return;
}

if($result.body -match $Env:APPVEYOR_REPO_COMMIT){
    # An other build already recreated the release
	Write-Host "An other build already recreated the release"
	Return;
}

$deleteAssetParams = @{
	Uri = "https://api.github.com/repos/Vocaluxe/Vocaluxe/releases/$releaseId";
	Method = 'DELETE';
	Headers = @{
		Authorization = 'Basic ' + [Convert]::ToBase64String(
		[Text.Encoding]::ASCII.GetBytes($Env:GitHubKey + ":x-oauth-basic"));
	}
}
try{
	$result = Invoke-RestMethod @deleteAssetParams 
	Write-Host "Successfully deleted release for: $targetTag"
}
catch [System.Net.WebException] 
{
	# Could not delete release
    $statusCode = [int]$_.Exception.Response.StatusCode
    $html = $_.Exception.Response.StatusDescription
	Write-Host "Could not delete release ($html) for: $targetTag"
	Return;
}
 