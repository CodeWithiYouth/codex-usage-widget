param([ValidatePattern('^$|^v[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')][string]$Version = '')
$ErrorActionPreference = 'Stop'
$taskOutput = Join-Path $PSScriptRoot 'dist'
$taskExe = Join-Path $taskOutput 'CodexUsageWidget-Windows.exe'
if (-not (Test-Path -LiteralPath $taskExe)) { throw 'Run build.ps1 before packaging.' }
$taskPackage = Join-Path $taskOutput 'portable'
$taskRelease = Join-Path $taskOutput 'release'
New-Item -ItemType Directory -Path $taskPackage,$taskRelease -Force | Out-Null
Copy-Item -LiteralPath $taskExe -Destination $taskPackage -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $taskPackage -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'styles.png'),(Join-Path $PSScriptRoot 'settings.png') -Destination $taskPackage -Force
[IO.File]::WriteAllText((Join-Path $taskPackage 'Start.cmd'), "@echo off`r`nstart `"`" `"%~dp0CodexUsageWidget-Windows.exe`"`r`n", [Text.Encoding]::ASCII)
Copy-Item -LiteralPath $taskExe -Destination $taskRelease -Force
$taskZip = Join-Path $taskRelease 'codex-usage-widget-windows-x64.zip'
Compress-Archive -LiteralPath (Join-Path $taskPackage 'CodexUsageWidget-Windows.exe'),(Join-Path $taskPackage 'README.md'),(Join-Path $taskPackage 'Start.cmd'),(Join-Path $taskPackage 'styles.png'),(Join-Path $taskPackage 'settings.png') -DestinationPath $taskZip -Force
$taskChecksums = foreach ($taskFile in @((Join-Path $taskRelease 'CodexUsageWidget-Windows.exe'),$taskZip)) {
    (Get-FileHash -LiteralPath $taskFile -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($taskFile)
}
[IO.File]::WriteAllText((Join-Path $taskRelease 'SHA256SUMS.txt'), ($taskChecksums -join "`n") + "`n", [Text.Encoding]::ASCII)
$taskNotes = @"
Windows x64 portable build $Version.

- Seven taskbar styles with custom colors, labels and remaining/used display.
- Percentage inside compact side-by-side rings.
- Six reset time formats; long dates wrap inside the ring layout.
- Native Store package and desktop cache CLI detection.

Download CodexUsageWidget-Windows.exe directly, or extract the ZIP and run Start.cmd.
Uses Windows .NET Framework 4.8; no runtime is bundled. Requires an available, signed-in Codex backend.
See README for setup and Windows 10 testing limitations.
"@
[IO.File]::WriteAllText((Join-Path $taskRelease 'release-notes.md'), $taskNotes, (New-Object Text.UTF8Encoding($false)))
Get-ChildItem -LiteralPath $taskRelease -File | Select-Object Name,Length
