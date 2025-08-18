using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POSKU.Data;
using POSKU.Core;
using System.Linq;

namespace POSKU.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            // TANGKAP ERROR GLOBAL
            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show(ex.Exception.ToString(), "DispatcherUnhandledException");
                ex.Handled = true; // biar app tidak langsung mati
            };
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                MessageBox.Show(ex.ExceptionObject?.ToString() ?? "Unknown", "UnhandledException");
            };
            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                MessageBox.Show(ex.Exception.ToString(), "UnobservedTaskException");
                ex.SetObserved();
            };

            base.OnStartup(e);

            try
            {
                // === LOKASI DB: bin/.../Data/pos.db ===
                var basePath = AppDomain.CurrentDomain.BaseDirectory; // contoh: ...\POSKU.Desktop\bin\Debug\net8.0-windows\
                var dbDir    = Path.Combine(basePath, "Data");
                Directory.CreateDirectory(dbDir);
                var dbPath   = Path.Combine(dbDir, "pos.db");

                var sc = new ServiceCollection();

                sc.AddDbContext<AppDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));

                sc.AddTransient<MainViewModel>();
                sc.AddSingleton<MainWindow>(sp => new MainWindow(sp.GetRequiredService<MainViewModel>()));
                sc.AddTransient<PosViewModel>();
                sc.AddTransient<PosWindow>(sp => new PosWindow(sp.GetRequiredService<PosViewModel>()));
                sc.AddTransient<ReportViewModel>();
                sc.AddTransient<ReportWindow>(sp => new ReportWindow(sp.GetRequiredService<ReportViewModel>()));
                sc.AddTransient<PurchaseViewModel>();
                sc.AddTransient<PurchaseWindow>(sp => new PurchaseWindow(sp.GetRequiredService<PurchaseViewModel>()));


                Services = sc.BuildServiceProvider();

                using (var scope = Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();

                    // Seed contoh produk (hanya jika kosong)
                    if (!db.Products.Any())
                    {
                        db.Products.AddRange(
                            new Product { Sku = "ABC001", Name = "Teh Botol 350ml", Price = 4500, Stock = 24 },
                            new Product { Sku = "ABC002", Name = "Roti Coklat", Price = 6500.50m, Stock = 10 },
                            new Product { Sku = "XYZ001", Name = "Gula 1kg", Price = 16000, Stock = 5 }
                        );
                        db.SaveChanges();
                    }
                }

                var main = Services.GetRequiredService<MainWindow>();
                this.MainWindow = main; // penting: set MainWindow agar ShutdownMode tahu window utama
                main.Show();

                // (opsional) tampilkan path DB agar mudah dicek saat dev
                Console.WriteLine($"[POSKU] Using DB: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "pos.db")}");
            }
            catch (Exception exAll)
            {
                MessageBox.Show(exAll.ToString(), "Startup Error");
                Shutdown(-1);
            }
        }
    }
}
