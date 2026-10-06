#requires -Version 7.2
param([Parameter(Mandatory=$true)][string]$Manifest, [Parameter(Mandatory=$true)][string]$BaselinePackage)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/consumer.ps1"
$root=Split-Path $PSScriptRoot -Parent
$m=Get-Content $Manifest -Raw | ConvertFrom-Json
$hash='C52D2E60A9E508ACF7EA1915EFBD5A84E7508AD39D8662453793759AC44174B6'
$run=Join-Path (Split-Path $Manifest -Parent) 'compatibility'
$consumer=Join-Path $run 'consumer'
$oldPackages=$env:NUGET_PACKAGES
try {
    New-PackageConsumer $root $consumer $BaselinePackage $hash
    $env:NUGET_PACKAGES="$consumer/packages"
    $project="$consumer/tests/CompatibilityClient/CompatibilityClient.csproj"
    Invoke-CheckedDotnet build $project -c Release -p:Material3Version=0.1.0-preview.1 -o "$run/old-client" | Tee-Object "$run/old-build.log"
    & dotnet "$run/old-client/CompatibilityClient.dll" | Tee-Object "$run/old-runtime.log"
    if ($LASTEXITCODE -ne 0) { throw 'Initial binary fixture failed.' }
    $clientHash=(Get-FileHash "$run/old-client/CompatibilityClient.dll" -Algorithm SHA256).Hash
    if ((Get-FileHash $m.package -Algorithm SHA256).Hash -cne $m.sha256) { throw 'New artifact changed.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip=[IO.Compression.ZipFile]::OpenRead($m.package)
    try {
        $entry=$zip.GetEntry('lib/net10.0/Avalonia.Material3.dll')
        if (!$entry) { throw 'Selected library assembly missing.' }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, "$run/old-client/Avalonia.Material3.dll", $true)
    } finally { $zip.Dispose() }
    # No rebuilding of the client: only the library binary changes.
    & dotnet "$run/old-client/CompatibilityClient.dll" | Tee-Object "$run/upgraded-binary-runtime.log"
    if ($LASTEXITCODE -ne 0) { throw 'Old compiled binary failed with new package assembly.' }
    if ((Get-FileHash "$run/old-client/CompatibilityClient.dll" -Algorithm SHA256).Hash -cne $clientHash) { throw 'Client unexpectedly rebuilt.' }
    Copy-Item $m.package "$consumer/artifacts/packages/"
    Invoke-CheckedDotnet build $project -c Release "-p:Material3Version=$($m.version)" -o "$run/rebuilt-client" | Tee-Object "$run/new-build.log"
    & dotnet "$run/rebuilt-client/CompatibilityClient.dll" | Tee-Object "$run/new-runtime.log"
    if ($LASTEXITCODE -ne 0) { throw 'Source/XAML rebuilt compatibility failed.' }
    @{ baselinePackage=$BaselinePackage; baselineHash=$hash; newVersion=$m.version; newHash=$m.sha256; oldClientSha256=$clientHash; upgrade='old compiled client unchanged; only M3 assembly replaced; constructor/deconstruct/with/compiled-XAML/theme resources'; appM11='BLOCKED; this fixture is NOT App'; commit=$m.commit } | ConvertTo-Json | Set-Content "$run/compatibility.json" -Encoding utf8
    Write-Host "PASS compatibility: $run/compatibility.json"
} finally { $env:NUGET_PACKAGES=$oldPackages; Stop-ConsumerCollectors $consumer }
