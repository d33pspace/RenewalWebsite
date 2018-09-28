using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models.ManageViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessageResourceName = "CurrentPasswordRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [DataType(DataType.Password)]
        [Display(Name = "CurrentPassword", ResourceType = typeof(Resources.DataAnnotations))]
        [RegularExpression(@"^(?=.*[A-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[*_%$@!#&()-?])(?!.*[pPoO])\S{6,}$", ErrorMessageResourceName = "PasswordStructure", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string OldPassword { get; set; }

        [Required(ErrorMessageResourceName = "NewPasswordRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [StringLength(100, ErrorMessageResourceName = "NewPasswordmustbe6andmax100charcterslong", ErrorMessageResourceType = typeof(Resources.DataAnnotations), MinimumLength = 6)]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[*_%$@!#&()-?])(?!.*[pPoO])\S{6,}$", ErrorMessageResourceName = "PasswordStructure", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "NewPassword", ResourceType = typeof(Resources.DataAnnotations))]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "ConfirmNewPassword", ResourceType = typeof(Resources.DataAnnotations))]
        [Compare("NewPassword", ErrorMessageResourceName = "NewPasswordAndConfirmPasswordNotMatch", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string ConfirmPassword { get; set; }
    }
}
