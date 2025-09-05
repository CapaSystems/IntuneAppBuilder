using System;
using System.IO;
using System.Threading.Tasks;
using IntuneAppBuilder.Domain;
using IntuneAppBuilder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Beta.Models;

namespace IntuneAppBuilder.Builders
{
    /// <summary>
    ///     Builds an app package from all files in a specified directory or from a single file.
    /// </summary>
    public class PathIntuneAppPackageBuilder : IIntuneAppPackageBuilder
    {
        private readonly IIntuneAppPackagingService packagingService;
        private readonly IServiceProvider serviceProvider;
        private readonly string path;

        public PathIntuneAppPackageBuilder(string path, IIntuneAppPackagingService packagingService, IServiceProvider serviceProvider = null)
        {
            Name = Path.GetFullPath(path);
            this.path = path;
            this.packagingService = packagingService;
            this.serviceProvider = serviceProvider;
        }

        public string Name { get; }

        public Task<IntuneAppPackage> BuildAsync(MobileLobApp app)
        {
            // Check if a specific setup file path was provided
            string setupFilePath = null;
            if (serviceProvider != null)
            {
                var setupFileInfo = serviceProvider.GetService<SetupFileInfo>();
                setupFilePath = setupFileInfo?.SetupFilePath;
            }

            return packagingService.BuildPackageAsync(path, setupFilePath);
        }
    }
}