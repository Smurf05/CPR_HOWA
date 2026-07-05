using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace HOWA.Shared.Configuration
{
    /// <summary>
    /// Loads appsettings.json and exposes a pre-built <see cref="IConfiguration"/> instance.
    /// The JSON stream is provided by the caller (MAUI app) so that this library
    /// does not take a direct dependency on Microsoft.Maui.Essentials.
    /// </summary>
    public static class AppConfiguration
    {
        /// <summary>
        /// Builds an <see cref="IConfiguration"/> from the supplied JSON stream.
        /// Pass <c>null</c> to get an empty configuration (safe fallback).
        /// </summary>
        public static IConfiguration Build(Stream? jsonStream = null)
        {
            var builder = new ConfigurationBuilder();

            if (jsonStream != null)
            {
                builder.AddJsonStream(jsonStream);
            }

            return builder.Build();
        }

        /// <summary>
        /// Returns the default database connection string from configuration,
        /// falling back to the hardcoded <see cref="Constants.DbConstants.DefaultConnectionString"/>.
        /// </summary>
        public static string GetConnectionString(IConfiguration configuration, string name = "DefaultConnection")
            => configuration.GetConnectionString(name)
               ?? Constants.DbConstants.DefaultConnectionString;
    }
}
