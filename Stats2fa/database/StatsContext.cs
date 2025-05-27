using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Stats2fa.api;
using Stats2fa.api.models;
using Stats2fa.logger;

namespace Stats2fa.database;

public class StatsContext : DbContext, IAsyncDisposable {
    private readonly string? _dbPath;

    // Constructor with DbContextOptions
    public StatsContext(DbContextOptions<StatsContext> options) : base(options: options) {
        // This constructor is used when options are provided directly
    }

    // Constructor that creates a SQLite database in a specified folder
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
    public DbSet<DistributorInformation> Distributors { get; set; } = null!;
    public DbSet<VendorInformation> Vendors { get; set; } = null!;
    public DbSet<ClientInformation> Clients { get; set; } = null!;
    public DbSet<UserInformation> Users { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(value: _dbPath)) optionsBuilder.UseSqlite($"Data Source={_dbPath}");
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

        // Configure User entity
        modelBuilder.Entity<UserInformation>()
            .HasKey(u => u.UserId);

        modelBuilder.Entity<UserInformation>()
            .Property(u => u.UserId)
            .ValueGeneratedOnAdd();

        // Explicitly ignore API model types that shouldn't be mapped to tables
        modelBuilder.Ignore<User.UserCostCentre>();
        modelBuilder.Ignore<User.UserDefaultClient>();
        // Can't use Ignore<T> with struct types like ErrorDetails
        modelBuilder.Ignore<Common.Owner>();
        modelBuilder.Ignore<Common.Source>();
        modelBuilder.Ignore<Common.PasswordComplexity>();
        modelBuilder.Ignore<Common.OtpSettings>();
        modelBuilder.Ignore<Common.OtpMethods>();
        modelBuilder.Ignore<Common.TokenValidity>();
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