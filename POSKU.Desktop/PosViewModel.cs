using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using POSKU.Core;
using POSKU.Data;

namespace POSKU.Desktop;

public partial class PosViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    public ObservableCollection<CartLineVM> Cart { get; } = new();

    [ObservableProperty] private string entry = "";     // input scan/ketik SKU atau Barcode
    [ObservableProperty] private decimal subtotal;
    [ObservableProperty] private decimal grandTotal;

    public PosViewModel(AppDbContext db)
    {
        _db = db;
        RecalcTotals();
    }

    private void RecalcTotals()
    {
        Subtotal = Cart.Sum(x => x.LineTotal);
        GrandTotal = Subtotal; // R1.M3: belum diskon/ppn
    }

    private Product? FindProduct(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        key = key.Trim();

        // Prioritas: barcode -> SKU
        var prod = _db.Products.AsNoTracking().FirstOrDefault(p => p.Barcode == key);
        if (prod == null)
            prod = _db.Products.AsNoTracking().FirstOrDefault(p => p.Sku == key);

        if (prod is { IsActive: false }) return null;
        return prod;
    }

    private void AddProduct(Product p, decimal qty = 1)
    {
        var exists = Cart.FirstOrDefault(x => x.ProductId == p.Id);
        if (exists != null)
        {
            exists.Qty += qty;
            exists.Recalc();
        }
        else
        {
            Cart.Add(new CartLineVM
            {
                ProductId = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Price = p.Price,
                Qty = qty
            });
        }
        RecalcTotals();
    }

    [RelayCommand]
    private void AddByEntry()
    {
        var key = Entry;
        if (string.IsNullOrWhiteSpace(key)) return;

        var p = FindProduct(key);
        if (p == null)
        {
            System.Windows.MessageBox.Show("Produk tidak ditemukan (cek Barcode/SKU).");
            return;
        }

        AddProduct(p, 1);
        Entry = ""; // kosongkan untuk scan berikutnya
    }

    [RelayCommand]
    private void ClearCart()
    {
        Cart.Clear();
        RecalcTotals();
    }

    [RelayCommand]
    private void RemoveLine(CartLineVM? line)
    {
        if (line == null) return;
        Cart.Remove(line);
        RecalcTotals();
    }

    [RelayCommand]
    private void IncQty(CartLineVM? line)
    {
        if (line == null) return;
        line.Qty += 1;
        line.Recalc();
        RecalcTotals();
    }

    [RelayCommand]
    private void DecQty(CartLineVM? line)
    {
        if (line == null) return;
        if (line.Qty > 1)
        {
            line.Qty -= 1;
            line.Recalc();
        }
        else
        {
            Cart.Remove(line);
        }
        RecalcTotals();
    }
}

public partial class CartLineVM : ObservableObject
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";

    [ObservableProperty] private decimal price;
    [ObservableProperty] private decimal qty = 1;
    [ObservableProperty] private decimal lineTotal;

    public void Recalc()
    {
        LineTotal = Qty * Price;
        OnPropertyChanged(nameof(LineTotal));
    }
}
