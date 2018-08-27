using RenewalWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Services
{
    public interface IInvoiceHistoryService
    {
        List<InvoiceHistory> GetInvoiceHistory(DateTime FromDate, DateTime ToDate, string Email);

        List<InvoiceHistory> GetAllInvoiceHistory(string Email);

        int GetAllInvoiceHistoryCount(string Email);
    }
}
