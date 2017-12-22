choco install gitlink --version 3.1.0-unstable0017 --pre --no-progress --limit-output

Get-ChildItem -Recurse "*.pdb" | %{
    If ($_.Fullname -contains 'PitchTracker.pdb'){
        & gitlink -u "`"https://github.com/$($Env:APPVEYOR_REPO_NAME)`"" -a --baseDir "`"$Env:APPVEYOR_BUILD_FOLDER`"" --commit "`"$Env:APPVEYOR_REPO_COMMIT`"" "`"$($_.Fullname)`""
    }
    else{
        & gitlink -u "`"https://github.com/$($Env:APPVEYOR_REPO_NAME)`"" --baseDir "`"$Env:APPVEYOR_BUILD_FOLDER`"" --commit "`"$Env:APPVEYOR_REPO_COMMIT`"" "`"$($_.Fullname)`""
    }    
}