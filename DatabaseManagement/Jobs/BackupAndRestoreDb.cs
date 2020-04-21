using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MassTransit;
using DataManagement.MessageContracts;

namespace DatabaseManagement
{
    internal class BackupAndRestoreDb
    {
        public static async Task BackupDB(string databaseName, string backupFilePath, IConfiguration configuration, IPublishEndpoint publishEndpoint, ILogger<DatabaseUpgrater> logger)
        {
            logger.LogInformation("Backup database operation started...");
            await publishEndpoint.Publish<ProgressMessage>(new { Message = "Backup database operation started", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });

            ServerConnection connection = null;

            try
            {
                var connectionString = configuration.GetValue<string>("MasterConnection");

                // Define a Backup object variable
                Backup backup = new Backup();

                // Set type of backup to be performed to database
                backup.Action = BackupActionType.Database;
                backup.BackupSetDescription = "Full backup of " + databaseName;
                // Set the name used to identify a particular backup set
                backup.BackupSetName = databaseName + " Backup";
                // Specify the name of the database to back up
                backup.Database = databaseName;

                // Set up the backup device to use filesystem
                BackupDeviceItem deviceItem = new BackupDeviceItem(backupFilePath, DeviceType.File);
                // Add the device to the Backup object
                backup.Devices.Add(deviceItem);

                // Setup a new connection to the data server
                connection = new ServerConnection(new SqlConnection(connectionString));
                Server sqlServer = new Server(connection);

                // Initialize devices associated with a backup operation
                backup.Initialize = true;
                backup.Checksum = true;
                // Set it to true to have the process continue even after checksum error
                backup.ContinueAfterError = true;
                // Set the Incremental property to False to specify that this is a full database backup  
                backup.Incremental = false;
                // Set the backup expiration date
                backup.ExpirationDate = DateTime.Now.AddYears(1);
                // Specify that the log must be truncated after the backup is complete
                backup.LogTruncation = BackupTruncateLogType.Truncate;

                backup.PercentCompleteNotification = 10;

                backup.PercentComplete += async (s, e) =>
                {
                    // Inform the user percent complete
                    logger.LogInformation($"Percent Backup: {e.Percent}");
                    await publishEndpoint.Publish<ProgressMessage>(new { Message = $"Percent Backup: {e.Percent}", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });
                };

                // Run SqlBackup to perform the full database backup on the instance of SQL Server
                backup.SqlBackup(sqlServer);

                // Inform the user that the backup has been completed 
                logger.LogInformation("Backup database has been completed");
                await publishEndpoint.Publish<ProgressMessage>(new { Message = "Backup database has been completed", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });
            }
            finally
            {
                if (connection != null)
                    connection.Disconnect();
            }
        }

        public static async Task RestoreDB(string databaseName, string backupFilePath, IConfiguration configuration, IPublishEndpoint publishEndpoint, ILogger<DatabaseUpgrater> logger)
        {
            logger.LogInformation("Restore database operation started...");
            await publishEndpoint.Publish<ProgressMessage>(new { Message = "Restore database operation started", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });

            ServerConnection connection = null;

            try
            {
                var connectionString = configuration.GetValue<string>("MasterConnection");

                // Define a Restore object variable
                Restore restore = new Restore();

                // Specify the database name
                restore.Database = databaseName;
                restore.Action = RestoreActionType.Database;

                // Add the device that contains the full database backup to the Restore object         
                restore.Devices.AddDevice(backupFilePath, DeviceType.File);

                // Set ReplaceDatabase = true to create new database regardless of the existence of specified database
                restore.ReplaceDatabase = true;

                // Set the NoRecovery property to False
                // If you have a differential or log restore to be followed, you would specify NoRecovery = true
                restore.NoRecovery = false;

                restore.PercentCompleteNotification = 10;

                // Setup a new connection to the data server
                connection = new ServerConnection(new SqlConnection(connectionString));
                Server sqlServer = new Server(connection);

                restore.PercentComplete += async (s, e) =>
                {
                    // Inform the user percent restore
                    logger.LogInformation($"Percent Restore: {e.Percent}");
                    await publishEndpoint.Publish<ProgressMessage>(new { Message = $"Percent Restore: {e.Percent}", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });
                };

                // Restore the full database backup with recovery         
                restore.SqlRestore(sqlServer);

                var db = sqlServer.Databases[restore.Database];
                db.SetOnline();
                sqlServer.Refresh();
                db.Refresh();

                // Inform the user that the restore has been completed
                logger.LogInformation("Restore database has been completed");
                await publishEndpoint.Publish<ProgressMessage>(new {  Message = "Restore database has been completed", Status = ProgressStatus.Processing, Timestamp = DateTime.Now });
            }
            finally
            {
                if (connection != null)
                    connection.Disconnect();
            }
        }
    }
}