using RenewalWebsite.Data;
using RenewalWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Services
{
    public class InvoiceHistoryService : IInvoiceHistoryService
    {
        private readonly ApplicationDbContext _dbContext;

        public InvoiceHistoryService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<InvoiceHistory> GetInvoiceHistory(DateTime FromDate, DateTime ToDate, string Email)
        {
            try
            {
                return _dbContext.InvoiceHistory.Where(a => a.Date.Date >= FromDate.Date && a.Date.Date <= ToDate.Date && a.Email.ToLower().Equals(Email.ToLower())).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public List<InvoiceHistory> GetAllInvoiceHistory(string Email)
        {
            try
            {
                return _dbContext.InvoiceHistory.Where(a => a.Email.ToLower().Equals(Email.ToLower())).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
