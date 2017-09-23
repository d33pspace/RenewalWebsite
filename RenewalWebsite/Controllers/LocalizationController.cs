using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RenewalWebsite.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;
using RenewalWebsite.Utility;
using RenewalWebsite.Models;

namespace RenewalWebsite.Controllers
{
    public class LocalizationController : BaseController
    {
        private readonly ICurrencyService _currencyService;
        private readonly ILoggerServicecs _loggerService;
        private EventLog log;

        public LocalizationController(ICurrencyService currencyService, ILoggerServicecs loggerService)
        {
            _currencyService = currencyService;
            _loggerService = loggerService;
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
                log = new EventLog() { EventId = (int)LoggingEvents.SET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message };
                _loggerService.SaveEventLog(log);
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
                log = new EventLog() { EventId = (int)LoggingEvents.SET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message };
                _loggerService.SaveEventLog(log);
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
                log = new EventLog() { EventId = (int)LoggingEvents.UPDATE_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message };
                _loggerService.SaveEventLog(log);
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
                log = new EventLog() { EventId = (int)LoggingEvents.UPDATE_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message };
                _loggerService.SaveEventLog(log);
                return View(null);
            }
        }
    }
}