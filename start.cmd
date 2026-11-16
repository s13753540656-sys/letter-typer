@echo off
setlocal
set "LETTER_TYPER_DIR=%~dp0"
set "LETTER_TYPER_MODE=%~1"
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -STA -Command "try { $ErrorActionPreference='Stop'; if ($ExecutionContext.SessionState.LanguageMode -ne 'FullLanguage') { throw 'FullLanguage PowerShell is required. Contact your administrator; do not change security policy.' }; Add-Type -AssemblyName System.Windows.Forms; Add-Type -AssemblyName System.Drawing; $src=[IO.File]::ReadAllText((Join-Path $env:LETTER_TYPER_DIR 'LetterTyper.cs'),[Text.Encoding]::UTF8); Add-Type -TypeDefinition $src -ReferencedAssemblies System.Windows.Forms,System.Drawing,System,System.Web.Extensions; $opts=@($env:LETTER_TYPER_DIR); if ($env:LETTER_TYPER_MODE -eq '--self-check') { $opts+='--self-check' }; $code=[LetterTyper]::Main($opts); if ($code -ne 0) { throw ('Application failed with code '+$code) } } catch { Write-Host $_; exit 1 }"
set "LETTER_TYPER_EXIT=%ERRORLEVEL%"
if not "%LETTER_TYPER_EXIT%"=="0" if not "%LETTER_TYPER_MODE%"=="--self-check" pause
exit /b %LETTER_TYPER_EXIT%
