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
        [Display(Name = "Email", ResourceType = typeof(Resources.DataAnnotations))]
        [EmailAddress(ErrorMessageResourceName = "InvalidEmail", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string Email { get; set; }

        [Required(ErrorMessageResourceName = "PasswordRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "Password", ResourceType = typeof(Resources.DataAnnotations))]
        [DataType(DataType.Password,ErrorMessageResourceName = "InvalidPassword",ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string Password { get; set; }

        [Display(Name = "RememberMe", ResourceType = typeof(Resources.DataAnnotations))]
        public bool RememberMe { get; set; }
    }
}
