using RenewalWebsite.Data;
using RenewalWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Services
{
    public class LoggerServicecs : ILoggerServicecs
    {
        private readonly ApplicationDbContext _dbContext;

        public LoggerServicecs(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void SaveEventLog(EventLog log)
        {
            log.CreatedTime = DateTime.Now;
            _dbContext.EventLog.Add(log);
            _dbContext.SaveChanges();
        }
    }
}
