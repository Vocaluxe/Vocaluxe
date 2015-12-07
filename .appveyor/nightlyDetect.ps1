if($Env:APPVEYOR_REPO_TAG -eq "true") {
    Write-Host "Taged commit detected"
	$targetTag = $Env:APPVEYOR_REPO_TAG_NAME
	Write-Host "Tag=$targetTag"
	$Env:NIGHTLY_BUILD = "false"
	if($targetTag -eq "Nightly") {
		$Env:NIGHTLY_BUILD = "true"
        Write-Host "This is a nightly build"
	}
    else
    {
        Write-Host "This is NOT a nightly build"
    }
}
else{
	Return;
}