using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public class Donation
    {
        public int Id { get; set; }

        public string CycleId { get; set; }

        public decimal? DonationAmount { get; set; }

        public string UserId { get; set; }

        public virtual ApplicationUser User { get; set; }

        public DateTime? TransactionDate { get; set; }

        public int SelectedAmount { get; set; }

        public string Currency { get; set; }

        public string Reason { get; set; }

        public bool IsCustom { get; set; }
    }
}
