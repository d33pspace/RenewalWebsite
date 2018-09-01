using RenewalWebsite.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Utility
{
    public static class GeneralUtility
    {
        public static List<Year> GetYeatList()
        {
            List<Year> years = new List<Year>();
            Year year;
            int startYear = DateTime.Now.Year;
            for (var i = 0; i < 10; i++)
            {
                year = new Year();
                year.Id = Convert.ToInt32((startYear + i).ToString().Substring(2, 2));
                year.YearValue = (startYear + i).ToString();
                years.Add(year);
            }
            return years;
        }
    }
}
