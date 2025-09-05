using Microsoft.Graph.Beta.Models;

namespace IntuneAppBuilder.Console
{
    /// <summary>
    /// Command line options for MSI properties
    /// </summary>
    internal class MsiCommandLineOptions
    {
        /// <summary>
        /// Gets or sets the MSI product name
        /// </summary>
        public string MsiProductName { get; set; }

        /// <summary>
        /// Gets or sets the MSI product code
        /// </summary>
        public string MsiProductCode { get; set; }

        /// <summary>
        /// Gets or sets the MSI product version
        /// </summary>
        public string MsiProductVersion { get; set; }

        /// <summary>
        /// Gets or sets the MSI upgrade code
        /// </summary>
        public string MsiUpgradeCode { get; set; }

        /// <summary>
        /// Gets or sets the MSI publisher
        /// </summary>
        public string MsiPublisher { get; set; }

        /// <summary>
        /// Gets or sets the MSI package type
        /// </summary>
        public string MsiPackageType { get; set; }

        /// <summary>
        /// Gets or sets whether the MSI requires a reboot
        /// </summary>
        public bool MsiRequiresReboot { get; set; } 
        /// <summary>
        /// Gets or sets the MSI package code
        /// </summary>
        public string MsiPackageCode { get; set; }

        /// <summary>
        /// Gets or sets whether the MSI requires logon
        /// </summary>
        public bool MsiRequiresLogon { get; set; }

        /// <summary>
        /// Gets or sets whether the MSI includes services
        /// </summary>
        public bool MsiIncludesServices { get; set; }

        /// <summary>
        /// Gets or sets whether the MSI includes ODBC data sources
        /// </summary>
        public bool MsiIncludesOdbcDataSource { get; set; } 

        /// <summary>
        /// Gets or sets whether the MSI contains system registry keys
        /// </summary>
        public bool MsiContainsSystemRegistryKeys { get; set; }

        /// <summary>
        /// Gets or sets whether the MSI contains system folders
        /// </summary>
        public bool MsiContainsSystemFolders { get; set; }
    }
}
