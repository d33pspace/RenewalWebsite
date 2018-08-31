using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.SettingModels
{
    public class EmailSettings
    {
        public string FromEmail { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string Host { get; set; }
        public bool EnableSsl { get; set; }
        public string EmailUserName { get; set; }
    }
}
