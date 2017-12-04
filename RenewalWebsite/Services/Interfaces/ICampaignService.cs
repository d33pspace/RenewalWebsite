using RenewalWebsite.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Services
{
    public interface ICampaignService
    {
        Dictionary<PaymentCycle, string> GetCycles();
        void Save(Donation donation);
        Donation GetById(int id);
        StripePlan GetOrCreatePlan(Donation donation);
        int GetByUserId(string userId);
    }
}
