@echo off
call "%~dp0start.cmd" --self-check
exit /b %ERRORLEVEL%
