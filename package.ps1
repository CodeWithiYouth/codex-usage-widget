param([ValidatePattern('^$|^v[0-9]+\.[0-9]+\.[0-9]+$')][string]$Version = '')
$ErrorActionPreference = 'Stop'
$taskOutput = Join-Path $PSScriptRoot 'dist'
$taskExe = Join-Path $taskOutput 'CodexUsageWidget-Windows.exe'
if (-not (Test-Path -LiteralPath $taskExe)) { throw 'Run build.ps1 before packaging.' }
$taskVersionSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'AppVersion.cs'))
$taskVersionMatch = [regex]::Match($taskVersionSource, 'Number\s*=\s*"([0-9]+\.[0-9]+\.[0-9]+)"')
if (-not $taskVersionMatch.Success) { throw 'AppVersion.cs must define a valid application version.' }
$taskSourceVersion = $taskVersionMatch.Groups[1].Value
$taskExeInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($taskExe)
if ($taskExeInfo.ProductVersion -ne $taskSourceVersion -or $taskExeInfo.FileVersion -ne ($taskSourceVersion + '.0')) { throw 'EXE version does not match source. Run build.ps1 again before packaging.' }
$taskBuildTag = 'v' + $taskSourceVersion
if ([string]::IsNullOrEmpty($Version)) { $Version = $taskBuildTag }
if ($Version -ne $taskBuildTag) { throw ('Requested package version does not match EXE version ' + $taskBuildTag + '.') }
$taskPackage = Join-Path (Join-Path $taskOutput 'portable') $Version
$taskRelease = Join-Path (Join-Path $taskOutput 'release') $Version
New-Item -ItemType Directory -Path $taskPackage,$taskRelease -Force | Out-Null
$taskContents = @('README.md','CHANGELOG.md','styles.png','settings.png','quota-colors.png')
Copy-Item -LiteralPath $taskExe -Destination $taskPackage -Force
foreach ($taskName in $taskContents) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $taskName) -Destination $taskPackage -Force }
[IO.File]::WriteAllText((Join-Path $taskPackage 'Start.cmd'), "@echo off`r`nstart `"`" `"%~dp0CodexUsageWidget-Windows.exe`"`r`n", [Text.Encoding]::ASCII)
Copy-Item -LiteralPath $taskExe -Destination $taskRelease -Force
$taskZip = Join-Path $taskRelease ('codex-usage-widget-' + $Version + '-windows-x64.zip')
$taskZipContents = @((Join-Path $taskPackage 'CodexUsageWidget-Windows.exe'),(Join-Path $taskPackage 'Start.cmd'))
$taskZipContents += $taskContents | ForEach-Object { Join-Path $taskPackage $_ }
Compress-Archive -LiteralPath $taskZipContents -DestinationPath $taskZip -Force
$taskChecksums = foreach ($taskFile in @((Join-Path $taskRelease 'CodexUsageWidget-Windows.exe'),$taskZip)) {
    (Get-FileHash -LiteralPath $taskFile -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($taskFile)
}
[IO.File]::WriteAllText((Join-Path $taskRelease 'SHA256SUMS.txt'), ($taskChecksums -join "`n") + "`n", [Text.Encoding]::ASCII)
$taskChanges = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'CHANGELOG.md'))
$taskNotes = "Windows x64 portable build $Version.`r`n`r`n" + $taskChanges + "`r`nDownload CodexUsageWidget-Windows.exe directly, or extract the ZIP and run Start.cmd.`r`nUses Windows .NET Framework 4.8; no runtime is bundled. Requires an available, signed-in Codex backend.`r`n"
[IO.File]::WriteAllText((Join-Path $taskRelease 'release-notes.md'), $taskNotes, (New-Object Text.UTF8Encoding($false)))
Get-ChildItem -LiteralPath $taskRelease -File | Select-Object Name,Length