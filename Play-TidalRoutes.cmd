@echo off
if not exist "%~dp0Builds\TidalRoutes\Waterline.exe" (
    echo Build not found. In Unity use Waterline - 26 Build prepared tidal routes.
    pause
    exit /b 1
)
start "" "%~dp0Builds\TidalRoutes\Waterline.exe"
