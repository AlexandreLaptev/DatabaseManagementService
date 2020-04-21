using Microsoft.EntityFrameworkCore;

namespace DatabaseManagement
{
    public class HangfireDbContext : DbContext
    {
        public HangfireDbContext(DbContextOptions<HangfireDbContext> options) : base(options)
        {
        }
    }
}