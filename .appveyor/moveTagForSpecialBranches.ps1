if($Env:APPVEYOR_REPO_TAG -ne "true" -and (-not $Env:APPVEYOR_PULL_REQUEST_NUMBER)) {
    Write-Host "Push without tag detected"
	if($Env:APPVEYOR_REPO_BRANCH -eq "develop"){
        Write-Host "Nightly build branch was detected"
		$targetTag = "Nightly"	
		$Env:NIGHTLY_BUILD = true		
	}
	else{
		Write-Host "No special branch found"
		Return
	}
    Write-Host "Moving tag $targetTag to $Env:APPVEYOR_REPO_BRANCH"
	git config --global user.email "noreply@vocaluxe.org";
	git config --global user.name "VocaluxeBuildBot";
	git tag "$targetTag" -f;
	git push -q -f "https://$($Env:GitHubKey):x-oauth-basic@github.com/Vocaluxe/Vocaluxe.git" "$targetTag";
	
	Write-Host "Tag $targetTag points now to the head of branch $Env:APPVEYOR_REPO_BRANCH"
}