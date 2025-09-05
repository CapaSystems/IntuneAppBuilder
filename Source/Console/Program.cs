using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IntuneAppBuilder.Builders;
using IntuneAppBuilder.Domain;
using IntuneAppBuilder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Serialization.Json;

// Use specific namespaces to avoid ambiguity
using FileSystemInfo = System.IO.FileSystemInfo;
using Microsoft.Graph.Beta.Models;
using Command = System.CommandLine.Command;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace IntuneAppBuilder.Console
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            if (!args.Any()) args = new[] { "--help" };

            var pack = new Command("pack")
            {
                new Option<FileSystemInfo[]>(new[] { "--source", "-s" },
                        "Specifies a source to package. May be a directory with files for a Win32 app or a single msi file. May be specified multiple times.")
                    { Name = "sources", IsRequired = true },
                new Option<string>(new[] { "--output", "-o" }, () => ".",
                    "Specifies an output directory for packaging artifacts. Each packaged application will exist as a raw intunewin file, a portal-ready portal.intunewin file, and an intunewin.json file containing metadata. Defaults to the working directory."),
                new Option<string>(new[] { "--setupFilePath" },
                    "Specifies the main setup file to use within the source directory. If not specified, the tool will automatically select an .msi or .exe file."),
                // Add MSI properties as optional parameters for non-Windows platforms
                new Option<string>("--msiProductName",
                    "Specifies the MSI product name for non-Windows platforms"),
                new Option<string>("--msiProductCode",
                    "Specifies the MSI product code for non-Windows platforms"),
                new Option<string>("--msiProductVersion",
                    "Specifies the MSI product version for non-Windows platforms"),
                new Option<string>("--msiUpgradeCode",
                    "Specifies the MSI upgrade code for non-Windows platforms"),
                new Option<string>("--msiPublisher",
                    "Specifies the MSI publisher for non-Windows platforms"),
                new Option<string>("--msiPackageType",
                    "Specifies the MSI package type for non-Windows platforms (PerUser, PerMachine, or DualPurpose)"),
                new Option<bool>("--msiRequiresReboot", () => false,
                    "Specifies whether the MSI requires a reboot for non-Windows platforms"),
                new Option<string>("--msiPackageCode",
                    "Specifies the MSI package code for non-Windows platforms"),
                new Option<bool>("--msiRequiresLogon", () => false,
                    "Specifies whether the MSI requires logon for non-Windows platforms"),
                new Option<bool>("--msiIncludesServices", () => false,
                    "Specifies whether the MSI includes services for non-Windows platforms"),
                new Option<bool>("--msiIncludesOdbcDataSource", () => false,
                    "Specifies whether the MSI includes ODBC data sources for non-Windows platforms"),
                new Option<bool>("--msiContainsSystemRegistryKeys", () => false,
                    "Specifies whether the MSI contains system registry keys for non-Windows platforms"),
                new Option<bool>("--msiContainsSystemFolders", () => false,
                    "Specifies whether the MSI contains system folders for non-Windows platforms")
            };
#pragma warning disable S3011
            pack.Handler = CommandHandler.Create<FileSystemInfo[], string, string, MsiCommandLineOptions>(
                (sources, output, setupFilePath, msiOptions) => PackAsync(sources, output, setupFilePath, msiOptions));
#pragma warning restore S3011

            var publish = new Command("publish")
            {
                new Option<string[]>(new[] { "--source", "-s" },
                        "Specifies a source to publish. May be a directory with *.intunewin.json files or a single json file")
                    { Name = "sources", IsRequired = true },
                new Option<string>(new[] { "--token", "-t" },
                        "Specifies an access token to use when publishing.")
                    { Name = "token", IsRequired = false }
            };
#pragma warning disable S3011
            publish.Handler = CommandHandler.Create(typeof(Program).GetMethod(nameof(PublishAsync), BindingFlags.Static | BindingFlags.NonPublic)!);
#pragma warning restore S3011

            var root = new RootCommand
            {
                pack,
                publish
            };
            root.TreatUnmatchedTokensAsErrors = true;

            return await root.InvokeAsync(args);
        }

        internal static IServiceCollection GetServices(string token = null)
        {
            var services = new ServiceCollection();
            services.AddIntuneAppBuilder(token);
            services.AddLogging(builder =>
            {
                // don't write info for HttpClient
                builder.AddFilter((category, level) => category.StartsWith("System.Net.Http.HttpClient") ? level >= LogLevel.Warning : level >= LogLevel.Information);
                builder.AddConsole();
            });
            return services;
        }

        internal static async Task PackAsync(
            FileSystemInfo[] sources, 
            string output,
            string setupFilePath = null,
            MsiCommandLineOptions msiOptions = null)
        {
            var services = GetServices();

            output = Path.GetFullPath(output);

            // Register MSI manual properties if any are provided
            RegisterMsiPropertiesIfProvided(services, msiOptions);

            AddBuilders(sources, services, setupFilePath);

            var sp = services.BuildServiceProvider();
            foreach (var builder in sp.GetRequiredService<IEnumerable<IIntuneAppPackageBuilder>>()) await BuildAsync(builder, sp.GetRequiredService<IIntuneAppPackagingService>(), output, GetLogger(sp));
        }

        private static Win32LobAppMsiPackageType? ParsePackageType(string packageType)
        {
            return packageType?.ToLowerInvariant() switch
            {
                "peruser" => Win32LobAppMsiPackageType.PerUser,
                "permachine" => Win32LobAppMsiPackageType.PerMachine,
                "dualpurpose" => Win32LobAppMsiPackageType.DualPurpose,
                _ => null
            };
        }

        internal static async Task PublishAsync(FileSystemInfo[] sources, string token = null, IServiceCollection services = null)
        {
            if (token != null && services != null)
            {
                throw new ArgumentException($"Cannot specify both {nameof(token)} and {nameof(services)}.");
            }

            services ??= GetServices(token);
            var sp = services.BuildServiceProvider();
            var publishingService = sp.GetRequiredService<IIntuneAppPublishingService>();
            var logger = GetLogger(sp);

            var sourceFiles = new List<FileInfo>(sources.OfType<FileInfo>());
            sourceFiles.AddRange(sources.OfType<DirectoryInfo>().SelectMany(di => di.EnumerateFiles("*.intunewin.json", SearchOption.AllDirectories)));
            foreach (var file in sourceFiles)
            {
                using var package = ReadPackage(file, logger);
                await publishingService.PublishAsync(package);
            }
        }

        /// <summary>
        ///     Registers the correct builder type for each source from the command line.
        /// </summary>
        /// <param name="sources">The source files or directories to package</param>
        /// <param name="services">The service collection to add builders to</param>
        /// <param name="setupFilePath">Optional path to the main setup file</param>
        private static void AddBuilders(IEnumerable<FileSystemInfo> sources, IServiceCollection services, string setupFilePath = null)
        {
            foreach (var source in sources)
            {
                if (!source.Exists) throw new InvalidOperationException($"{source.FullName} does not exist.");

                if (source.Extension.Equals(".msi", StringComparison.OrdinalIgnoreCase) || source is DirectoryInfo)
                {
                    // Register the setup file path if provided
                    if (!string.IsNullOrEmpty(setupFilePath))
                    {
                        services.AddSingleton(new SetupFileInfo { SetupFilePath = setupFilePath });
                    }
                    
                    services.AddSingleton<IIntuneAppPackageBuilder>(sp => 
                    {
                        var packagingService = sp.GetRequiredService<IIntuneAppPackagingService>();
                        var builder = new PathIntuneAppPackageBuilder(source.FullName, packagingService, sp);
                        return builder;
                    });
                }
                else
                {
                    throw new InvalidOperationException($"{source} is not a supported packaging source.");
                }
            }
        }

        /// <summary>
        ///     Invokes the builder in a dedicated working directory.
        /// </summary>
        private static async Task BuildAsync(IIntuneAppPackageBuilder builder, IIntuneAppPackagingService packagingService, string output, ILogger logger)
        {
            var cd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = output;
            try
            {
                var package = await builder.BuildAsync(null);
                package.Data.Position = 0;

                var baseFileName = Path.GetFileNameWithoutExtension(package.App.FileName);

                using (var jsonSerializerWriter = new JsonSerializationWriter())
                {
                    jsonSerializerWriter.WriteObjectValue(string.Empty, package);
                    var serializedStream = jsonSerializerWriter.GetSerializedContent();
                    using (var reader = new StreamReader(serializedStream, Encoding.UTF8))
                    {
                        var packageJsonString = await reader.ReadToEndAsync();
                        File.WriteAllText($"{baseFileName}.intunewin.json", packageJsonString);
                    }
                }

                await using (var fs = File.Open($"{baseFileName}.intunewin", FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await package.Data.CopyToAsync(fs);
                }

                await using (var fs = File.Open($"{baseFileName}.portal.intunewin", FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await packagingService.BuildPackageForPortalAsync(package, fs);
                }

                logger.LogInformation($"Finished writing {baseFileName} package files to {output}.");
            }
            finally
            {
                Environment.CurrentDirectory = cd;
            }
        }

        private static ILogger GetLogger(IServiceProvider sp) => sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Program));

        private static IntuneAppPackage ReadPackage(FileInfo file, ILogger logger)
        {
            logger.LogInformation($"Loading package from file {file.FullName}.");

            var jsonParseNode = new JsonParseNode(JsonDocument.Parse(File.ReadAllText(file.FullName)).RootElement);
            var package = jsonParseNode.GetObjectValue(IntuneAppPackage.CreateFromDiscriminatorValue);
            var dataPath = Path.Combine(file.DirectoryName!, Path.GetFileNameWithoutExtension(file.FullName));
            if (!File.Exists(dataPath)) throw new FileNotFoundException($"Could not find data file at {dataPath}.");
            logger.LogInformation($"Using package data file {dataPath}");
            package!.Data = File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return package;
        }

        private static void RegisterMsiPropertiesIfProvided(
            IServiceCollection services,
            MsiCommandLineOptions options)
        {
            // Check if any MSI properties are provided
            if (HasAnyMsiProperties(options))
            {
                var properties = CreateMsiManualProperties(options);
                // Register with the explicit type to avoid type resolution issues
                services.AddSingleton(typeof(IntuneAppBuilder.Domain.MsiManualProperties), properties);
                System.Console.WriteLine($"MSI properties registered with service collection: ProductName={properties.ProductName}, ProductCode={properties.ProductCode}");
            }
        }
        
        private static bool HasAnyMsiProperties(MsiCommandLineOptions options)
        {
            if (options == null)
                return false;
                
            return HasAnyStringProperties(options) || HasAnyBooleanProperties(options);
        }

        private static bool HasAnyStringProperties(MsiCommandLineOptions options)
        {
            // Check basic product properties first
            if (!string.IsNullOrEmpty(options.MsiProductName) || 
                !string.IsNullOrEmpty(options.MsiProductCode) || 
                !string.IsNullOrEmpty(options.MsiProductVersion))
            {
                return true;
            }
            
            // Check additional product metadata
            if (!string.IsNullOrEmpty(options.MsiUpgradeCode) || 
                !string.IsNullOrEmpty(options.MsiPublisher))
            {
                return true;
            }
            
            // Check package-specific properties
            return !string.IsNullOrEmpty(options.MsiPackageType) || 
                   !string.IsNullOrEmpty(options.MsiPackageCode);
        }

        private static bool HasAnyBooleanProperties(MsiCommandLineOptions options)
        {
            // Check installation requirements
            if (options.MsiRequiresReboot || options.MsiRequiresLogon)
            {
                return true;
            }
            
            // Check package content properties
            if (options.MsiIncludesServices || options.MsiIncludesOdbcDataSource)
            {
                return true;
            }
            
            // Check system impact properties
            return options.MsiContainsSystemRegistryKeys || 
                   options.MsiContainsSystemFolders;
        }
        
        private static IntuneAppBuilder.Domain.MsiManualProperties CreateMsiManualProperties(MsiCommandLineOptions options)
        {
            if (options == null)
                return null;
                
            var properties = new IntuneAppBuilder.Domain.MsiManualProperties
            {
                ProductName = options.MsiProductName,
                ProductCode = options.MsiProductCode,
                ProductVersion = options.MsiProductVersion,
                UpgradeCode = options.MsiUpgradeCode,
                Publisher = options.MsiPublisher,
                PackageType = options.MsiPackageType != null ? ParsePackageType(options.MsiPackageType) : null,
                RequiresReboot = options.MsiRequiresReboot ? (bool?)true : null,
                PackageCode = options.MsiPackageCode,
                RequiresLogon = options.MsiRequiresLogon ? (bool?)true : null,
                IncludesServices = options.MsiIncludesServices ? (bool?)true : null,
                IncludesOdbcDataSource = options.MsiIncludesOdbcDataSource ? (bool?)true : null,
                ContainsSystemRegistryKeys = options.MsiContainsSystemRegistryKeys ? (bool?)true : null,
                ContainsSystemFolders = options.MsiContainsSystemFolders ? (bool?)true : null
            };
            
            
            return properties;
        }
    }
}