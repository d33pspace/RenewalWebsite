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

namespace RenewalWebsite.Controllers
{
    public class SubscriptionController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDonationService _donationService;
        private readonly IOptions<StripeSettings> _stripeSettings;

        public SubscriptionController(UserManager<ApplicationUser> userManager,
            IDonationService donationService,
            IOptions<StripeSettings> stripeSettings)
        {
            _userManager = userManager;
            _donationService = donationService;
            _stripeSettings = stripeSettings;
        }
        public IActionResult Delete(string subscriptionId)
        {
            var subscriptionService = new StripeSubscriptionService(_stripeSettings.Value.SecretKey);
            var result = subscriptionService.Cancel(subscriptionId);

            SetTempMessage($"You have successfully deleted '{result.StripePlan.Name}' subscription");
            return RedirectToAction("Index", "Manage");
        }
    }
}