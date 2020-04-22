using System;
using System.IO;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Hangfire;
using MassTransit;
using DataManagement.MessageContracts;

namespace DatabaseManagement
{
    public class DatabaseUpgrater
    {
        private readonly IConfiguration _configuration;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<DatabaseUpgrater> _logger;
        private readonly CancellationToken _cancellationToken;

        public DatabaseUpgrater(IConfiguration configuration, IHostApplicationLifetime applicationLifetime, IPublishEndpoint publishEndpoint, ILogger<DatabaseUpgrater> logger)
        {
            _configuration = configuration;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
            _cancellationToken = applicationLifetime.ApplicationStopping;
        }

        [AutomaticRetry(Attempts = 0)] // If you don’t want a job to be retried, place an explicit attribute with 0 maximum retry attempts value
        public async Task PerformAsync()
        {
            var builder = new SqlConnectionStringBuilder(_configuration.GetValue<string>("NorthwindConnection"));
            var databaseName = builder.InitialCatalog;

            var backupDirectory = _configuration.GetValue<string>("BackupDirectory");
            string backupFilePath = string.Empty;

            try
            {
                var updateRequired = DeployDbChanges.IsUpgradeRequired(_configuration);

                _cancellationToken.ThrowIfCancellationRequested();

                if (updateRequired)
                {
                    if (!Directory.Exists(backupDirectory))
                    {
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            backupDirectory = backupDirectory.Replace("\\", "//");

                        Directory.CreateDirectory(backupDirectory);
                    }

                    var backupFileName = $"Northwind_{ DateTime.Now:yyyy-MM-dd-HH-mm-ss}.bak";
                    backupFilePath = Path.Combine(backupDirectory, backupFileName);
                    backupFilePath = Path.GetFullPath(backupFilePath);

                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        backupFilePath = backupFilePath.Replace("\\", "//");

                    _cancellationToken.ThrowIfCancellationRequested();

                    // Backup database
                    await BackupAndRestoreDb.BackupDB(databaseName, backupFilePath, _configuration, _publishEndpoint, _logger);

                    _cancellationToken.ThrowIfCancellationRequested();

                    // Check if backup file created
                    if (!File.Exists(backupFilePath))
                        throw new Exception($"Backup file '{backupFilePath}' has not been created.");

                    // Perform database upgrade
                    await DeployDbChanges.PerformUpgrade(_configuration, _publishEndpoint, _logger);

                    // Message of completion
                    _logger.LogInformation("Database upgrade is completed.");
                    await _publishEndpoint.Publish<UpdateProgress>(new { Message = "Database upgrade is completed", Status = ProgressStatus.Completed, Timestamp = DateTime.Now });
                    await _publishEndpoint.Publish<UpdateCompleted>(new {});
                }
                else
                {
                    _logger.LogInformation("Upgrade is not required.");
                    await _publishEndpoint.Publish<UpdateProgress>(new { Message = "Upgrade is not required", Status = ProgressStatus.NotRequired, Timestamp = DateTime.Now });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                await _publishEndpoint.Publish<UpdateProgress>(new { ex.Message, Status = ProgressStatus.Error, Timestamp = DateTime.Now });

                if (!_cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(backupFilePath) && File.Exists(backupFilePath))
                        {
                            // Restore databse
                            await BackupAndRestoreDb.RestoreDB(databaseName, backupFilePath, _configuration, _publishEndpoint, _logger);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                }
            }

            // Remove the backup files from the hard disk
            if (!string.IsNullOrEmpty(backupFilePath) && File.Exists(backupFilePath))
                File.Delete(backupFilePath);

            await Task.CompletedTask;
        }
    }
}