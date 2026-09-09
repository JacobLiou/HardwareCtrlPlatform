@echo off
REM VOA 准直器手动调节工位发布脚本（自包含，Batch）

setlocal enabledelayedexpansion

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
set APP_PROJECT=%PROJECT_ROOT%\src\VoaCollimator.App\VoaCollimator.App.csproj
set CONFIGURATION=Release
set RUNTIME_ID=win-x86
set OUTPUT_BASE=%PROJECT_ROOT%\publish
set PLATFORM_TARGET=x86

if not "%~1"=="" set CONFIGURATION=%~1
if not "%~2"=="" set RUNTIME_ID=%~2
if not "%~3"=="" set OUTPUT_BASE=%~3

if /I "%RUNTIME_ID%"=="win-x86" set PLATFORM_TARGET=x86
if /I "%RUNTIME_ID%"=="win-arm64" set PLATFORM_TARGET=ARM64
if /I "%RUNTIME_ID%"=="win-x64" set PLATFORM_TARGET=x64

set OUTPUT_PATH=%OUTPUT_BASE%\%RUNTIME_ID%\%CONFIGURATION%

if not exist "%APP_PROJECT%" (
    echo 错误：找不到项目文件 %APP_PROJECT%
    exit /b 1
)

echo.
echo ========================================
echo VOA 准直器手动调节工位 — 自包含发布
echo ========================================
echo 项目：%APP_PROJECT%
echo 配置：%CONFIGURATION%
echo 运行时：%RUNTIME_ID%
echo 输出目录：%OUTPUT_PATH%
echo ========================================
echo.

dotnet publish "%APP_PROJECT%" ^
    -c %CONFIGURATION% ^
    -r %RUNTIME_ID% ^
    -o "%OUTPUT_PATH%" ^
    --self-contained true ^
    /p:DebugType=embedded ^
    /p:DebugSymbols=true ^
    /p:PublishReadyToRun=true ^
    /p:PlatformTarget=%PLATFORM_TARGET%

if errorlevel 1 (
    echo.
    echo 错误：发布失败
    exit /b 1
)

if exist "%PROJECT_ROOT%\config" (
    if exist "%OUTPUT_PATH%\config" rmdir /s /q "%OUTPUT_PATH%\config"
    xcopy "%PROJECT_ROOT%\config" "%OUTPUT_PATH%\config\" /E /I /Y >nul
    echo 已复制配置目录：%OUTPUT_PATH%\config
)

echo.
echo 发布成功
echo 输出位置：%OUTPUT_PATH%
echo.
echo 可执行文件：%OUTPUT_PATH%\VoaCollimator.App.exe
echo 说明：目标机无需安装 .NET 运行时；请整目录拷贝部署。
echo.

endlocal
