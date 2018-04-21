using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public class CustomerRePaymentViewModel
    {
        public string UserName { get; set; }

        [Required(ErrorMessageResourceName = "NameRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "PersonName", ResourceType = typeof(Resources.DataAnnotations))]
        public string Name { get; set; }

        // Donation attributes
        public int DonationId { get; set; }
        public string CycleId { get; set; }

        public List<CustomerSubscriptionViewModel> Subscriptions { get; set; }

        // Address

        [Display(Name = "AddressLine1", ResourceType = typeof(Resources.DataAnnotations))]
        public string AddressLine1 { get; set; }

        [Display(Name = "AddressLine2", ResourceType = typeof(Resources.DataAnnotations))]
        public string AddressLine2 { get; set; }

        [Display(Name = "StateProvince", ResourceType = typeof(Resources.DataAnnotations))]
        public string State { get; set; }

        [Display(Name = "ZipPostalCode", ResourceType = typeof(Resources.DataAnnotations))]
        public string Zip { get; set; }

        [Display(Name = "City", ResourceType = typeof(Resources.DataAnnotations))]
        public string City { get; set; }

        [Display(Name = "Country", ResourceType = typeof(Resources.DataAnnotations))]
        public string Country { get; set; }

        public string Frequency { get; set; }

        public string Description { get; set; }

        public decimal Amount { get; set; }

        public string Last4Digit { get; set; }
        public string CardId { get; set; }

        //[Required]
        //public string Currency { get; set; }

        public string Paymentgatway { get; set; }

        public string DisableCurrencySelection { get; set; }

        public decimal ExchangeRate { get; set; }

        public bool IsCustom { get; set; }

        public string TimeZone { get; set; }

        public List<CountryViewModel> countries { get; set; }
    }
}
