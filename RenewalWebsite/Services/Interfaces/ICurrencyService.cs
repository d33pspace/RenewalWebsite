using RenewalWebsite.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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
