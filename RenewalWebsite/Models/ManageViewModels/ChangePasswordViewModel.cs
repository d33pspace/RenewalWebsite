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
        [DataType(DataType.Password, ErrorMessageResourceName = "InvalidCurrentPassword", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "CurrentPassword", ResourceType = typeof(Resources.DataAnnotations))]
        public string OldPassword { get; set; }

        [Required(ErrorMessageResourceName = "NewPasswordRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [StringLength(100, ErrorMessageResourceName = "PasswordLength", ErrorMessageResourceType = typeof(Resources.DataAnnotations), MinimumLength = 6)]
        [DataType(DataType.Password, ErrorMessageResourceName = "InvalidNewPassword", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "NewPassword", ResourceType = typeof(Resources.DataAnnotations))]
        public string NewPassword { get; set; }

        [DataType(DataType.Password, ErrorMessageResourceName = "InvalidConfirmPassword", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "ConfirmPassword", ResourceType = typeof(Resources.DataAnnotations))]
        [Compare("NewPassword", ErrorMessageResourceName = "PasswordMatch", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string ConfirmPassword { get; set; }
    }
}
