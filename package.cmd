@echo off
setlocal
set "LETTER_TYPER_DIR=%~dp0"
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -Command "try { $ErrorActionPreference='Stop'; & ([scriptblock]::Create([IO.File]::ReadAllText((Join-Path $env:LETTER_TYPER_DIR 'scripts\Package.ps1')))) -Root $env:LETTER_TYPER_DIR } catch { Write-Host $_; exit 1 }"
exit /b %ERRORLEVEL%
