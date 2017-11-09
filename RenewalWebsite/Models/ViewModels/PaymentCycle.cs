using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public enum PaymentCycle
    {
        [Description("monthly")]
        Month = 1,
        [Description("quarterly")]
        Quarter = 2,
        [Description("yearly")]
        Year = 3,
        [Description("one time")]
        OneTime = 4
    }
}
