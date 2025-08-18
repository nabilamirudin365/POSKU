using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using POSKU.Core;
using POSKU.Data;
using CommunityToolkit.Mvvm.Messaging;

namespace POSKU.Desktop
{
    public partial class PurchaseViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        public ObservableCollection<PurchaseLineVM> Cart { get; } = new();

        [ObservableProperty] private string entry = "";      // SKU input
        [ObservableProperty] private string supplier = "";
        [ObservableProperty] private decimal subtotal;       // Σ(Qty*Cost)
        [ObservableProperty] private decimal grandTotal;     // sementara = Subtotal

        public PurchaseViewModel(AppDbContext db)
        {
            _db = db;

            // Recalc awal
            Recalc();

            // Pantau perubahan isi keranjang (tambah/hapus baris)
            Cart.CollectionChanged += Cart_CollectionChanged;
        }

        #region Wiring helpers (agar Subtotal auto-update saat Qty/Cost berubah)
        private void Cart_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var it in e.OldItems.OfType<PurchaseLineVM>())
                    it.PropertyChanged -= Line_PropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (var it in e.NewItems.OfType<PurchaseLineVM>())
                    it.PropertyChanged += Line_PropertyChanged;
            }
            Recalc();
        }

        private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PurchaseLineVM.Qty) or nameof(PurchaseLineVM.Cost))
            {
                // Normalisasi ringan: tidak boleh negatif
                if (sender is PurchaseLineVM line)
                {
                    if (line.Qty < 0) line.Qty = 0;
                    if (line.Cost < 0) line.Cost = 0;
                }
                Recalc();
            }
        }
        #endregion

        private void Recalc()
        {
            Subtotal = Cart.Sum(x => x.Qty * x.Cost);
            GrandTotal = Subtotal; // belum pajak/diskon
        }

        private Product? FindBySku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku)) return null;
            sku = sku.Trim();
            return _db.Products.AsNoTracking()
                               .FirstOrDefault(p => p.Sku == sku && p.IsActive);
        }

        private void AddProduct(Product p, decimal qty = 1)
        {
            var ex = Cart.FirstOrDefault(x => x.ProductId == p.Id);
            if (ex != null)
            {
                ex.Qty += qty;
            }
            else
            {
                Cart.Add(new PurchaseLineVM
                {
                    ProductId = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    Cost = p.Cost,  // default last cost produk
                    Qty = qty
                });
            }
            Recalc();
        }

        // ===== Commands =====
        [RelayCommand]
        private void AddByEntry()
        {
            var p = FindBySku(Entry);
            if (p == null)
            {
                MessageBox.Show("SKU tidak ditemukan atau non-aktif.");
                return;
            }
            AddProduct(p, 1);
            Entry = "";
        }

        [RelayCommand] private void ClearCart() { Cart.Clear(); Recalc(); }

        [RelayCommand]
        private void RemoveLine(PurchaseLineVM? line)
        {
            if (line == null) return;
            Cart.Remove(line);
            Recalc();
        }

        [RelayCommand]
        private void IncQty(PurchaseLineVM? line)
        {
            if (line == null) return;
            line.Qty += 1;
            // Recalc dipanggil dari Line_PropertyChanged
        }

        [RelayCommand]
        private void DecQty(PurchaseLineVM? line)
        {
            if (line == null) return;
            if (line.Qty > 1) line.Qty -= 1;
            else Cart.Remove(line);
            // Recalc otomatis dari event
        }

        // ===== Posting Pembelian =====
        [RelayCommand]
        private void PostPurchase()
        {
            if (Cart.Count == 0)
            {
                MessageBox.Show("Keranjang kosong.");
                return;
            }

            // Validasi minimal
            if (Cart.Any(x => x.Qty <= 0))
            {
                MessageBox.Show("Qty setiap baris harus > 0.");
                return;
            }

            using var trx = _db.Database.BeginTransaction();
            try
            {
                var number = GenerateDocNumber();

                var header = new PurchaseHeader
                {
                    Number = number,
                    Date = DateTime.Now,
                    Supplier = Supplier ?? "",
                    Subtotal = Subtotal,
                    GrandTotal = GrandTotal
                };

                foreach (var line in Cart)
                {
                    header.Items.Add(new PurchaseItem
                    {
                        ProductId = line.ProductId,
                        Qty = line.Qty,
                        Cost = line.Cost,
                        LineTotal = line.Qty * line.Cost,
                        WarehouseId = 1    // gudang default
                    });
                }

                _db.PurchaseHeaders.Add(header);

                // Update stok & ledger IN
                foreach (var line in Cart)
                {
                    var prod = _db.Products.First(p => p.Id == line.ProductId);
                    prod.Stock += (int)line.Qty;
                    prod.Cost = line.Cost; // kebijakan: last cost

                    _db.StockTxns.Add(new StockTxn
                    {
                        Date = DateTime.Now,
                        WarehouseId = 1,
                        ProductId = prod.Id,
                        Direction = StockDir.In,
                        Qty = line.Qty,
                        UnitCost = line.Cost,
                        RefType = StockRef.Purchase,
                        RefId = header.Id, // akan valid setelah SaveChanges
                        Note = header.Number
                    });
                }

                _db.SaveChanges();

                // (opsional) perbarui RefId ledger bila belum terisi
                foreach (var t in _db.StockTxns.Where(t => t.RefId == null && t.RefType == StockRef.Purchase && t.Note == header.Number))
                    t.RefId = header.Id;

                _db.SaveChanges();
                trx.Commit();
                
                foreach (var line in Cart)
                {
                    WeakReferenceMessenger.Default.Send(new StockChangedMessage(line.ProductId));
                }

                MessageBox.Show($"Pembelian tersimpan.\nNo: {header.Number}\nTotal: {header.GrandTotal:N0}");
                Cart.Clear();
                Supplier = "";
                Recalc();
            }
            catch (Exception ex)
            {
                trx.Rollback();
                MessageBox.Show("Gagal menyimpan pembelian:\n" + ex.Message);
            }
        }

        private string GenerateDocNumber()
        {
            var today = DateTime.Now.Date;
            var countToday = _db.PurchaseHeaders.Count(h => h.Date >= today && h.Date < today.AddDays(1));
            return $"PUR-{today:yyyyMMdd}-{(countToday + 1):0000}";
        }
    }

    public partial class PurchaseLineVM : ObservableObject
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = "";
        public string Name { get; set; } = "";

        [ObservableProperty] private decimal cost;
        [ObservableProperty] private decimal qty = 1;
        // Tidak perlu LineTotal properti—Subtotal kolom dihitung via converter di XAML

        public decimal SubtotalLine => Qty * Cost;

        // Pastikan UI update saat Qty/Cost berubah
        partial void OnQtyChanged(decimal value)
        {
            OnPropertyChanged(nameof(SubtotalLine));
        }

        partial void OnCostChanged(decimal value)
        {
            OnPropertyChanged(nameof(SubtotalLine));
        }

    }
}
