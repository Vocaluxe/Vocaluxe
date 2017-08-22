Write-Host "Deployment finished -> do en empty commit on the gh-pages branch to update the page"
cd ..
git clone -q "https://github.com/Vocaluxe/Vocaluxe.git" -b "gh-pages" --single-branch "VocaluxeWeb"
cd VocaluxeWeb
git config --global user.email "noreply@vocaluxe.org"
git config --global user.name "VocaluxeBuildBot"
git commit --allow-empty -m "Trigger page update after a new publish (triggered by $Env:APPVEYOR_REPO_COMMIT_MESSAGE [$Env:APPVEYOR_REPO_COMMIT])"
git push -q "https://$($Env:GitHubKey):x-oauth-basic@github.com/Vocaluxe/Vocaluxe.git" "gh-pages"
cd ..\Vocaluxe