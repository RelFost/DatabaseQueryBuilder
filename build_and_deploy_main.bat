@echo off

:: Set the console encoding to UTF-8
chcp 65001

:: Find the csproj file
for %%i in (src\*.csproj) do set csprojPath=%%i

:: Extract the file name without extension
for %%i in (%csprojPath%) do set dllName=%%~ni

:: Run the PowerShell script
powershell -ExecutionPolicy Bypass -File build_and_deploy_dev.ps1 -CsprojPath "%csprojPath%" -DllName "%dllName%"
if %errorlevel% neq 0 (
    echo PowerShell script execution failed.
    pause
    exit /b %errorlevel%
)

git checkout -b main

:: Add files to git and commit
git add .
if %errorlevel% neq 0 (
    echo Failed to add files to git.
    pause
    exit /b %errorlevel%
)

set /p commitMessage="Enter commit message: "
git commit -m "%commitMessage%"
if %errorlevel% neq 0 (
    echo Failed to commit changes.
    pause
    exit /b %errorlevel%
)
git push origin main
if %errorlevel% neq 0 (
    echo Failed to push to main branch.
    pause
    exit /b %errorlevel%
)

echo Script execution completed.
pause
