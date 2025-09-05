@echo off
echo Building IntuneAppBuilder solution for Linux (self-contained)...

:: Set environment variables
set CONFIGURATION=Release
set MSBUILD_VERBOSITY=minimal
set FIXED_VERSION=4.0.0

echo.
echo ===== Restoring NuGet packages =====
dotnet restore IntuneAppBuilder.sln --verbosity %MSBUILD_VERBOSITY%
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to restore NuGet packages.
    exit /b 1
)

echo.
echo ===== Building specific projects (skipping tests) =====

:: Building only the specific projects we need, not the entire solution with tests
dotnet build Source\IntuneAppBuilder\IntuneAppBuilder.csproj -p:Version=%FIXED_VERSION% --configuration %CONFIGURATION% --verbosity %MSBUILD_VERBOSITY%
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build of IntuneAppBuilder project failed.
    exit /b 1
)

dotnet build Source\Console\Console.csproj -p:Version=%FIXED_VERSION% --configuration %CONFIGURATION% --verbosity %MSBUILD_VERBOSITY%
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build of Console project failed.
    exit /b 1
)

echo.
echo ===== Tests explicitly skipped =====

echo.
echo ===== Publishing self-contained app for Linux =====
dotnet publish Source\Console\Console.csproj --configuration %CONFIGURATION% --runtime linux-x64 --self-contained true -p:PublishSingleFile=true -p:Version=%FIXED_VERSION%
if %ERRORLEVEL% neq 0 (
    echo ERROR: Publishing self-contained app for Linux failed.
    exit /b 1
)

echo.
echo ===== Build completed successfully =====
echo Output files are located in:
echo Source\Console\bin\%CONFIGURATION%\net8.0\linux-x64\publish
echo Source\IntuneAppBuilder\bin\%CONFIGURATION%

exit /b 0
