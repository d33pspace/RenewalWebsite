using Microsoft.Extensions.Options;
using RenewalWebsite.Data;
using RenewalWebsite.Helpers;
using RenewalWebsite.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly ICurrencyService _currencyService;

        public CampaignService(ApplicationDbContext dbContext,
            IOptions<StripeSettings> stripeSettings,
            ICurrencyService currencyService)
        {
            _dbContext = dbContext;
            _stripeSettings = stripeSettings;
            _currencyService = currencyService;
        }

        public Dictionary<PaymentCycle, string> GetCycles()
        {
            return EnumInfo<PaymentCycle>
                .GetValues()
                .ToDictionary(o => o.Key, o => o.Value);
        }

        public void Save(Donation donation)
        {
            _dbContext.Donations.Add(donation);
            _dbContext.SaveChanges();
        }

        public Donation GetById(int id)
        {
            return _dbContext.Donations.Find(id);
        }

        /// <summary>
        /// Create plan for this donation if is does not exist and return its instance. If it does exist
        /// return the instance.
        /// </summary>
        /// <param name="donation"></param>
        /// <returns></returns>
        public StripePlan GetOrCreatePlan(Donation donation)
        {
            var planService = new StripePlanService(_stripeSettings.Value.SecretKey);

            // Construct plan name from the selected donation type and the cycle
            var cycle = EnumInfo<PaymentCycle>.GetValue(donation.CycleId);
            var frequency = EnumInfo<PaymentCycle>.GetDescription(cycle);
            decimal amount = donation.DonationAmount ?? 0;
            string currency = donation.Currency;
            var planName = $"{frequency}_{amount}_{currency}".ToLower(); //

            // Create new plan is this one does not exist
            if (!Exists(planService, planName))
            {
                var plan = new StripePlanCreateOptions
                {
                    Id = planName,
                    Amount = Convert.ToInt32(amount * 100),
                    Currency = currency.ToLower(),
                    Nickname = planName,
                    Product = new StripePlanProductCreateOptions()
                    {
                        Name = planName
                    }
                    //StatementDescriptor = _stripeSettings.Value.StatementDescriptor
                };

                // Take care intervals
                if (cycle == PaymentCycle.Quarter)
                {
                    plan.IntervalCount = 3;
                    plan.Interval = "month";
                }
                else
                {
                    plan.Interval = cycle.ToString().ToLower(); // day/month/year 
                }
                return planService.Create(plan);
            }
            else
                return planService.Get(planName);
        }

        public int GetByUserId(string userId)
        {
            return _dbContext.Donations.Last(d => d.UserId == userId).Id;
        }
        
        /// <summary>
        /// Check is the plan exists. The API does not have an exists endpoint so we have to use an
        /// exception to detemine existence. 
        /// </summary>
        /// <param name="planService">The StripePlanService Instance</param>
        /// <param name="planName">The Plan name</param>
        /// <returns></returns>
        private bool Exists(StripePlanService planService, string planName)
        {
            try
            {
                planService.Get(planName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
