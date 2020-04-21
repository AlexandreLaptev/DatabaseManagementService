using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using JetBrains.Annotations;

namespace DatabaseManagement
{
    public static class HangfireDbContextServiceCollectionExtensions
    {
        public static IServiceCollection CreateHangfireContext(this IServiceCollection services, [NotNull] string connectionString, ILogger<Startup> logger)
        {
            logger.LogInformation("Creating Hangfire database...");

            var optionsBuilder = new DbContextOptionsBuilder<HangfireDbContext>()
                .UseSqlite(connectionString);

            using (var dbContext = new HangfireDbContext(optionsBuilder.Options))
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }

            logger.LogInformation("Hangfire database created.");
            return services;
        }
    }
}