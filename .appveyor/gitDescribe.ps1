if($Env:APPVEYOR_PULL_REQUEST_NUMBER)
{
	$Env:VersionTag = "PR_$Env:APPVEYOR_PULL_REQUEST_NUMBER"
	Return $Env:VersionTag
}

$currentCommitSha = $Env:CurrentCommitSha
$githubRepoApiUri = $Env:githubRepoApiUri #"https://api.github.com/repos/:user/:repo/"
$Env:VersionTag = ""
$gitApiAuthHeader = @{
		Authorization = 'Basic ' + [Convert]::ToBase64String(
		[Text.Encoding]::ASCII.GetBytes($Env:GitHubKey + ":x-oauth-basic"));
	};
# Inline-if helper function
Function IIf($If, $Right, $Wrong) {If ($If) {$Right} Else {$Wrong}}

try{
	$result = Invoke-RestMethod -Uri "$($githubRepoApiUri)git/refs/tags" -Method 'GET' -Headers $gitApiAuthHeader
}
catch [System.Net.WebException] 
{	
	# Error getting tags
    $statusCode = [int]$_.Exception.Response.StatusCode
    $html = $_.Exception.Response.StatusDescription
	Write-Host "Error getting tags ($html)"
	Return ;
}

# Get tag info
try{
	$tagsInfo = ($result | % { "$($githubRepoApiUri)git/tags/$($_.object.sha)" ;} | % { try { Invoke-RestMethod -Method 'GET' -Uri $($_) -Headers $gitApiAuthHeader} catch {}})
}
catch [System.Net.WebException] 
{	
	# Error getting tags
    $statusCode = [int]$_.Exception.Response.StatusCode
    $html = $_.Exception.Response.StatusDescription
	Write-Host "Error getting info for tags ($html)"
	Return ;
}

# Compare taged commits with current commit
try{
	$commitTagComp = $tagsInfo | select sha, tag, tagger, @{n='dist';e={[int](Invoke-RestMethod -Method 'GET' -Uri "$($githubRepoApiUri)compare/$($_.object.sha)...$currentCommitSha" -Headers $gitApiAuthHeader | % { (IIf ($_.status -ne "diverged") ($_.ahead_by) (-1))})}}
}
catch [System.Net.WebException] 
{	
	# Error comparing tags
    $statusCode = [int]$_.Exception.Response.StatusCode
    $html = $_.Exception.Response.StatusDescription
	Write-Host "Error comparing tags with current commit ($html)"
	Return ;
}

$latestTag = $commitTagComp | where {$_.dist -ge 0} | sort-object -property @{Expression="dist";Descending=$false}, @{Expression="tag";Descending=$false} | Select-Object -first 1

if(!$latestTag){
	Write-Host "Commit not describable"	
	Return ;
}

$Env:VersionTag = "$($latestTag.tag)-$($latestTag.dist)-g$($currentCommitSha.Substring(0,7))"
Return $Env:VersionTag
