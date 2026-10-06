# Shared authoritative copying, exact-package and restore-provenance gate. Dot-source from verifiers.
$ErrorActionPreference = 'Stop'
function Invoke-CheckedDotnet {
    # A simple forwarding function avoids PowerShell common-parameter capture of dotnet -o.
    $Arguments = [string[]]@($args | ForEach-Object { $_ })
    & dotnet @Arguments --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed ($LASTEXITCODE): $($Arguments -join ' ')" }
}
function Assert-PackageIdentity([string]$Package, [string]$Version, [string]$Commit) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($Package)
    try {
        $entry = $zip.GetEntry('Avalonia.Material3.nuspec')
        if (!$entry) { throw 'Selected package is not Avalonia.Material3.' }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { [xml]$spec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($spec.package.metadata.id -cne 'Avalonia.Material3' -or
            $spec.package.metadata.version -cne $Version -or
            $spec.package.metadata.repository.commit -cne $Commit) {
            throw 'Stale/wrong selected package identity, version or source commit.'
        }
    } finally { $zip.Dispose() }
}
function New-PackageConsumer([string]$Root, [string]$Destination, [string]$Package, [string]$ExpectedHash) {
    if ((Get-FileHash $Package -Algorithm SHA256).Hash -cne $ExpectedHash) { throw 'Selected artifact hash mismatch.' }
    New-Item -ItemType Directory -Force "$Destination/artifacts/packages" | Out-Null
    Copy-Item $Package "$Destination/artifacts/packages/"
    foreach ($file in 'Directory.Build.props', 'global.json') { Copy-Item "$Root/$file" $Destination }
    # Deliberately public-only config; never reads/copies repository or user credentials/configuration.
    @'
<configuration><packageSources><clear/><add key="local-m3" value="artifacts/packages"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="local-m3"><package pattern="Avalonia.Material3"/></packageSource><packageSource key="nuget.org"><package pattern="*"/></packageSource></packageSourceMapping></configuration>
'@ | Set-Content "$Destination/NuGet.Config" -Encoding utf8
    foreach ($directory in 'samples', 'tests') {
        Get-ChildItem "$Root/$directory" -Recurse -File | Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj|artifacts)[\\/]' -and
            $_.FullName -notmatch '[\\/]Avalonia.Material3.Tests[\\/](Avalonia.Material3.Tests.csproj|TestApplication.cs)$'
        } | ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($Root, $_.FullName)
            $target = Join-Path $Destination $relative
            New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
            Copy-Item $_.FullName $target
        }
    }
    if (Test-Path "$Destination/src") { throw 'Library source copied into consumer.' }
    # Gallery/Standalone are independent. Ticket-specific wrappers may reference Gallery only.
    # Reject library source or any other/transitive repo reference, including imported props.
    $projects = @(Get-ChildItem "$Destination/samples" -Filter '*.csproj' -Recurse) +
        @(Get-Item "$Destination/tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj")
    foreach ($project in $projects) {
        $json = (& dotnet msbuild $project.FullName -nologo '-getItem:ProjectReference' | Out-String) | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0) { throw "Cannot evaluate project references: $($project.Name)" }
        $allowed = if ($project.BaseName -eq 'PackageConsumption.Tests') {
            @('Gallery','StandaloneHost','TextFieldHost')
        } elseif ($project.BaseName -in 'Gallery','StandaloneHost') { @() } else { @('Gallery') }
        $allowedPaths = @($allowed | ForEach-Object { [IO.Path]::GetFullPath("$Destination/samples/$_/$_.csproj") })
        foreach ($reference in $json.Items.ProjectReference) {
            if ($reference.FullPath -notin $allowedPaths) {
                throw "Forbidden consumer ProjectReference: $($project.Name) -> $($reference.FullPath)"
            }
        }
    }
}
function Assert-ConsumerAssets([string]$Destination, [string]$Version, [string]$Package, [string]$Hash) {
    if ((Get-FileHash $Package -Algorithm SHA256).Hash -cne $Hash) { throw 'Selected package changed during verification.' }
    $assetFiles = @(Get-ChildItem "$Destination/samples", "$Destination/tests/PackageConsumption.Tests" -Filter project.assets.json -Recurse)
    if ($assetFiles.Count -eq 0) { throw 'Consumer has no restored asset graph.' }
    foreach ($assets in $assetFiles) {
        $data = Get-Content $assets.FullName -Raw | ConvertFrom-Json -AsHashtable
        $key = 'Avalonia.Material3/' + $Version
        if (!$data.libraries.ContainsKey($key) -or $data.libraries[$key].type -ne 'package') { throw "M3 is not the selected versioned package: $($assets.FullName)" }
        foreach ($entry in $data.libraries.GetEnumerator()) {
            if ($entry.Value.type -eq 'project' -and ($entry.Key -notmatch '^(Gallery|StandaloneHost|TextFieldHost)/')) { throw "Unexpected project dependency: $($entry.Key)" }
        }
        $cached = Join-Path $Destination "packages/avalonia.material3/$Version/avalonia.material3.$Version.nupkg"
        if ((Get-FileHash $cached -Algorithm SHA256).Hash -cne $Hash) { throw 'Restored package differs from selected artifact.' }
    }
}
function Get-ScenarioNames([string]$Trx) {
    [xml]$document = Get-Content $Trx -Raw
    $names = @($document.TestRun.Results.UnitTestResult | ForEach-Object {
        if ($_.outcome -ne 'Passed') { throw "Scenario not passed: $($_.testName)" }; [string]$_.testName
    })
    [Array]::Sort($names, [StringComparer]::Ordinal)
    return ,$names
}
function Assert-ScenarioParity([string]$Source, [string]$Package, [string]$Output, [string]$ExpectedExtras) {
    $sourceNames = Get-ScenarioNames $Source; $packageNames = Get-ScenarioNames $Package
    $shared = [Collections.Generic.HashSet[string]]::new([string[]]$sourceNames, [StringComparer]::Ordinal)
    $actual = [Collections.Generic.HashSet[string]]::new([string[]]$packageNames, [StringComparer]::Ordinal)
    if ($shared.Count -ne $sourceNames.Count -or $actual.Count -ne $packageNames.Count) { throw 'Duplicate scenario identities.' }
    foreach ($name in $sourceNames) { if (!$actual.Contains($name)) { throw "Missing package scenario: $name" } }
    $extras = @($packageNames | Where-Object { !$shared.Contains($_) })
    $allowed = [Collections.Generic.HashSet[string]]::new([string[]](Get-Content $ExpectedExtras), [StringComparer]::Ordinal)
    if (!$allowed.SetEquals([string[]]$extras)) { throw 'Package-only fixture identity differs from reviewed PackageOnlyScenarios.txt.' }
    @{ comparer='Ordinal'; shared=$sourceNames; packageOnly=$extras } | ConvertTo-Json -Depth 4 | Set-Content $Output -Encoding utf8
}
function Stop-ConsumerCollectors([string]$Destination) {
    if ($IsWindows) { Get-CimInstance Win32_Process | Where-Object {
        $_.Name -eq 'dotnet.exe' -and $_.CommandLine -and $_.CommandLine.Contains($Destination) -and $_.CommandLine.Contains('Avalonia.BuildServices.Collector.dll')
    } | ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue } }
}
