param([switch]$Appearance,[switch]$Live,[switch]$StoreOnly)
$ErrorActionPreference = 'Stop'
if ($StoreOnly -and -not $Live) { throw '-StoreOnly requires -Live.' }
& (Join-Path $PSScriptRoot 'build.ps1')
$taskOutput = Join-Path $PSScriptRoot 'dist'
$taskExe = Join-Path $taskOutput 'CodexUsageWidget-Windows.exe'
$taskArguments = @('--check')
if ($Live) { $taskArguments += '--live' }
if ($StoreOnly) { $taskArguments += '--store-only' }
$taskCheck = Start-Process -FilePath $taskExe -ArgumentList $taskArguments -WorkingDirectory $taskOutput -WindowStyle Hidden -Wait -PassThru
if ($taskCheck.ExitCode -ne 0) { throw ('Widget checks failed: ' + $taskCheck.ExitCode) }
if ($Appearance) {
    $taskFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
    $taskReferences = @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Windows.Forms.dll','System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $taskFramework $_) }
    $taskReferences += @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll') | ForEach-Object { '/reference:' + (Join-Path $taskFramework ('WPF\' + $_)) }
    $taskTestExe = Join-Path $taskOutput 'AppearanceChecks.exe'
    & (Join-Path $taskFramework 'csc.exe') /nologo /utf8output /target:exe /platform:x64 ('/reference:' + $taskExe) ('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')) ('/out:' + $taskTestExe) @taskReferences (Join-Path $PSScriptRoot 'AppearanceChecks.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Appearance checks build failed.' }
    & $taskTestExe
    if ($LASTEXITCODE -ne 0) { throw 'Appearance checks failed.' }
}
Write-Output ('Checks passed. Reports: ' + $taskOutput)
