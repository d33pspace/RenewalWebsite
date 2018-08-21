using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public class UnsubscribeUserViewModel
    {
        public string email { get; set; }
        public bool isUnsubscribe { get; set; }
        public string language { get; set; }
        public string feedback { get; set; }
        public string salutation { get; set; }
    }
}
