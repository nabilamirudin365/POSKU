using Microsoft.EntityFrameworkCore;
using POSKU.Core;

namespace POSKU.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product>     Products     => Set<Product>();
    public DbSet<Customer>    Customers    => Set<Customer>();
    public DbSet<Warehouse>   Warehouses   => Set<Warehouse>();
    public DbSet<SalesHeader> SalesHeaders => Set<SalesHeader>();
    public DbSet<SalesItem>   SalesItems   => Set<SalesItem>();
    public DbSet<StockTxn>    StockTxns    => Set<StockTxn>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Produk
        mb.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        mb.Entity<Product>().HasIndex(p => p.Barcode).IsUnique();
        mb.Entity<Product>().Property(p => p.Price).HasPrecision(18,2);
        mb.Entity<Product>().Property(p => p.Cost).HasPrecision(18,2);

        // Warehouse
        mb.Entity<Warehouse>().HasIndex(w => w.Code).IsUnique();

        // Sales
        mb.Entity<SalesHeader>().HasIndex(h => h.Number).IsUnique();
        mb.Entity<SalesItem>().Property(i => i.Price).HasPrecision(18,2);
        mb.Entity<SalesItem>().Property(i => i.Discount).HasPrecision(18,2);
        mb.Entity<SalesItem>().Property(i => i.Tax).HasPrecision(18,2);
        mb.Entity<SalesItem>().Property(i => i.LineTotal).HasPrecision(18,2);

        // StockTxn
        mb.Entity<StockTxn>().Property(t => t.UnitCost).HasPrecision(18,2);
        mb.Entity<StockTxn>().Property(t => t.Qty).HasPrecision(18,3); // qty bisa pecahan

        // Seed gudang default (sekali saja)
        mb.Entity<Warehouse>().HasData(new Warehouse { Id = 1, Code = "MAIN", Name = "Gudang Utama", IsDefault = true });
    }
}
