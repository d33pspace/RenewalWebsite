using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RenewalWebsite.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;
using RenewalWebsite.Utility;

namespace RenewalWebsite.Controllers
{
    public class LocalizationController : BaseController
    {
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<LocalizationController> _logger;

        public LocalizationController(ICurrencyService currencyService, ILogger<LocalizationController> logger)
        {
            _currencyService = currencyService;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            try
            {
                SetLanguage(culture);
                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError((int)LoggingEvents.SET_ITEM, ex.Message);
                return View(null);
            }
        }

        [HttpPost]
        public IActionResult SetCurrency(string culture, string returnUrl)
        {
            try
            {
                SetCurrency(culture);
                return LocalRedirect(returnUrl);
            }
            catch(Exception ex)
            {
                _logger.LogError((int)LoggingEvents.SET_ITEM, ex.Message);
                return View(null);
            }
        }

        public IActionResult ToggleLanguage(string returnUrl)
        {
            try
            {
                var feature = Request.HttpContext.Features.Get<IRequestCultureFeature>();
                // Culture contains the information of the requested culture
                var currentCulture = feature.RequestCulture.Culture;
                var culture = currentCulture.Name == "en-US" ? "zh-CN" : "en-US";
                SetLanguage(culture);
                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError((int)LoggingEvents.UPDATE_ITEM, ex.Message);
                return View(null);
            }
        }

        public IActionResult ToggleCurrency(string returnUrl)
        {
            try
            {
                var current = _currencyService.GetCurrent();
                var culture = current.Name == "en-US" ? "zh-CN" : "en-US";
                SetCurrency(culture);
                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError((int)LoggingEvents.UPDATE_ITEM, ex.Message);
                return View(null);
            }
        }
    }
}