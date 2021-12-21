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
        [EmailAddress(ErrorMessageResourceName = "NotValidEmailAddress", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessageResourceName = "PasswordRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [StringLength(100, ErrorMessageResourceName = "Passwordmustbe6andmax100charcterslong", ErrorMessageResourceType = typeof(Resources.DataAnnotations), MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [RegularExpression(@"^(?=.*[A-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[*_%$@!#&()-?~`^+=,./\|:;{}])\S{6,}$", ErrorMessageResourceName = "PasswordStructure", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        //[RegularExpression(@"^(?=.*[A-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[*_%$@!#&()-?~`^+=,./\|:;{}])(?!.*[pPoO])\S{6,}$", ErrorMessageResourceName = "PasswordStructure", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessageResourceName = "PasswordAndConfirmPasswordNotMatch", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string ConfirmPassword { get; set; }

        public string TimeZone { get; set; }

        [Required(ErrorMessageResourceName = "CaptchaRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [StringLength(4)]
        public string CaptchaCode { get; set; }
    }
}
