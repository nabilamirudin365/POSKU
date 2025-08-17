using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using POSKU.Core;
using POSKU.Data;
using System.Windows;

namespace POSKU.Desktop;

public partial class PosViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    public ObservableCollection<CartLineVM> Cart { get; } = new();

    // Entry & totals
    [ObservableProperty] private string entry = "";
    [ObservableProperty] private decimal subtotal;        // setelah dikurangi diskon item, sebelum diskon nota
    [ObservableProperty] private decimal grandTotal;

    // Pembayaran
    [ObservableProperty] private decimal paid;
    [ObservableProperty] private decimal change;

    // Breakdown R1.M5
    [ObservableProperty] private decimal subtotalLines;   // Σ (Qty * Price) seluruh baris
    [ObservableProperty] private decimal discountItems;   // Σ (Discount per baris)
    [ObservableProperty] private decimal discountNote;    // Diskon nota (Rp)

    public PosViewModel(AppDbContext db)
    {
        _db = db;

        // Hook perubahan item cart agar total selalu ter-update
        Cart.CollectionChanged += Cart_CollectionChanged;

        RecalcTotals();
    }

    // Reaktif: jika Paid berubah, hitung Change
    partial void OnPaidChanged(decimal value) => Change = Paid - GrandTotal;

    // Reaktif: jika Diskon Nota berubah, hitung ulang total
    partial void OnDiscountNoteChanged(decimal value) => RecalcTotals();

    private void Cart_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (CartLineVM old in e.OldItems)
                old.PropertyChanged -= CartLine_PropertyChanged;
        }
        if (e.NewItems != null)
        {
            foreach (CartLineVM add in e.NewItems)
                add.PropertyChanged += CartLine_PropertyChanged;
        }
        RecalcTotals();
    }

    private void CartLine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CartLineVM.Qty)
            or nameof(CartLineVM.Price)
            or nameof(CartLineVM.Discount))
        {
            RecalcTotals();
        }
    }

    private void RecalcTotals()
    {
        SubtotalLines = Cart.Sum(x => x.Qty * x.Price);
        DiscountItems = Cart.Sum(x => x.Discount);

        var sub = SubtotalLines - DiscountItems;
        if (sub < 0) sub = 0;

        Subtotal   = sub;                    // sebelum diskon nota
        GrandTotal = Subtotal - DiscountNote;
        if (GrandTotal < 0) GrandTotal = 0;

        Change = Paid - GrandTotal;
    }

    private Product? FindProduct(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        key = key.Trim();

        // Saat barcode di-skip, cari hanya SKU
        var prod = _db.Products.AsNoTracking().FirstOrDefault(p => p.Sku == key);

        if (prod is { IsActive: false }) return null;
        return prod;
    }

    private void AddProduct(Product p, decimal qty = 1)
    {
        var exists = Cart.FirstOrDefault(x => x.ProductId == p.Id);
        if (exists != null)
        {
            exists.Qty += qty;
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

    // ===== Commands =====

    [RelayCommand]
    private void AddByEntry()
    {
        var key = Entry;
        if (string.IsNullOrWhiteSpace(key)) return;

        var p = FindProduct(key);
        if (p == null)
        {
            MessageBox.Show("Produk tidak ditemukan (cek SKU).");
            return;
        }

        AddProduct(p, 1);
        Entry = "";
    }

    [RelayCommand] private void ClearCart() { Cart.Clear(); RecalcTotals(); }

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
        // RecalcTotals akan terpanggil via PropertyChanged hook
    }

    [RelayCommand]
    private void DecQty(CartLineVM? line)
    {
        if (line == null) return;
        if (line.Qty > 1) line.Qty -= 1;
        else Cart.Remove(line);
        // RecalcTotals akan terpanggil via hook / remove
    }

    // ===== R1.M4/M5: Posting =====

    [RelayCommand]
    private void PostSale()
    {
        if (Cart.Count == 0)
        {
            MessageBox.Show("Keranjang kosong.");
            return;
        }

        if (Paid < GrandTotal)
        {
            var ok = MessageBox.Show("Pembayaran kurang dari total. Lanjutkan?", "Konfirmasi", MessageBoxButton.YesNo);
            if (ok != MessageBoxResult.Yes) return;
        }

        using var trx = _db.Database.BeginTransaction();
        try
        {
            var number = GenerateDocNumber(); // POS-YYYYMMDD-####

            var header = new SalesHeader
            {
                Number = number,
                Date = DateTime.Now,
                Subtotal = SubtotalLines,                       // sebelum diskon apapun
                DiscountTotal = DiscountItems + DiscountNote,   // total diskon (item + nota)
                TaxTotal = 0m,
                GrandTotal = GrandTotal,
                PaymentMethod = PaymentMethod.Cash,
                Paid = Paid,
                Change = Paid - GrandTotal,
                Status = SalesStatus.Posted
            };

            foreach (var line in Cart)
            {
                header.Items.Add(new SalesItem
                {
                    ProductId = line.ProductId,
                    Qty = line.Qty,
                    Price = line.Price,
                    Discount = line.Discount,                   // simpan diskon per item
                    Tax = 0m,
                    LineTotal = Math.Max(0, (line.Qty * line.Price) - line.Discount),
                    WarehouseId = 1                             // asumsi gudang default MAIN Id=1
                });
            }

            _db.SalesHeaders.Add(header);

            // Kurangi stok & ledger stok (StockTxn OUT)
            foreach (var line in Cart)
            {
                var prod = _db.Products.First(p => p.Id == line.ProductId);
                prod.Stock -= (int)line.Qty;

                _db.StockTxns.Add(new StockTxn
                {
                    Date = DateTime.Now,
                    WarehouseId = 1,
                    ProductId = prod.Id,
                    Direction = StockDir.Out,
                    Qty = line.Qty,
                    UnitCost = prod.Cost,        // dipakai nanti untuk HPP
                    RefType = StockRef.Sale,
                    RefId = header.Id,           // akan valid setelah SaveChanges (SQLite assign Id)
                    Note = header.Number
                });
            }

            _db.SaveChanges();

            // (Opsional) pastikan RefId diisi bila DB driver menunda Id
            foreach (var t in _db.StockTxns.Where(t => t.RefId == null && t.RefType == StockRef.Sale && t.Note == header.Number))
                t.RefId = header.Id;
            _db.SaveChanges();

            trx.Commit();

            MessageBox.Show($"Transaksi tersimpan.\nNo: {header.Number}\nTotal: {header.GrandTotal:N0}\nKembali: {header.Change:N0}");

            // Reset layar
            Cart.Clear();
            Paid = 0;
            DiscountNote = 0;
            RecalcTotals();
        }
        catch (Exception ex)
        {
            trx.Rollback();
            MessageBox.Show("Gagal menyimpan transaksi:\n" + ex.Message, "Error");
        }
    }

    private string GenerateDocNumber()
    {
        var today = DateTime.Now.Date;
        var countToday = _db.SalesHeaders.Count(h => h.Date >= today && h.Date < today.AddDays(1));
        var seq = countToday + 1;
        return $"POS-{today:yyyyMMdd}-{seq:0000}";
    }
}

// ===== View model untuk baris keranjang =====
public partial class CartLineVM : ObservableObject
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";

    [ObservableProperty] private decimal price;
    [ObservableProperty] private decimal qty = 1;

    // Diskon per baris (Rupiah)
    [ObservableProperty] private decimal discount;
}
