using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite
{
    public class CampaignSettings
    {
        public string CampaignName { get; set; }
        public int Defaultcamp { get; set; }
        public List<Campaign> Campaign { get; set; }
    }

    public class Campaign
    {
        public decimal Value { get; set; }
        public int Type { get; set; }
        public string CurrencyType { get; set; }
    }

    public class ExchangeRate
    {
        public decimal Rate { get; set; }
    }
}
