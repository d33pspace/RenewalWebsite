using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public class ForgotPasswordMailModel
    {
        public string Name { get; set; }
        public string message { get; set; }
        public string ValidHours { get; set; }
        public string HeaderInformation { get; set; }
        public string ResetLink { get; set; }
        public string Thanks { get; set; }
        public string RenewalTeam { get; set; }
    }
}
