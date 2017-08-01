using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using RenewalWebsite.Models;

namespace RenewalWebsite.Services
{
    public interface ICurrencyService
    {
        CultureInfo GetCurrent();
        List<Currency> GetAll();
        string GetSymbol(CultureInfo culture);
        string GetISOName(CultureInfo culture);
    }
}