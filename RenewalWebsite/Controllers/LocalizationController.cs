using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RenewalWebsite.Services;
using Microsoft.AspNetCore.Localization;

namespace RenewalWebsite.Controllers
{
    public class LocalizationController : BaseController
    {
        private readonly ICurrencyService _currencyService;

        public LocalizationController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            SetLanguage(culture);
            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        public IActionResult SetCurrency(string culture, string returnUrl)
        {
            SetCurrency(culture);
            return LocalRedirect(returnUrl);
        }

        public IActionResult ToggleLanguage(string returnUrl)
        {
            var feature = Request.HttpContext.Features.Get<IRequestCultureFeature>();
            // Culture contains the information of the requested culture
            var currentCulture = feature.RequestCulture.Culture;
            var culture = currentCulture.Name == "en-US" ? "zh-CN" : "en-US";
            SetLanguage(culture);
            return LocalRedirect(returnUrl);
        }

        public IActionResult ToggleCurrency(string returnUrl)
        {
            var current = _currencyService.GetCurrent();
            var culture = current.Name == "en-US" ? "zh-CN" : "en-US";
            SetCurrency(culture);
            return LocalRedirect(returnUrl);
        }
    }
}