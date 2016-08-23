
if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }


#$revision = @{ $true = $env:APPVEYOR_BUILD_NUMBER; $false = 1 }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];
#$revision = "{0:D4}" -f [convert]::ToInt32($revision, 10)
#$rev=(git rev-parse --short HEAD)

$json = Get-Content -Raw -Path ".\src\SemVer\project.json" | ConvertFrom-Json
$env:projectVersion = $json.version.trimend("*").trimend("-")
$env:projectVersion

dotnet restore
dotnet test .\src\SemVer.Tests -c Release
dotnet pack .\src\SemVer -c Release -o .\artifacts
#dotnet pack .\src\SemVer -c Release -o .\artifacts --version-suffix="git-$rev"