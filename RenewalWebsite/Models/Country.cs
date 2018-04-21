using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public class Country
    {
        public int Id { get; set; }
        public string CountryEnglish { get; set; }
        public string CountryChinese { get; set; }
        public string ShortCode { get; set; }
        public int order { get; set; }
    }
}
