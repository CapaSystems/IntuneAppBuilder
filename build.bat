@echo off
echo Building IntuneAppBuilder solution...

:: Set environment variables
set SOLUTION_PATH=c:\dev\IntuneAppBuilder\IntuneAppBuilder.sln
set CONFIGURATION=Release
set MSBUILD_VERBOSITY=minimal

:: Check if solution exists
if not exist "%SOLUTION_PATH%" (
    echo ERROR: Solution file not found at %SOLUTION_PATH%
    exit /b 1
)

echo.
echo ===== Restoring NuGet packages =====
dotnet restore "%SOLUTION_PATH%" --verbosity %MSBUILD_VERBOSITY%
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to restore NuGet packages.
    exit /b 1
)

echo.
echo ===== Building specific projects (skipping tests) =====
:: Set a fixed version now that GitVersion is disabled
set FIXED_VERSION=4.0.0

:: Building only the specific projects we need, not the entire solution with tests
dotnet build "c:\dev\IntuneAppBuilder\Source\IntuneAppBuilder\IntuneAppBuilder.csproj" -p:Version=%FIXED_VERSION% --configuration %CONFIGURATION% --verbosity %MSBUILD_VERBOSITY%
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build of IntuneAppBuilder project failed.
    exit /b 1
)

dotnet build "c:\dev\IntuneAppBuilder\Source\Console\Console.csproj" -p:Version=%FIXED_VERSION% --configuration %CONFIGURATION% --verbosity %MSBUILD_VERBOSITY% 
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build of Console project failed.
    exit /b 1
)

echo.
echo ===== Tests explicitly skipped =====

echo.
echo ===== Build completed successfully =====
echo Output files are located in:
echo c:\dev\IntuneAppBuilder\Source\Console\bin\%CONFIGURATION%
echo c:\dev\IntuneAppBuilder\Source\IntuneAppBuilder\bin\%CONFIGURATION%

exit /b 0
