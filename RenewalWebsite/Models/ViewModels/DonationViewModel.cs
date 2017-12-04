using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using RenewalWebsite.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public class DonationViewModel
    {
        public int Id { get; set; }

        public string CycleId { get; set; }

        [RegularExpression(@"\d+(\.\d{1,2})?", ErrorMessage = "Invalid price")]
        public decimal? DonationAmount { get; set; }

        public List<SelectListItem> DonationCycles { get; set; }

        public int SelectedAmount { set; get; }

        //public List<DonationListOption> DonationOptions { get; set; }

        private const int StripeMultiplier = 100;

        public string PaymentGatway { get; set; }

        //public decimal ExchangeRate { get; set; }

        public string Reason { get; set; }

        public bool IsCustom { get; set; }

        public DonationViewModel()
        {
        }
        
        public string GetCycle(string CycleId)
        {
            var pc = EnumInfo<PaymentCycle>.GetValue(CycleId);
            return EnumInfo<PaymentCycle>.GetDescription(pc);
        }
        
        public static implicit operator DonationViewModel(Donation donation)
        {
            return new DonationViewModel();
        }
    }

    public class ErrorViewModel
    {
        public string Error { get; set; }
    }
}
