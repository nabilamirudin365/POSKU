using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using POSKU.Core;
using POSKU.Data;

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
        if (string.IsNullOrWhiteSpace(Sku) || string.IsNullOrWhiteSpace(Name)) return;

        var p = new Product { Sku = Sku.Trim(), Name = Name.Trim(), Price = Price, Stock = Stock, IsActive = true };
        _db.Products.Add(p);
        _db.SaveChanges();

        Load();
        Sku = Name = "";
        Price = 0;
        Stock = 0;
    }
}
