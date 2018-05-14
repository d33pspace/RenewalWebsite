using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public class SearchViewModel
    {
        [Required(ErrorMessageResourceName = "FromDateRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string FromDate { get; set; }

        [Required(ErrorMessageResourceName = "ToDateRequired", ErrorMessageResourceType = typeof(Resources.DataAnnotations))]
        public string ToDate { get; set; }

        public bool showUSD { get; set; }

        [Required]
        public bool displayUSD { get; set; }
    }

    public class InvoiceHistoryViewModel
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
    }
}
