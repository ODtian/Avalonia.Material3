#requires -Version 7.2
param([Parameter(Mandatory=$true)][string]$Manifest, [switch]$NativeSmoke)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/consumer.ps1"
$root=Split-Path $PSScriptRoot -Parent
$Manifest=[IO.Path]::GetFullPath($Manifest)
$inputManifest=Get-Content $Manifest -Raw | ConvertFrom-Json
$commit=(git -C $root rev-parse HEAD).Trim()
if ($inputManifest.commit -cne $commit) { throw 'Stale manifest: package commit is not selected HEAD.' }
$version=[string](([xml](Get-Content "$root/Directory.Build.props" -Raw)).Project.PropertyGroup.Material3Version)
if ($inputManifest.version -cne $version) { throw 'Stale selected package version.' }
Assert-PackageIdentity $inputManifest.package $version $commit
$run=Join-Path (Split-Path $Manifest -Parent) ('publish-' + [guid]::NewGuid().ToString('N'))
$consumer=Join-Path $run 'consumer'
$oldPackages=$env:NUGET_PACKAGES
try {
    New-PackageConsumer $root $consumer $inputManifest.package $inputManifest.sha256
    $env:NUGET_PACKAGES="$consumer/packages"
    $outputs=@()
    foreach ($mode in 'aot','trimmed-managed') {
        foreach ($hostName in 'Gallery','StandaloneHost') {
            $output=Join-Path $run "$mode/$hostName"
            $aot=if ($mode -eq 'aot') { 'true' } else { 'false' }
            $publishArgs=@('publish', "$consumer/samples/$hostName/$hostName.csproj", '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
                "-p:PublishAot=$aot", '-p:PublishTrimmed=true', '-p:TrimMode=full', '-p:EnableAotAnalyzer=true', '-p:EnableTrimAnalyzer=true',
                '-p:SuppressTrimAnalysisWarnings=false', '-p:TrimmerSingleWarn=false', '-p:ILLinkTreatWarningsAsErrors=true', '-p:IlcTreatWarningsAsErrors=true',
                "-bl:$run/$hostName-$mode.binlog", '-o', $output)
            Invoke-CheckedDotnet @publishArgs | Tee-Object "$run/$hostName-$mode.log"
            Assert-ConsumerAssets $consumer $version $inputManifest.package $inputManifest.sha256
            $executable="$output/$hostName.exe"
            $outputs += @{ mode=$mode; host=$hostName; executable=$executable; sha256=(Get-FileHash $executable -Algorithm SHA256).Hash; bytes=(Get-Item $executable).Length; directoryBytes=(Get-ChildItem $output -File -Recurse | Measure-Object Length -Sum).Sum }
            if ($NativeSmoke) {
                if (!$IsWindows) { throw 'Windows NativeAOT runtime requires Windows.' }
                & powershell.exe -NoProfile -File "$PSScriptRoot/windows-gallery-smoke.ps1" -Executable $executable -Evidence "$run/native-$hostName-$mode"
                if ($LASTEXITCODE -ne 0) { throw "Published native scenario failed: $hostName/$mode" }
            }
        }
    }
    @{ commit=$commit; version=$version; packageSha256=$inputManifest.sha256; rid='win-x64'; sdk=(& dotnet --version); outputs=$outputs; strictIlWarnings=$true; nativeSmoke=[bool]$NativeSmoke } | ConvertTo-Json -Depth 5 | Set-Content "$run/published.json" -Encoding utf8
    Write-Host "PASS published artifacts: $run/published.json"
}
finally { $env:NUGET_PACKAGES=$oldPackages; Stop-ConsumerCollectors $consumer; Write-Host "Publish evidence retained: $run" }
