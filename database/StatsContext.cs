using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Stats2fa.database
{
    public class StatsContext : DbContext, IAsyncDisposable
    {
        private readonly string _dbPath;

        public StatsContext(string folderPath, string dbName)
        {
            _dbPath = Path.Combine(folderPath, dbName);
            Console.WriteLine($"DB Path {_dbPath}");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }

        // Define your DbSet properties here
        // Example:
        public DbSet<DistributorInformation> Distributors { get; set; }
        // public DbSet<Vendor> Vendors { get; set; }
        public DbSet<ClientInformation> Clients { get; set; }
    }
}