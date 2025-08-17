using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using POSKU.Data;

namespace POSKU.Desktop
{
    public partial class ReportViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        [ObservableProperty] private DateTime? date = DateTime.Today;
        public ObservableCollection<Row> Rows { get; } = new();
        [ObservableProperty] private int count;
        [ObservableProperty] private decimal sumGrand;

        public ReportViewModel(AppDbContext db)
        {
            _db = db;
            Refresh();
        }

        [RelayCommand]
        private void Refresh()
        {
            Rows.Clear();
            if (Date is null) return;
            var d0 = Date.Value.Date;
            var d1 = d0.AddDays(1);

            var q = _db.SalesHeaders
                       .AsNoTracking()
                       .Where(h => h.Date >= d0 && h.Date < d1)
                       .OrderBy(h => h.Id)
                       .Select(h => new Row
                       {
                           Number = h.Number,
                           Date = h.Date,
                           Subtotal = h.Subtotal,
                           DiscountTotal = h.DiscountTotal,
                           GrandTotal = h.GrandTotal
                       })
                       .ToList();

            foreach (var r in q) Rows.Add(r);
            Count = Rows.Count;
            SumGrand = Rows.Sum(x => x.GrandTotal);
        }

        public class Row
        {
            public string Number { get; set; } = "";
            public DateTime Date { get; set; }
            public decimal Subtotal { get; set; }
            public decimal DiscountTotal { get; set; }
            public decimal GrandTotal { get; set; }
        }
    }
}
