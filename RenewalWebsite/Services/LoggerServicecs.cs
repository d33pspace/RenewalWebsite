using RenewalWebsite.Data;
using RenewalWebsite.Models;
using RenewalWebsite.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RenewalWebsite.SettingModels;
using RenewalWebsite.Controllers;

namespace RenewalWebsite.Services
{
    public class LoggerServicecs : ILoggerServicecs
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IViewRenderService _viewRenderService;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<EmailNotification> _EmailNotiFy;

        public LoggerServicecs(IEmailSender emailSender, IViewRenderService viewRenderService, ApplicationDbContext dbContext, IOptions<EmailNotification> EmailNotification)
        {
            _emailSender = emailSender;
            _viewRenderService = viewRenderService;
            _dbContext = dbContext;
            _EmailNotiFy = EmailNotification;
        }

        public async void SaveEventLogAsync(EventLog log, ApplicationUser user = null)
        {
            log.CreatedTime = DateTime.Now;
            _dbContext.EventLog.Add(log);
            _dbContext.SaveChanges();

            try
            {
                if (user != null)
                {
                    log.UserName = user.FullName;
                    log.EmailId = user.Email;
                }
                string template = await _viewRenderService.RenderToStringAsync("Shared/_ExceptionMail", log);
                await _emailSender.SendEmailAsync(_EmailNotiFy.Value.Email, "Exception", "", "Admin", template);
            }
            catch (Exception ex)
            {
            }
        }

        public void SaveEventLogToDb(EventLog log)
        {
            try
            {
                log.CreatedTime = DateTime.Now;
                _dbContext.EventLog.Add(log);
                _dbContext.SaveChanges();
            }
            catch (Exception)
            {
            }
        }
    }
}
