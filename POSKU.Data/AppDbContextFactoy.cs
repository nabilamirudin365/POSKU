using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace POSKU.Data
{
    // Dipakai HANYA oleh 'dotnet ef' saat design-time (migrations)
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Samakan lokasi DB dengan runtime (%LocalAppData%\POSKU\pos.db)
            var dbDir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "POSKU");
            Directory.CreateDirectory(dbDir);
            var dbPath = Path.Combine(dbDir, "pos.db");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            return new AppDbContext(options);
        }
    }
}
