using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models.ManageViewModels
{
    public class CardViewModel
    {
        public string cardId { get; set; }

        public string Last4Digit { get; set; }

        //[Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required]
        [RegularExpression(@"\d{3}", ErrorMessageResourceName = "InvalidCVC", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string Cvc { get; set; }

        [Range(1, 12, ErrorMessageResourceName = "InvalidMonth", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "Expiry Date")]
        public int ExpiryMonth { get; set; }

        [Range(17, 30, ErrorMessageResourceName = "InvalidYear", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public int ExpiryYear { get; set; }

        public string CardBrand { get; set; }

        public string CardClass { get; set; }
    }

    public class NewCardViewModel
    {
        [Required(ErrorMessageResourceName = "CardRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [CreditCard(ErrorMessageResourceName = "InvalidCard", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        [Display(Name = "CardNumber", ResourceType = typeof(Resources.DataAnnotations))]
        public string CardNumber { get; set; }

        [Required]
        [RegularExpression(@"\d{3}", ErrorMessageResourceName = "InvalidCVC", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string Cvc { get; set; }

        [Range(1, 12, ErrorMessageResourceName = "InvalidMonth", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public int ExpiryMonth { get; set; }

        //[Range(17, 30, ErrorMessageResourceName = "InvalidYear", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public int NewExpiryYear { get; set; }
    }
}
