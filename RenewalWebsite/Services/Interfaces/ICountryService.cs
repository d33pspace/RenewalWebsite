using RenewalWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Services
{
    public interface ICountryService
    {
        List<Country> GetAllCountry();
    }
}
