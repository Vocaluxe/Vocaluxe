
if($Env:APPVEYOR_REPO_TAG -ne "true") {
    Write-Host "Pushed tag detected"
	if($Env:APPVEYOR_REPO_BRANCH -eq "appveyorTest"){
        Write-Host "Nightly build tag was detected"
		$targetTag = "Nightly"	
		$Env:NIGHTLY_BUILD = true		
	}
	else{
		Write-Host "No special branch found"
		Return
	}
    Write-Host "Moving tag $targetTag to $Env:APPVEYOR_REPO_BRANCH"
	git config --global user.email "build@vocaluxe.de";
	git config --global user.name "Vocaluxe_Automatic_Build";
	git tag "$targetTag" -f;
	git push -q -f "https://$($Env:GitHubKey):x-oauth-basic@github.com/lukeIam/Vocaluxe.git" "$targetTag";
	
	Write-Host "Tag $targetTag points now to the head of branch $Env:APPVEYOR_REPO_BRANCH"
}
else
{
    Write-Host "No tag for this detected"
}