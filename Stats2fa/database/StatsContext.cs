using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Stats2fa.api;
using Stats2fa.logger;

namespace Stats2fa.database;

public class StatsContext : DbContext, IAsyncDisposable {
    private readonly string? _dbPath;

    // Constructor with DbContextOptions
    public StatsContext(DbContextOptions<StatsContext> options) : base(options) {
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
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_dbPath)) {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
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

        // Configure UserInformation entity
        modelBuilder.Entity<UserInformation>()
            .HasKey(u => u.UserInformationId);

        modelBuilder.Entity<UserInformation>()
            .Property(u => u.UserInformationId)
            .ValueGeneratedOnAdd();

        // Set a different table name for UserInformation to avoid conflict with Users
        modelBuilder.Entity<UserInformation>()
            .ToTable("UserInformationTable");

        // Ignore the nested classes/complex types to prevent EF from treating them as entities
        modelBuilder.Entity<ClientInformation>()
            .Ignore(c => c.ClientUsers);

        modelBuilder.Entity<UserInformation>()
            .Ignore(u => u.UserData);

        // Explicitly ignore UserCostCentre when EF tries to map it as an entity
        // This should prevent EF from trying to map complex types inside User classes
        modelBuilder.Ignore<Stats2fa.api.models.User.UserCostCentre>();

        // Mark Users as a keyless entity type - it's just a response container
        modelBuilder.Entity<Stats2fa.api.models.Users>().HasNoKey();

        // Mark response models as keyless entities when they're being tracked
        modelBuilder.Entity<Stats2fa.api.models.User>().HasNoKey();
        modelBuilder.Entity<Stats2fa.api.models.User.UserDefaultClient>().HasNoKey();
        modelBuilder.Entity<Stats2fa.api.models.Common.Owner>().HasNoKey();
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