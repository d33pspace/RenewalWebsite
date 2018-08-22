using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models.AccountViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessageResourceName = "EmailRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [EmailAddress(ErrorMessageResourceName = "NotValidEmailAddress", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string Email { get; set; }

        [Required(ErrorMessageResourceName = "PasswordRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
