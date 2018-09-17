using RenewalWebsite.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models.ViewModels
{
    public class UserPreferenceViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Language { get; set; }

        [Required(ErrorMessageResourceName = "PreferenceName", ErrorMessageResourceType = typeof(DataAnnotations))]
        public string Salutation { get; set; }

        [Required(ErrorMessageResourceName = "PreferenceEmail", ErrorMessageResourceType = typeof(DataAnnotations))]
        [EmailAddress(ErrorMessageResourceName = "PreferenceValidEmail", ErrorMessageResourceType = typeof(DataAnnotations))]
        public string NewEmail { get; set; }
    }
}
