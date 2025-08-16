namespace POSKU.Core;

public class SalesItem
{
    public int Id { get; set; }

    public int SalesHeaderId { get; set; }
    public SalesHeader? SalesHeader { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public decimal Qty { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal LineTotal { get; set; } // (Qty*Price - Diskon + PPN)
}
