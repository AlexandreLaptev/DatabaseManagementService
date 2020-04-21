using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DatabaseManagement
{
    public class Program
    {
        /// <summary>
        /// Main application endpoint.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddEnvironmentVariables("ASPNETCORE_")
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                // Uses the AddCommandLine() method that allows to specify the hosting environment at run time using the --environment argument:
                // > dotnet run --environment "Staging" or > dotnet run --environment="Production"
                .AddCommandLine(args);

            var config = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

            try
            {
                var webHostBuilder = new WebHostBuilder()
                   .UseSerilog()
                   .UseKestrel()
                   .UseContentRoot(Directory.GetCurrentDirectory())
                   .UseConfiguration(config)
                   .UseStartup<Startup>()
                   .ConfigureLogging((context, logging) =>
                   {
                       logging.AddSerilog();
                   });

                var host = webHostBuilder.Build();
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}