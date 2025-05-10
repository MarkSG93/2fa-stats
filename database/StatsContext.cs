using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Stats2fa.api;
using Stats2fa.logger;

namespace Stats2fa.database {
    public class StatsContext : DbContext, IAsyncDisposable {
        private readonly string _dbPath;

        public StatsContext(string folderPath, string dbName, ApiInformation? apiInformation) {
            // Ensure the directory exists
            if (!Directory.Exists(folderPath)) {
                StatsLogger.Log(apiInformation, $"Creating directory {folderPath}");
                Directory.CreateDirectory(folderPath);
            }

            _dbPath = Path.Combine(folderPath, dbName);
            StatsLogger.Log(apiInformation, $"DB Path {_dbPath}");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // Configure DistributorInformation entity
            modelBuilder.Entity<DistributorInformation>()
                .HasKey(d => d.DistributorInformationId);

            modelBuilder.Entity<DistributorInformation>()
                .Property(d => d.DistributorInformationId)
                .ValueGeneratedOnAdd();

            // Configure VendorInformation entity
            modelBuilder.Entity<VendorInformation>()
                .HasKey(v => v.VendorInformationId);

            modelBuilder.Entity<VendorInformation>()
                .Property(v => v.VendorInformationId)
                .ValueGeneratedOnAdd();

            // Configure ClientInformation entity
            modelBuilder.Entity<ClientInformation>()
                .HasKey(c => c.ClientInformationId);

            modelBuilder.Entity<ClientInformation>()
                .Property(c => c.ClientInformationId)
                .ValueGeneratedOnAdd();
        }

        // Define your DbSet properties here
        // Example:
        public DbSet<DistributorInformation> Distributors { get; set; }

        public DbSet<VendorInformation> Vendors { get; set; }
        public DbSet<ClientInformation> Clients { get; set; }

        public static Guid Int2Guid(int value) {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        public static Int64 Guid2Int(Guid value) {
            byte[] b = value.ToByteArray();
            Int64 bint = BitConverter.ToInt64(b, 0);
            return bint;
        }

        public static Int64 Guid2Int(string value) {
            return Guid2Int(new Guid(value));
        }
    }
}