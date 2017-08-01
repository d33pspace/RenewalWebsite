using System.ComponentModel.DataAnnotations;

namespace RenewalWebsite.Models
{
    public class Currency
    {
        public string Symbol { get; set; }
        public string CultureName { get; set; }
    }
}