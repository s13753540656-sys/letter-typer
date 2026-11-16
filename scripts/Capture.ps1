param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$source = [IO.File]::ReadAllText((Join-Path $Root 'LetterTyper.cs'), [Text.Encoding]::UTF8)
Add-Type -TypeDefinition $source -ReferencedAssemblies System.Windows.Forms,System.Drawing,System,System.Web.Extensions
$output = Join-Path $Root 'docs\images\window.png'
$code = [LetterTyper]::Main(@($Root, '--screenshot', $output))
if ($code -ne 0 -or !(Test-Path -LiteralPath $output)) { throw 'Screenshot capture failed.' }
