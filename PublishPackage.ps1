$ProjectDir = "PSWikiClient"
$OutputDir = Join-Path $ProjectDir "bin/Release/netstandard2.0"
$RedistDir = Join-Path $ProjectDir "bin/Redist/PSWikiClient"

dotnet build -c RELEASE $ProjectDir
if ($LASTEXITCODE) {
    Exit $LASTEXITCODE
}

$RepositoryName = (Read-Host "Repository")
$ApiKey = (Read-Host "NuGet API Key")

try {
    Remove-Item $RedistDir -Recurse -Force
}
catch {
    Write-Output $Error
}
New-Item $RedistDir -Type:Directory -Force
Copy-Item (Join-Path $OutputDir *) (Join-Path $RedistDir /) -Recurse
Publish-Module -Path:$RedistDir -Repository:$RepositoryName -NuGetApiKey:$ApiKey
