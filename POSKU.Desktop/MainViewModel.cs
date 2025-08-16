using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using POSKU.Core;
using POSKU.Data;
using System.Windows;


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
    }

    private void Load()
    {
        Products.Clear();
        foreach (var p in _db.Products.AsNoTracking().OrderBy(p => p.Name))
            Products.Add(p);
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
