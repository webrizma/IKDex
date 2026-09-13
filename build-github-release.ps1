param(
    [Parameter(Mandatory=$true)][string]$Version,
    [Parameter(Mandatory=$true)][string]$RepositoryUrl
)

$ErrorActionPreference = "Stop"
if ($RepositoryUrl -notmatch '^https://github\.com/[^/]+/[^/]+/?$') { throw "Geçerli bir GitHub depo adresi girin." }
if ([string]::IsNullOrWhiteSpace($env:VPK_TOKEN)) { throw "GitHub erişim anahtarını VPK_TOKEN ortam değişkeninde tanımlayın." }

$repositoryRoot = $PSScriptRoot
$publishDirectory = Join-Path $repositoryRoot "artifacts\velopack-publish"
$releaseDirectory = Join-Path $repositoryRoot "artifacts\releases"
$configurationPath = Join-Path $repositoryRoot "IKDex\update.json"
$configuration = @{ repositoryUrl = $RepositoryUrl.TrimEnd('/') } | ConvertTo-Json
Set-Content -LiteralPath $configurationPath -Value $configuration -Encoding UTF8

dotnet tool restore
dotnet publish (Join-Path $repositoryRoot "IKDex\IKDex.csproj") -c Release -r win-x64 --self-contained true -o $publishDirectory -p:Version=$Version
dotnet tool run vpk download github --repoUrl $RepositoryUrl --outputDir $releaseDirectory --token $env:VPK_TOKEN
dotnet tool run vpk pack --packId IKDex.HR --packVersion $Version --packDir $publishDirectory --mainExe IKDex.exe --packTitle IKDex --icon (Join-Path $repositoryRoot "IKDex\Assets\ikdex-app.ico") --outputDir $releaseDirectory
dotnet tool run vpk upload github --repoUrl $RepositoryUrl --outputDir $releaseDirectory --token $env:VPK_TOKEN --publish --tag "v$Version" --releaseName "IKDex $Version"
