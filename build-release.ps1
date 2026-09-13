param([string]$Version = "0.1.0")

$ErrorActionPreference = "Stop"
$repositoryRoot = $PSScriptRoot
$publishDirectory = Join-Path $repositoryRoot "artifacts\publish"
$installerDirectory = Join-Path $repositoryRoot "artifacts\installer"
$portableArchive = Join-Path $installerDirectory "IKDex-$Version-win-x64-portable.zip"

New-Item -ItemType Directory -Force -Path $publishDirectory, $installerDirectory | Out-Null
dotnet publish (Join-Path $repositoryRoot "IKDex\IKDex.csproj") -p:PublishProfile=Windows-x64 -p:Version=$Version
if ($LASTEXITCODE -ne 0) { throw "Yayın derlemesi başarısız oldu." }

if (Test-Path -LiteralPath $portableArchive) { Remove-Item -LiteralPath $portableArchive -Force }
Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $portableArchive -CompressionLevel Optimal
Write-Host "Taşınabilir paket: $portableArchive"

$innoCompiler = Get-Command iscc.exe -ErrorAction SilentlyContinue
if ($innoCompiler) {
    & $innoCompiler.Source "/DMyAppVersion=$Version" (Join-Path $repositoryRoot "installer\IKDex.iss")
    if ($LASTEXITCODE -ne 0) { throw "Kurulum paketi oluşturulamadı." }
    Write-Host "Kurulum paketi: $installerDirectory"
} else {
    Write-Warning "Inno Setup bulunamadı. Taşınabilir paket oluşturuldu; kurulum EXE'si için Inno Setup 6 kurup betiği yeniden çalıştırın."
}
