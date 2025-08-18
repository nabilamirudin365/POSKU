using System;
using System.Collections.Generic;

namespace POSKU.Core
{
    public class PurchaseHeader
    {
        public int Id { get; set; }
        public string Number { get; set; } = "";   // PUR-YYYYMMDD-####
        public DateTime Date { get; set; }
        public string Supplier { get; set; } = "";

        // Nilai agregat
        public decimal Subtotal { get; set; }      // Σ (Qty × Cost)
        public decimal GrandTotal { get; set; }    // sementara = Subtotal (tanpa pajak/diskon)

        public List<PurchaseItem> Items { get; set; } = new();
    }
}
