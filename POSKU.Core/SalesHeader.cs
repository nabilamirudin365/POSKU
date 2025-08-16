namespace POSKU.Core;

public enum PaymentMethod { Cash = 0, Card = 1, Transfer = 2 }
public enum SalesStatus   { Draft = 0, Hold = 1, Posted = 2 }

public class SalesHeader
{
    public int Id { get; set; }
    public string Number { get; set; } = "";          // nomor nota
    public DateTime Date { get; set; } = DateTime.Now;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }        // diskon per nota
    public decimal TaxTotal { get; set; }             // PPN
    public decimal GrandTotal { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public decimal Paid { get; set; }                 // uang dibayar
    public decimal Change { get; set; }               // kembalian
    public SalesStatus Status { get; set; } = SalesStatus.Posted;

    public List<SalesItem> Items { get; set; } = new();
}
