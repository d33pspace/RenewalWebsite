using System;
using System.Globalization;
using RenewalWebsite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace RenewalWebsite.Controllers
{
    public class BaseController : Controller
    {
        public const string TempMessage = "$tempMessage";
        public readonly string DefaultCurrencyCookieName = ".AspNetCore.Currency";

        public string GetTempMessage()
        {
            var tempMessage = HttpContext.Session.GetString(TempMessage);
            HttpContext.Session.Remove(TempMessage);
            return tempMessage;
        }

        public void SetTempMessage(string message)
        {
            HttpContext.Session.SetString(TempMessage, message);
        }

        public void SetLanguage(string culture)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTime.UtcNow.AddYears(1) }
            );
        }

        public void SetCurrency(string culture)
        {
            Response.Cookies.Append(
                DefaultCurrencyCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTime.UtcNow.AddYears(1) }
            );
        }

    }
}