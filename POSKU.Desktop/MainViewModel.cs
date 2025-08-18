using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using POSKU.Core;
using POSKU.Data;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;


namespace POSKU.Desktop;

public partial class MainViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    public ObservableCollection<Product> Products { get; } = new();

    [ObservableProperty] private string sku = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private decimal price;
    [ObservableProperty] private int stock;

    public MainViewModel(AppDbContext db)
    {
        _db = db;
        Load();

        WeakReferenceMessenger.Default.Register<StockChangedMessage>(this, (r, m) =>
        {
            RefreshSingleProduct(m.Value); // m.Value = ProductId
        });
    }

    private void Load()
    {
        Products.Clear();
        foreach (var p in _db.Products.AsNoTracking().OrderBy(p => p.Name))
            Products.Add(p);
    }
    private void RefreshSingleProduct(int productId)
    {
        // ambil stok terbaru dari DB
        var updated = _db.Products.AsNoTracking().FirstOrDefault(p => p.Id == productId);
        if (updated is null) return;

        var local = Products.FirstOrDefault(p => p.Id == productId);
        if (local is null)
        {
            // kalau belum ada di list, tambahkan
            Products.Add(updated);
            return;
        }

        // update nilai yang berubah (stok, cost, dll)
        local.Stock = updated.Stock;
        local.Cost  = updated.Cost;

        // jika Product bukan ObservableObject, paksa refresh baris:
        // Caranya sederhana: ganti item di koleksi (trigger UI refresh)
        var idx = Products.IndexOf(local);
        Products[idx] = updated;
    }

    [RelayCommand]
    private void AddProduct()
    {
       if (string.IsNullOrWhiteSpace(Sku)) { MessageBox.Show("SKU wajib diisi."); return; }
    if (string.IsNullOrWhiteSpace(Name)) { MessageBox.Show("Nama produk wajib diisi."); return; }
    if (Price < 0) { MessageBox.Show("Harga tidak boleh negatif."); return; }
    if (Stock < 0) { MessageBox.Show("Stok tidak boleh negatif."); return; }

    var p = new Product
    {
        Sku = Sku.Trim(),
        Name = Name.Trim(),
        Price = Price,
        Stock = Stock,
        IsActive = true
    };

    _db.Products.Add(p);
    try
    {
        _db.SaveChanges();
    }
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
    {
        MessageBox.Show("SKU sudah dipakai. Gunakan SKU lain.", "Gagal Simpan");
        _db.ChangeTracker.Clear();
        return;
    }

    Load();
    Sku = Name = "";
    Price = 0;
    Stock = 0;
    }
}
