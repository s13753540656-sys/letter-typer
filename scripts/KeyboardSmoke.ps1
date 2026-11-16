param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$source = [IO.File]::ReadAllText((Join-Path $Root 'LetterTyper.cs'), [Text.Encoding]::UTF8)
$harness = [IO.File]::ReadAllText((Join-Path $Root 'tests\KeyboardSmoke.cs'), [Text.Encoding]::UTF8)
Add-Type -TypeDefinition ($source + "`n" + $harness.Replace('using System;', '').Replace('using System.Drawing;', '').Replace('using System.Runtime.InteropServices;', '').Replace('using System.Windows.Forms;', '')) -ReferencedAssemblies System.Windows.Forms,System.Drawing,System,System.Web.Extensions
$result = [KeyboardSmoke]::Run()
Write-Output $result
if (!$result.StartsWith('PASS:')) { throw 'Keyboard verification failed.' }
