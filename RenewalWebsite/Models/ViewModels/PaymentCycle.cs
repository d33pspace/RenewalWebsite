using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using RenewalWebsite.Utility;

namespace RenewalWebsite.Models
{
    public enum PaymentCycle
    {
        [LocalizedDescription("monthly", typeof(Resources.DataAnnotations))]
        Month = 1,
        [LocalizedDescription("quarterly", typeof(Resources.DataAnnotations))]
        Quarter = 2,
        [LocalizedDescription("yearly", typeof(Resources.DataAnnotations))]
        Year = 3,
        [LocalizedDescription("oneTime", typeof(Resources.DataAnnotations))]
        OneTime = 4
    }

    public enum PaymentCycleEn
    {
        [Description("monthly")]
        Month = 1,
        [Description("quarterly")]
        Quarter = 2,
        [Description("yearly")]
        Year = 3,
        [Description("oneTime")]
        OneTime = 4
    }
}
