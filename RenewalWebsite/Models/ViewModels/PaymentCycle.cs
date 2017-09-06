using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public enum PaymentCycle
    {
        [Description("Monthly")]
        Month = 1,
        [Description("Quarterly")]
        Quarter = 2,
        [Description("Yearly")]
        Year = 3,
        [Description("One Time")]
        OneTime = 4
    }
}
