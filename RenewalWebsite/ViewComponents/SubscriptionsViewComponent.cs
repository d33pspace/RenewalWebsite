using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RenewalWebsite.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace RenewalWebsite.ViewComponents
{
    public class SubscriptionsViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOptions<StripeSettings> _stripeSettings;

        public SubscriptionsViewComponent(
            UserManager<ApplicationUser> userManager,
            IOptions<StripeSettings> stripeSettings)
        {
            _userManager = userManager;
            _stripeSettings = stripeSettings;
        }

        public IViewComponentResult Invoke(int numberOfItems)
        {
            var user = GetCurrentUserAsync();

            if (!string.IsNullOrEmpty(user.StripeCustomerId))
            {
                var customerService = new StripeSubscriptionService(_stripeSettings.Value.SecretKey);

                var StripSubscriptionListOption = new StripeSubscriptionListOptions()
                {
                    CustomerId = user.StripeCustomerId,
                    Limit = 100
                };

                var customerSubscription = new CustomerPaymentViewModel();

                try
                {
                    var subscriptions = customerService.List(StripSubscriptionListOption);
                    customerSubscription = new CustomerPaymentViewModel
                    {
                        UserName = user.Email,
                        Subscriptions = subscriptions.Select(s => new CustomerSubscriptionViewModel
                        {
                            Id = s.Id,
                            Name = s.StripePlan.Name.Split('_')[0].ToLower(),
                            Amount = s.StripePlan.Amount,
                            Currency = s.StripePlan.Currency,
                            Status = s.Status
                        }).ToList()
                    };

                    foreach (CustomerSubscriptionViewModel subscriptionVal in customerSubscription.Subscriptions)
                    {
                        subscriptionVal.Status = GetStatus(subscriptionVal.Status);
                        subscriptionVal.Name = GetPlanName(subscriptionVal.Name);
                    }
                }
                catch (StripeException sex)
                {
                    ModelState.AddModelError("CustmoerNotFound", sex.Message);
                }

                return View("View", customerSubscription);
            }

            var subscription = new CustomerPaymentViewModel
            {
                UserName = user.Email,

                Subscriptions = new List<CustomerSubscriptionViewModel>()
            };
            return View("View", subscription);
        }

        private string GetPlanName(string subscriptionValName)
        {
            ResourceManager resourceManager = new ResourceManager("RenewalWebsite.Resources.DataAnnotations",
                Assembly.GetExecutingAssembly());
            switch (subscriptionValName.ToLower())
            {
                case "monthly":
                    return resourceManager.GetString("monthly", CultureInfo.CurrentCulture);
                case "quarterly":
                    return resourceManager.GetString("quarterly", CultureInfo.CurrentCulture);
                case "yearly":
                    return resourceManager.GetString("yearly", CultureInfo.CurrentCulture);
                default:
                    return resourceManager.GetString("monthly", CultureInfo.CurrentCulture);
            }
        }

        private string GetStatus(string subscriptionValStatus)
        {
            ResourceManager resourceManager = new ResourceManager("RenewalWebsite.Resources.DataAnnotations",
                Assembly.GetExecutingAssembly());
            switch (subscriptionValStatus.ToLower())
            {
                case "trialing":
                    return resourceManager.GetString("Trialing", CultureInfo.CurrentCulture);
                case "active":
                    return resourceManager.GetString("Active", CultureInfo.CurrentCulture);
                case "past_due":
                    return resourceManager.GetString("PastDue", CultureInfo.CurrentCulture);
                case "canceled":
                    return resourceManager.GetString("Canceled", CultureInfo.CurrentCulture);
                case "unpaid":
                    return resourceManager.GetString("Unpaid", CultureInfo.CurrentCulture);
                default:
                    return resourceManager.GetString("Active", CultureInfo.CurrentCulture);
            }
        }

        private ApplicationUser GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User).Result;
        }
    }
}
