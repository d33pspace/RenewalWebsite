using RenewalWebsite.Data;
using RenewalWebsite.Models;
using RenewalWebsite.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Services
{
    public class LoggerServicecs : ILoggerServicecs
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IViewRenderService _viewRenderService;
        private readonly IEmailSender _emailSender;

        public LoggerServicecs(IEmailSender emailSender, IViewRenderService viewRenderService, ApplicationDbContext dbContext)
        {
            _emailSender = emailSender;
            _viewRenderService = viewRenderService;
            _dbContext = dbContext;
        }

        public async void SaveEventLogAsync(EventLog log)
        {
            log.CreatedTime = DateTime.Now;
            _dbContext.EventLog.Add(log);
            _dbContext.SaveChanges();

            try
            {
                string template = await _viewRenderService.RenderToStringAsync("Shared/_ExceptionMail", log);
                await _emailSender.SendEmailAsync("girishkolte2000@gmail.com", "Exception", "", "Admin", template);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
