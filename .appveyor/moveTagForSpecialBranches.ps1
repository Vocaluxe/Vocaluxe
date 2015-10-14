if($Env:APPVEYOR_REPO_TAG -ne "true") {
	if($Env:APPVEYOR_REPO_BRANCH -eq "appveyorTest"){		
		$Env:NIGHTLY_BUILD = true		
	}
	else{
		Write-Host "No tag or special branch found"
		Return
	}
	git config --global user.email "build@vocaluxe.de";
	git config --global user.name "Vocaluxe_Automatic_Build";
	git tag "$targetTag" -f;
	git push -q -f "https://$($Env:GitHubKey):x-oauth-basic@github.com/lukeIam/Vocaluxe.git" "$targetTag";
	
	Write-Host "Branch $Env:APPVEYOR_REPO_BRANCH -> Tag=$targetTag"
}