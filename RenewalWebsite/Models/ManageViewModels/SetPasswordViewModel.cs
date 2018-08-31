using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models.ManageViewModels
{
    public class SetPasswordViewModel
    {
        [Required(ErrorMessageResourceName = "PasswordRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [StringLength(100, ErrorMessageResourceName = "Passwordmustbe6andmax100charcterslong", ErrorMessageResourceType = typeof(Resources.DataAnnotations), MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password", ResourceType = typeof(Resources.DataAnnotations))]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "ConfirmPassword", ResourceType = typeof(Resources.DataAnnotations))]
        [Compare("NewPassword", ErrorMessageResourceName = "PasswordAndConfirmPasswordNotMatch", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string ConfirmPassword { get; set; }
    }
}
