@echo off
setlocal enabledelayedexpansion

if "%~1"=="" (
    echo Version number is required.
    echo Usage: build.bat [version] [extra_args...]
    exit /b 1
)

set "version=%~1"

echo.
echo Compiling Rake with dotnet...
dotnet publish Rake -c Release -o %~dp0publish

echo.
echo Building Velopack Release v%version%
vpk pack -u Rake -o %~dp0releases -p %~dp0publish -v %*