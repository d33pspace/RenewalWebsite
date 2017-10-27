using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models.AccountViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessageResourceName = "EmailRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [EmailAddress(ErrorMessageResourceName = "InvalidEmail", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "Email", ResourceType = typeof(Resources.DataAnnotations))]
        public string Email { get; set; }

        [Required(ErrorMessageResourceName = "PasswordRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [StringLength(100, ErrorMessageResourceName = "PasswordLength", ErrorMessageResourceType = typeof(Resources.DataAnnotations), MinimumLength = 6)]
        [DataType(DataType.Password, ErrorMessageResourceName = "InvalidPassword", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "Password", ResourceType = typeof(Resources.DataAnnotations))]
        public string Password { get; set; }

        [DataType(DataType.Password, ErrorMessageResourceName = "InvalidPassword", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "ConfirmPassword", ResourceType = typeof(Resources.DataAnnotations))]
        [Compare("Password", ErrorMessageResourceName = "PasswordMatch", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string ConfirmPassword { get; set; }
    }
}
