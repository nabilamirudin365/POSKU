namespace POSKU.Core;

public enum StockDir { In = 1, Out = -1 }
public enum StockRef { Init = 0, Purchase = 1, Sale = 2, Adjust = 3, Transfer = 4, ReturnPurchase = 5, ReturnSale = 6 }

public class StockTxn
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }

    public StockDir Direction { get; set; }           // In / Out
    public decimal Qty { get; set; }                  // selalu positif
    public decimal UnitCost { get; set; }             // untuk HPP (R2)
    public StockRef RefType { get; set; }             // asal transaksi
    public int? RefId { get; set; }                   // Id header asal (mis: SalesHeaderId)
    public string? Note { get; set; }
}
