﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessageResourceName = "EmailRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [EmailAddress(ErrorMessageResourceName = "NotValidEmailAddress", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string Email { get; set; }
        
    }
}
