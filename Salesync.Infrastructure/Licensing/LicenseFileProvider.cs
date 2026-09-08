using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Salesync.Application.Common.Licensing;

namespace Salesync.Infrastructure.Licensing
{
    public class LicenseFileProvider : ILicenseFileProvider
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public LicenseFileProvider(
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public SignedLicenseDocument Load()
        {
            var configuredPath =
                _configuration["License:FilePath"];

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                throw new InvalidOperationException(
                    "Salesync license file path is not configured.");
            }

            var fullPath =
                Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.Combine(
                        _environment.ContentRootPath,
                        configuredPath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    "Salesync license file was not found.",
                    fullPath);
            }

            var json =
                File.ReadAllText(fullPath);

            var document =
                JsonSerializer.Deserialize<SignedLicenseDocument>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (document is null)
            {
                throw new InvalidOperationException(
                    "Salesync license file is invalid.");
            }

            if (document.Payload is null)
            {
                throw new InvalidOperationException(
                    "Salesync license payload is missing.");
            }

            if (string.IsNullOrWhiteSpace(
                    document.Signature))
            {
                throw new InvalidOperationException(
                    "Salesync license signature is missing.");
            }

            return document;
        }
    }
}