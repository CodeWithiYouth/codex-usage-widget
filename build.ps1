param()
$ErrorActionPreference = 'Stop'
$taskFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$taskCompiler = Join-Path $taskFramework 'csc.exe'
if (-not (Test-Path -LiteralPath $taskCompiler)) { throw 'Windows .NET Framework C# compiler not found.' }
$taskOutput = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskDestination = Join-Path $taskOutput 'CodexUsageWidget-Windows.exe'
$taskReferences = @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Windows.Forms.dll','System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $taskFramework $_) }
$taskReferences += @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll','WindowsFormsIntegration.dll') | ForEach-Object { '/reference:' + (Join-Path $taskFramework ('WPF\' + $_)) }
$taskSources = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | Where-Object { $_.Name -ne 'AppearanceChecks.cs' } | Select-Object -ExpandProperty FullName
& $taskCompiler /nologo /target:winexe /platform:x64 /optimize+ /utf8output ('/out:' + $taskDestination) ('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')) @taskReferences @taskSources
if ($LASTEXITCODE -ne 0) { throw 'Widget build failed.' }
Get-Item -LiteralPath $taskDestination | Select-Object Name,Length,FullName
