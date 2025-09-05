@echo off
echo Building IntuneAppBuilder for Linux (self-contained, not trimmed)...

REM Clean previous builds
if exist "bin\linux-x64" rmdir /s /q "bin\linux-x64"

REM Build the Console project as self-contained for Linux x64
dotnet publish Source\Console\Console.csproj ^
    --configuration Release ^
    --runtime linux-x64 ^
    --self-contained true ^
    --output "bin\linux-x64" ^
    /p:PublishSingleFile=true ^
    /p:PublishTrimmed=false ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:DebugType=None ^
    /p:DebugSymbols=false

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo Output directory: bin\linux-x64
    echo Executable: bin\linux-x64\IntuneAppBuilder.Console
    echo.
    dir "bin\linux-x64\IntuneAppBuilder.Console"
) else (
    echo.
    echo Build failed with error level %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

pause
