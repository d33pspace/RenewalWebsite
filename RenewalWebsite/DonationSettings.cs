using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite
{
    public class DonationSettings
    {
        public List<Donate> Donate { get; set; }
    }

    public class Donate
    {
        public int Id { get; set; }
        public decimal Value { get; set; }
    }
}
