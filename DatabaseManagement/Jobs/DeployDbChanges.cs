using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DbUp;
using DbUp.ScriptProviders;
using MassTransit;
using DataManagement.MessageContracts;

namespace DatabaseManagement
{
    internal class DeployDbChanges
    {
        public static async Task PerformUpgrade(IConfiguration configuration, IPublishEndpoint publishEndpoint, ILogger<DatabaseUpgrater> logger)
        {
            await PerformSchemaUpgrade(configuration, publishEndpoint, logger);
            await PerformDataUpgrade(configuration, publishEndpoint, logger);
        }

        public static async Task PerformSchemaUpgrade(IConfiguration configuration, IPublishEndpoint publishEndpoint, ILogger<DatabaseUpgrater> logger)
        {
            string scriptsPath = GetScriptsDirectory("Schema", configuration);

            if (Directory.GetFiles(scriptsPath, "*.sql", SearchOption.TopDirectoryOnly).Length > 0)
            {
                logger.LogInformation("Performing schema upgrade started...");
                await publishEndpoint.Publish<ProgressMessage>(new { Message = "Performing schema upgrade started", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });

                var connectionString = configuration["NorthwindConnection"];

                var scriptsExecutor =
                DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithScriptsFromFileSystem
                    (
                        scriptsPath,
                        new FileSystemScriptOptions { IncludeSubDirectories = false }
                    )
                    .WithTransaction() // apply all changes in a single transaction
                    .LogToConsole();

                var upgrader = scriptsExecutor.Build();

                //Check if an upgrade is required
                if (upgrader.IsUpgradeRequired())
                {
                    var upgradeResult = upgrader.PerformUpgrade();

                    if (upgradeResult.Successful)
                    {
                        logger.LogInformation("Database schema upgraded successfully.");
                        await publishEndpoint.Publish<ProgressMessage>(new { Message = "Database schema upgraded successfully", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });
                    }
                    else
                    {
                        var message = "Error performing schema upgrade.";
                        logger.LogError(message);
                        throw new Exception($"{message}: ", upgradeResult.Error);
                    }
                }
                else
                {
                    logger.LogInformation("Schema upgrade is not required.");
                    await publishEndpoint.Publish<ProgressMessage>(new { Message = "Schema upgrade is not required", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });
                }
            }
        }

        public static async Task PerformDataUpgrade(IConfiguration configuration, IPublishEndpoint publishEndpoint, ILogger<DatabaseUpgrater> logger)
        {
            string scriptsPath = GetScriptsDirectory("Data", configuration);

            if (Directory.GetFiles(scriptsPath, "*.sql", SearchOption.TopDirectoryOnly).Length > 0)
            {
                logger.LogInformation("Performing data upgrade started...");
                await publishEndpoint.Publish<ProgressMessage>(new { Message = "Performing data upgrade started", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });

                var connectionString = configuration["NorthwindConnection"];

                var scriptsExecutor =
                    DeployChanges.To
                        .SqlDatabase(connectionString)
                        .WithScriptsFromFileSystem
                        (
                            scriptsPath,
                            new FileSystemScriptOptions { IncludeSubDirectories = false }
                        )
                        .WithTransaction() // apply all changes in a single transaction
                        .LogToConsole();

                var upgrader = scriptsExecutor.Build();

                // Check if an upgrade is required
                if (upgrader.IsUpgradeRequired())
                {
                    var upgradeResult = upgrader.PerformUpgrade();

                    if (upgradeResult.Successful)
                    {
                        logger.LogInformation("Database data upgraded successfully.");
                        await publishEndpoint.Publish<ProgressMessage>(new { Message = "Database data upgraded successfully", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });
                    }
                    else
                    {
                        var message = "Error performing data upgrade.";
                        logger.LogError(message);
                        throw new Exception($"{message}: ", upgradeResult.Error);
                    }
                }
                else
                {
                    logger.LogInformation("Data update is not required.");
                    await publishEndpoint.Publish<ProgressMessage>(new { Message = "Data update is not required", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });
                }
            }
        }

        public static bool IsUpgradeRequired(IConfiguration configuration)
        {
            var updateRequired = IsSchemaUpgradeRequired(configuration);

            if (updateRequired)
                return true;

            return IsDataUpgradeRequired(configuration);
        }

        public static bool IsSchemaUpgradeRequired(IConfiguration configuration)
        {
            string scriptsPath = GetScriptsDirectory("Schema", configuration);

            if (Directory.GetFiles(scriptsPath, "*.sql", SearchOption.TopDirectoryOnly).Length > 0)
            {
                var connectionString = configuration["NorthwindConnection"];

                var scriptsExecutor =
                DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithScriptsFromFileSystem
                    (
                        scriptsPath,
                        new FileSystemScriptOptions { IncludeSubDirectories = false }
                    )
                    .WithTransaction() // apply all changes in a single transaction
                    .LogToConsole();

                var upgrader = scriptsExecutor.Build();
                return upgrader.IsUpgradeRequired();
            }

            return false;
        }

        public static bool IsDataUpgradeRequired(IConfiguration configuration)
        {
            string scriptsPath = GetScriptsDirectory("Data", configuration);

            if (Directory.GetFiles(scriptsPath, "*.sql", SearchOption.TopDirectoryOnly).Length > 0)
            {
                var connectionString = configuration["NorthwindConnection"];

                var dataScriptsExecutor =
                    DeployChanges.To
                        .SqlDatabase(connectionString)
                        .WithScriptsFromFileSystem
                        (
                            scriptsPath,
                            new FileSystemScriptOptions { IncludeSubDirectories = false }
                        )
                        .WithTransaction() // apply all changes in a single transaction
                        .LogToConsole();

                var upgrader = dataScriptsExecutor.Build();
                return upgrader.IsUpgradeRequired();
            }

            return false;
        }

        private static string GetScriptsDirectory(string subDirName, IConfiguration configuration)
        {
            var scriptsDirectory = configuration["ScriptsDirectory"];
            var scriptsPath = Path.GetFullPath(scriptsDirectory);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                scriptsPath = scriptsPath.Replace("\\", "//");

            if (!Directory.Exists(scriptsPath))
                throw new MissingFieldException($"Error: database scripts directoty '{scriptsPath}' not found.");

            string subDirectoryPath = Path.Combine(scriptsDirectory, subDirName);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                subDirectoryPath = subDirectoryPath.Replace("\\", "//");

            return subDirectoryPath;
        }
    }
}