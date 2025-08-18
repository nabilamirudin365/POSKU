namespace POSKU.Core
{
    public class PurchaseItem
    {
        public int Id { get; set; }

        // FK ke header pembelian
        public int PurchaseHeaderId { get; set; }
        public PurchaseHeader Header { get; set; } = null!;

        // FK ke produk
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Detail baris
        public decimal Qty { get; set; }
        public decimal Cost { get; set; }          // << penting: kita pakai Cost untuk pembelian
        public decimal LineTotal { get; set; }     // Qty × Cost

        // Gudang (sementara 1)
        public int WarehouseId { get; set; }
    }
}
