namespace POSKU.Core;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;      // unik
    public string? Barcode { get; set; }                 // bisa unik/null
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }                   // harga jual
    public decimal Cost { get; set; }                    // HPP rata-rata (R2)
    public int Stock { get; set; }                       // cache stok (disiplinkan via transaksi)
    public bool IsActive { get; set; } = true;
}
