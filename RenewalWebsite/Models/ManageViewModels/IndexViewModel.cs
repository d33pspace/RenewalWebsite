using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RenewalWebsite.Models.ManageViewModels
{
    public class IndexViewModel
    {
        
        [Display(Name = "FullName", ResourceType = typeof(Resources.DataAnnotations))]
        [Required(ErrorMessageResourceName = "FullNameRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string FullName { get; set; }

        [Display(Name = "AddressLine1", ResourceType = typeof(Resources.DataAnnotations))]
        public string AddressLine1 { get; set; }

        [Display(Name = "AddressLine2", ResourceType = typeof(Resources.DataAnnotations))]
        public string AddressLine2 { get; set; }

        [Display(Name = "State", ResourceType = typeof(Resources.DataAnnotations))]
        public string State { get; set; }

        [Display(Name = "Zip", ResourceType = typeof(Resources.DataAnnotations))]
        public string Zip { get; set; }

        [Display(Name = "City", ResourceType = typeof(Resources.DataAnnotations))]
        public string City { get; set; }

        [Display(Name = "Country", ResourceType = typeof(Resources.DataAnnotations))]
        public string Country { get; set; }

        public bool HasPassword { get; set; }

        public IList<UserLoginInfo> Logins { get; set; }

        public string PhoneNumber { get; set; }

        public bool TwoFactor { get; set; }

        public bool BrowserRemembered { get; set; }

        public string UserId { get; set; }

        public string TokenId { get; set; }

        public string Message { get; set; }

        public CardViewModel card { get; set; }

        public string TimeZone { get; set; }

        public List<CountryViewModel> countries { get; set; }
    }
}
