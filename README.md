te# IntuneAppBuilder

Package MSI and Win32 application packages as .intunewin format to Microsoft Intune with this cross-platform tool.

## Overview

Use the Simeon IntuneAppBuilder tool to create and deploy Microsoft Intune packages for MSI and Win32 applications. The
tool converts installation files into the .intunewin format that can then be published using the tool or uploaded
manually into the Intune Portal.

IntuneAppBuilder is an open source component from the Simeon Microsoft 365 Management toolset. Learn more about Simeon’s
full functionality at https://simeoncloud.com.

[![Build Status](https://github.com/simeoncloud/IntuneAppBuilder/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/simeoncloud/IntuneAppBuilder/actions/workflows/ci.yml?query=branch%3Amaster)

## Getting Started

1. **[Get .NET Core 3.1 SDK](https://dotnet.microsoft.com/download)** (or higher)

2. **Install** from an elevated command prompt

```
dotnet tool install -g IntuneAppBuilder.Console
IntuneAppBuilder [args]
```

3. **Run** from a command prompt to print usage instructions

```
IntuneAppBuilder 

Usage:
  IntuneAppBuilder [options] [command]

Options:
  --version         Show version information
  -?, -h, --help    Show help and usage information

Commands:
  pack
  publish

```

The tool can ```pack``` your app or ```publish``` an app you have previously packaged.

4. **Package** an app

```
IntuneAppBuilder pack --source .\MyAppInstallFiles --output .\MyAppPackage
```

By default, the tool automatically selects the main file to use as the setup file (first .msi or .exe found). 
If you need to specify a specific setup file, you can use the `--setupFilePath` parameter:

```
IntuneAppBuilder pack --source .\MyAppInstallFiles --output .\MyAppPackage --setupFilePath .\MyAppInstallFiles\setup.exe
```

You should see 3 files in the output folder:

- MyAppInstallFiles.intunewin.json - this file contains metadata about the packaged app
- MyAppInstallFiles.intunewin - this file can be used directly to publish the app using
  the ```IntuneAppBuilder publish``` command
- MyAppInstallFiles.portal.intunewin - this file can be uploaded to the Intune Portal as a Win32 app

5. **Publish** an app

```
IntuneAppBuilder publish --source .\MyAppPackage\MyAppInstallFiles.intunewin.json
```

IntuneAppBuilder will publish the app content. You will be prompted to sign in to your tenant when publishing.

6. **Configure**

After publishing, you can find the app in your Intune portal and make any required changes (assigning, updating the
command line, detection rules, etc.).

## MSI Properties

The Windows Installer COM service is used to retrieve information about MSIs if one is included in your application.
When the tool is running on non-Windows systems, you can manually provide the MSI properties using command-line parameters:

```
IntuneAppBuilder.Console pack --source .\MyAppInstallFiles --output .\MyAppPackage --msiProductName "My App" --msiProductCode "{12345678-1234-1234-1234-123456789012}" --msiProductVersion "1.0.0" --msiUpgradeCode "{87654321-4321-4321-4321-210987654321}" --msiPublisher "My Company" --msiPackageType "PerMachine" --msiRequiresReboot
```

These MSI parameters are optional and only required when:
- Running on non-Windows platforms where MSI metadata can't be automatically extracted
- You want to override the MSI metadata that would be automatically detected

When running on Linux, these parameters can be retrieved using msiinfo from msitools, and provided to the tool.

### Available Parameters

Here's a summary of the key parameters:

- `--setupFilePath`: Specifies the main setup file to use within the source directory. If not specified, the tool will automatically select an .msi or .exe file.

### Available MSI Parameters

Here's a summary of all supported MSI parameters:

- `--msiProductName`: The name of the product
- `--msiProductCode`: The MSI product code (GUID)
- `--msiProductVersion`: The product version
- `--msiUpgradeCode`: The upgrade code (GUID) 
- `--msiPublisher`: The publisher name
- `--msiPackageType`: The package type (PerUser, PerMachine, or DualPurpose)
- `--msiRequiresReboot`: Flag indicating if the MSI requires a reboot
- `--msiPackageCode`: The MSI package code (GUID)
- `--msiRequiresLogon`: Flag indicating if the MSI requires logon
- `--msiIncludesServices`: Flag indicating if the MSI includes services
- `--msiIncludesOdbcDataSource`: Flag indicating if the MSI includes ODBC data sources
- `--msiContainsSystemRegistryKeys`: Flag indicating if the MSI contains system registry keys
- `--msiContainsSystemFolders`: Flag indicating if the MSI contains system folders

## Authentication

IntuneAppBuilder uses
the [device code flow](https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token-device-code-flow)
to authenticate by default. Optionally, an access token may be provided as a parameter to the publish command instead:

```
IntuneAppBuilder publish --source .\MyAppPackage\MyAppInstallFiles.intunewin.json --token <token>
```

## Dependencies

- **Package Name**: [xunit](https://github.com/xunit/xunit) and [xunit.runner.visualstudio](https://github.com/xunit/visualstudio.xunit)
  - **Version**: 2.4.0
  - **Author**: xunit
  - **License**: [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)
