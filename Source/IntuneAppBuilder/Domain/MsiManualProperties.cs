using Microsoft.Graph.Beta.Models;

namespace IntuneAppBuilder.Domain
{
    /// <summary>
    /// Class to hold manually provided MSI properties when running on non-Windows platforms
    /// </summary>
    public class MsiManualProperties
    {
        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Gets or sets the product code.
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// Gets or sets the product version.
        /// </summary>
        public string ProductVersion { get; set; }

        /// <summary>
        /// Gets or sets the upgrade code.
        /// </summary>
        public string UpgradeCode { get; set; }

        /// <summary>
        /// Gets or sets the publisher.
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// Gets or sets the package type.
        /// </summary>
        public Win32LobAppMsiPackageType? PackageType { get; set; }

        /// <summary>
        /// Gets or sets whether a reboot is required.
        /// </summary>
        public bool? RequiresReboot { get; set; }

        /// <summary>
        /// Gets or sets the package code.
        /// </summary>
        public string PackageCode { get; set; }

        /// <summary>
        /// Gets or sets whether logon is required.
        /// </summary>
        public bool? RequiresLogon { get; set; }

        /// <summary>
        /// Gets or sets whether the MSI includes services.
        /// </summary>
        public bool? IncludesServices { get; set; }

        /// <summary>
        /// Gets or sets whether the MSI includes ODBC data sources.
        /// </summary>
        public bool? IncludesOdbcDataSource { get; set; }

        /// <summary>
        /// Gets or sets whether the MSI contains system registry keys.
        /// </summary>
        public bool? ContainsSystemRegistryKeys { get; set; }

        /// <summary>
        /// Gets or sets whether the MSI contains system folders.
        /// </summary>
        public bool? ContainsSystemFolders { get; set; }

        /// <summary>
        /// Checks if any properties have been set
        /// </summary>
        /// <returns>True if at least one property has a value</returns>
        public bool HasValues()
        {
            return HasAnyStringValues() || HasAnyNullableValues();
        }
        
        private bool HasAnyStringValues()
        {
            // Check string properties
            string[] properties = new[]
            {
                ProductName,
                ProductCode,
                ProductVersion,
                UpgradeCode,
                Publisher,
                PackageCode
            };
            
            foreach (var prop in properties)
            {
                if (!string.IsNullOrEmpty(prop))
                    return true;
            }
            
            return false;
        }
        
        private bool HasAnyNullableValues()
        {
            return PackageType.HasValue || HasAnyBooleanValues();
        }

        private bool HasAnyBooleanValues()
        {
            if (RequiresReboot.HasValue || RequiresLogon.HasValue)
                return true;
                
            if (IncludesServices.HasValue || IncludesOdbcDataSource.HasValue)
                return true;
                
            return ContainsSystemRegistryKeys.HasValue || ContainsSystemFolders.HasValue;
        }
    }
}
