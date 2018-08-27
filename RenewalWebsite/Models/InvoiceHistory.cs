using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public class InvoiceHistoryModel
    {
        public bool showUSDConversion { get; set; }
        public bool displayConversion { get; set; }
        public bool showUSDOption { get; set; }
        public int Type { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<InvoiceHistory> InvoiceHistory { get; set; }
    }

    public class InvoiceHistory
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal USDAmount { get; set; }
        public string Method { get; set; }
        public string InvoiceNumber { get; set; }
        public string Description { get; set; }
    }
}
