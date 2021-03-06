using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{

    public class ResponseModel
    {
        public Error error { get; set; }
    }

    public class Error
    {
        public string code { get; set; }
        public string decline_code { get; set; }
        public string doc_url { get; set; }
        public string message { get; set; }
        public string param { get; set; }
        public string type { get; set; }
    }
}
