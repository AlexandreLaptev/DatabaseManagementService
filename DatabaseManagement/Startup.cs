using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.SQLite;
using Serilog;
using MassTransit;
using DataManagement.MessageContracts;

namespace DatabaseManagement
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        ILogger<Startup> Logger { get; }

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            Logger = logger;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetValue<string>("HangfireConnection");
            services.CreateHangfireContext(connectionString, Logger);

            services.AddControllers();
            services.AddRazorPages();

            // Add Hangfire services
            services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
                config.UseSQLiteStorage(connectionString, new SQLiteStorageOptions());
            });

            // Fill RabbitMQConfig object instance that has values set from the configuration file
            var rabbitMQConfig = new RabbitMQConfig();
            Configuration.Bind("spring:rabbitmq", rabbitMQConfig);
            services.AddSingleton(rabbitMQConfig);

            // Configure MassTransit
            if (!string.IsNullOrEmpty(rabbitMQConfig.Host))
            {
                // Create the bus using RabbitMQ bus
                var rabbitMQBus = Bus.Factory.CreateUsingRabbitMq(busFactoryConfig =>
                {
                    var virtualHost = (!string.IsNullOrEmpty(rabbitMQConfig.VirtualHost)) ? rabbitMQConfig.VirtualHost : "/";

                    // Specify the messages to be sent to a specific topics (exchanges)
                    busFactoryConfig.Message<UpdateProgress>(configTopology => configTopology.SetEntityName("database.updates.progress"));
                    busFactoryConfig.Message<UpdateCompleted>(configTopology => configTopology.SetEntityName("database.updates.completed"));

                    var host = busFactoryConfig.Host(rabbitMQConfig.Host, virtualHost, h =>
                    {
                        h.Username(rabbitMQConfig.Username);
                        h.Password(rabbitMQConfig.Password);
                    });
                });

                // Add MassTransit
                services.AddMassTransit(config =>
                {
                    config.AddBus(provider => rabbitMQBus);
                });

                // Register MassTransit's IPublishEndpoint and IBus which can be used to send and publish messages
                services.AddSingleton<IPublishEndpoint>(rabbitMQBus);
                services.AddSingleton<IBus>(rabbitMQBus);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseHangfireDashboard();
            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                WorkerCount = 1,
                ServerCheckInterval = TimeSpan.FromMinutes(5),
                CancellationCheckInterval = TimeSpan.FromMinutes(5)
            }); ;

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}