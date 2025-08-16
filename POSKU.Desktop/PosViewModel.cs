using System.Collections.ObjectModel;
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

    [ObservableProperty] private string entry = "";
    [ObservableProperty] private decimal subtotal;
    [ObservableProperty] private decimal grandTotal;

    // >>> R1.M4: pembayaran
    [ObservableProperty] private decimal paid;
    [ObservableProperty] private decimal change;

    public PosViewModel(AppDbContext db)
    {
        _db = db;
        RecalcTotals();
    }

    partial void OnPaidChanged(decimal value) => Change = Paid - GrandTotal;

    private void RecalcTotals()
    {
        Subtotal = Cart.Sum(x => x.LineTotal);
        GrandTotal = Subtotal;   // belum diskon/ppn
        Change = Paid - GrandTotal;
    }

    private Product? FindProduct(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        key = key.Trim();

        // Saat barcode di-skip, hanya SKU
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
            exists.Recalc();
        }
        else
        {
            var line = new CartLineVM
            {
                ProductId = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Price = p.Price,
                Qty = qty
            };
            line.Recalc();
            Cart.Add(line);
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
            System.Windows.MessageBox.Show("Produk tidak ditemukan (cek SKU).");
            return;
        }

        AddProduct(p, 1);
        Entry = "";
    }

    [RelayCommand] private void ClearCart() { Cart.Clear(); RecalcTotals(); }
    [RelayCommand] private void RemoveLine(CartLineVM? line) { if (line==null) return; Cart.Remove(line); RecalcTotals(); }
    [RelayCommand] private void IncQty(CartLineVM? line) { if (line==null) return; line.Qty += 1; line.Recalc(); RecalcTotals(); }
    [RelayCommand] private void DecQty(CartLineVM? line)
    {
        if (line == null) return;
        if (line.Qty > 1) { line.Qty -= 1; line.Recalc(); }
        else { Cart.Remove(line); }
        RecalcTotals();
    }

    // ===== R1.M4: Posting =====

    [RelayCommand]
    private void PostSale()
    {
        if (Cart.Count == 0)
        {
            System.Windows.MessageBox.Show("Keranjang kosong.");
            return;
        }

        if (Paid < GrandTotal)
        {
            var ok = System.Windows.MessageBox.Show("Pembayaran kurang dari total. Lanjutkan?", "Konfirmasi", System.Windows.MessageBoxButton.YesNo);
            if (ok != System.Windows.MessageBoxResult.Yes) return;
        }

        using var trx = _db.Database.BeginTransaction();

        try
        {
            var number = GenerateDocNumber(); // POS-YYYYMMDD-####

            var header = new SalesHeader
            {
                Number = number,
                Date = DateTime.Now,
                Subtotal = Subtotal,
                DiscountTotal = 0m,
                TaxTotal = 0m,
                GrandTotal = GrandTotal,
                PaymentMethod = PaymentMethod.Cash,
                Paid = Paid,
                Change = Paid - GrandTotal,
                Status = SalesStatus.Posted
            };

            foreach (var line in Cart)
            {
                var item = new SalesItem
                {
                    ProductId = line.ProductId,
                    Qty = line.Qty,
                    Price = line.Price,
                    Discount = 0m,
                    Tax = 0m,
                    LineTotal = line.LineTotal,
                    WarehouseId = 1 // asumsi gudang default MAIN (Id=1 dari seeding)
                };
                header.Items.Add(item);
            }

            _db.SalesHeaders.Add(header);

            // Kurangi stok & tulis ledger stok (StockTxn OUT)
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
                    UnitCost = prod.Cost, // akan dipakai di R2 (HPP)
                    RefType = StockRef.Sale,
                    RefId = header.Id, // sementara 0; akan terisi setelah SaveChanges
                    Note = header.Number
                });
            }

            _db.SaveChanges();

            // Perbarui RefId StockTxn dengan Header.Id jika perlu (opsional—di SQLite SaveChanges sudah memberi Id)
            foreach (var t in _db.StockTxns.Where(t => t.RefId == null && t.RefType == StockRef.Sale && t.Note == header.Number))
                t.RefId = header.Id;
            _db.SaveChanges();

            trx.Commit();

            System.Windows.MessageBox.Show($"Transaksi tersimpan.\nNo: {header.Number}\nTotal: {header.GrandTotal:N0}\nKembali: {header.Change:N0}");

            // Reset layar
            Cart.Clear();
            Paid = 0;
            RecalcTotals();
        }
        catch (Exception ex)
        {
            trx.Rollback();
            System.Windows.MessageBox.Show("Gagal menyimpan transaksi:\n" + ex.Message, "Error");
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
