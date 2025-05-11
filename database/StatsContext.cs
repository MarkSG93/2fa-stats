using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Stats2fa.api;
using Stats2fa.logger;

namespace Stats2fa.database;

public class StatsContext : DbContext, IAsyncDisposable {
    private readonly string _dbPath;

    public StatsContext(string folderPath, string dbName, ApiInformation? apiInformation) {
        // Ensure the directory exists
        if (!Directory.Exists(path: folderPath)) {
            StatsLogger.Log(stats: apiInformation, $"Creating directory {folderPath}");
            Directory.CreateDirectory(path: folderPath);
        }

        _dbPath = Path.Combine(path1: folderPath, path2: dbName);
        StatsLogger.Log(stats: apiInformation, $"DB Path {_dbPath}");
    }

    // Define your DbSet properties here
    // Example:
    public DbSet<DistributorInformation> Distributors { get; set; }

    public DbSet<VendorInformation> Vendors { get; set; }
    public DbSet<ClientInformation> Clients { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder: modelBuilder);

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

        // Ignore the nested classes/complex types to prevent EF from treating them as entities
        modelBuilder.Entity<ClientInformation>()
            .Ignore(c => c.ClientUsers);
    }

    public static Guid Int2Guid(int value) {
        var bytes = new byte[16];
        BitConverter.GetBytes(value: value).CopyTo(array: bytes, 0);
        return new Guid(b: bytes);
    }

    public static long Guid2Int(Guid value) {
        var b = value.ToByteArray();
        var bint = BitConverter.ToInt64(value: b, 0);
        return bint;
    }

    public static long Guid2Int(string value) {
        return Guid2Int(new Guid(g: value));
    }
}