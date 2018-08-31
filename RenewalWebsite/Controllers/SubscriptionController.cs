using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using RenewalWebsite.Models;
using RenewalWebsite.Services;
using Microsoft.Extensions.Options;
using Stripe;
using Microsoft.Extensions.Logging;
using RenewalWebsite.Utility;
using Microsoft.Extensions.Localization;
using RenewalWebsite.SettingModels;

namespace RenewalWebsite.Controllers
{
    public class SubscriptionController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDonationService _donationService;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly ILoggerServicecs _loggerService;
        private readonly IStringLocalizer<SubscriptionController> _localizer;
        private EventLog log;

        public SubscriptionController(UserManager<ApplicationUser> userManager,
            IDonationService donationService,
            IOptions<StripeSettings> stripeSettings,
            IStringLocalizer<SubscriptionController> localizer,
            ILoggerServicecs loggerService)
        {
            _userManager = userManager;
            _donationService = donationService;
            _stripeSettings = stripeSettings;
            _loggerService = loggerService;
            _localizer = localizer;
        }

        public IActionResult Delete(string subscriptionId)
        {
            try
            {
                var subscriptionService = new StripeSubscriptionService(_stripeSettings.Value.SecretKey);
                var result = subscriptionService.Cancel(subscriptionId);

                SetTempMessage(_localizer["You have successfully deleted"] + result.StripePlan.Nickname
                + _localizer["subscription"]);
                return RedirectToAction("Index", "Manage");
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.DELETE_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }
    }
}