@if "%SCM_TRACE_LEVEL%" NEQ "4" @echo off

:: Setup
:: -----

setlocal enabledelayedexpansion

IF NOT DEFINED DEPLOYMENT_SOURCE (
  SET DEPLOYMENT_SOURCE=%~dp0%.
)

IF NOT DEFINED DEPLOYMENT_TARGET (
  SET DEPLOYMENT_TARGET=%DEPLOYMENT_SOURCE%\publish
)

:: Build và Publish
:: ---------------

echo Building and publishing application...

:: Clean publish folder if exists
IF EXIST "%DEPLOYMENT_TARGET%" (
  rd /s /q "%DEPLOYMENT_TARGET%"
)

:: Restore dependencies
call dotnet restore
IF !ERRORLEVEL! NEQ 0 goto error

:: Build project
call dotnet build --configuration Release
IF !ERRORLEVEL! NEQ 0 goto error

:: Publish project
call dotnet publish --configuration Release --output "%DEPLOYMENT_TARGET%"
IF !ERRORLEVEL! NEQ 0 goto error

echo Successfully deployed to %DEPLOYMENT_TARGET%
goto end

:error
echo An error has occurred during web site deployment.
call :exitSetErrorLevel
call :exitFromFunction 2>nul

:exitSetErrorLevel
exit /b 1

:exitFromFunction
()

:end
echo Finished successfully.