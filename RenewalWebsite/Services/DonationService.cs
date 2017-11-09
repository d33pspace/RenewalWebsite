using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RenewalWebsite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using RenewalWebsite.Data;
using RenewalWebsite.Helpers;
using Stripe;
using System;

namespace RenewalWebsite.Services
{
    public class DonationService : IDonationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly IOptions<ExchangeRate> _exchangeSettings;
        private readonly IOptions<DonationSettings> _donateSettings;
        private readonly ICurrencyService _currencyService;

        private List<DonationListOption> donationOptions = new List<DonationListOption>
        {
            new DonationListOption {Id = 1, Reason = "to be doubled to"},
            new DonationListOption {Id = 2, Reason = "to be doubled to"},
            new DonationListOption {Id = 3, Reason = "to be doubled to"},
            new DonationListOption {Id = 4, Reason = "My most generous possible gift to be doubled.", IsCustom = true},
        };

        public DonationService(ApplicationDbContext dbContext,
            IOptions<StripeSettings> stripeSettings,
            IOptions<ExchangeRate> exchangeSettings,
            IOptions<DonationSettings> donateSettings,
            ICurrencyService currencyService)
        {
            _dbContext = dbContext;
            _stripeSettings = stripeSettings;
            _exchangeSettings = exchangeSettings;
            _donateSettings = donateSettings;
            _currencyService = currencyService;

            if (_currencyService.GetCurrent().Name.Contains("en"))
            {
                foreach (DonationListOption item in donationOptions)
                {
                    item.Amount = _donateSettings.Value.Donate.Where(a => a.Id == item.Id && a.CurrencyType == "USD").Select(a => a.Value).FirstOrDefault();
                }
            }
            else
            {
                foreach (DonationListOption item in donationOptions)
                {
                    item.Amount = _donateSettings.Value.Donate.Where(a => a.Id == item.Id && a.CurrencyType == "CNY").Select(a => a.Value).FirstOrDefault();
                }
            }
        }

        public Dictionary<PaymentCycle, string> GetCycles()
        {
            return EnumInfo<PaymentCycle>
                .GetValues()
                .ToDictionary(o => o.Key, o => o.Value);
        }

        public List<DonationListOption> DonationOptions
        {
            get
            {
                return donationOptions;
            }
            set
            {
                donationOptions = value;
            }
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
            string currency = donation.currency;
            if (donation.DonationAmount == null)
            {
                var model = (DonationViewModel)donation;
                model.DonationOptions = DonationOptions;

                //amount = Math.Round((model.GetDisplayAmount() / _exchangeSettings.Value.Rate), 2);
                amount = model.GetDisplayAmount();
            }
            var planName = $"{frequency}_{amount}_{currency}".ToLower(); //

            // Create new plan is this one does not exist
            if (!Exists(planService, planName))
            {
                var plan = new StripePlanCreateOptions
                {
                    Id = planName,
                    Amount = Convert.ToInt32(amount * 100),
                    Currency = currency.ToLower(),
                    Name = planName,
                    StatementDescriptor = _stripeSettings.Value.StatementDescriptor
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
        /// Automatically create the standard plans to enable, new users to be able to subscribe. These
        /// are managed in Stripe
        /// </summary>
        public void EnsurePlansExist()
        {
            var planService = new StripePlanService(_stripeSettings.Value.SecretKey);

            var options = new DonationViewModel(DonationOptions).DonationOptions;
            foreach (var cycle in GetCycles())
            {
                foreach (var option in options)
                {
                    if (cycle.Key != PaymentCycle.OneTime)
                    {
                        if (option.Amount > 0)
                        {
                            //var planName = $"{cycle.Value}_{(Math.Round((option.Amount / _exchangeSettings.Value.Rate), 2))}".ToLower();
                            var planName = $"{cycle.Value}_{option.Amount}".ToLower();
                            var plan = new StripePlanCreateOptions
                            {
                                Id = planName,
                                //Amount = Convert.ToInt32(Math.Round((option.Amount / _exchangeSettings.Value.Rate), 2) * 100),
                                Amount = Convert.ToInt32(option.Amount * 100),
                                Currency = "usd",
                                Name = planName,
                                StatementDescriptor = _stripeSettings.Value.StatementDescriptor
                            };

                            // Take care intervals
                            if (cycle.Key == PaymentCycle.Quarter)
                            {
                                plan.IntervalCount = 3;
                                plan.Interval = "month";
                            }
                            else
                            {
                                plan.Interval = cycle.Key.ToString().ToLower(); // day/month/year 
                            }

                            if (!Exists(planService, planName))
                                planService.Create(plan);
                        }
                    }
                }
            }
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
