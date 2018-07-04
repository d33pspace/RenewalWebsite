using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using RenewalWebsite.Models;
using RenewalWebsite.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using RenewalWebsite.Utility;

namespace RenewalWebsite.Controllers
{
    public class DonateController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDonationService _donationService;
        private readonly ICampaignService _campaignService;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly IStringLocalizer<DonateController> _localizer;
        private readonly ILoggerServicecs _loggerService;
        private EventLog log;

        const string SessionKey = "sessionKey";

        public DonateController(UserManager<ApplicationUser> userManager,
            IDonationService donationService,
            IOptions<StripeSettings> stripeSettings,
            ICampaignService campaignService,
            IStringLocalizer<DonateController> localizer,
            ILoggerServicecs loggerService)
        {
            _userManager = userManager;
            _donationService = donationService;
            _stripeSettings = stripeSettings;
            _campaignService = campaignService;
            _localizer = localizer;
            _loggerService = loggerService;

        }

        public IActionResult Index()
        {

            try
            {
                var agent = Request.Headers["User-Agent"];
                Console.WriteLine(agent.ToString());
                ViewBag.Browser = agent.ToString();
                var model = new DonationViewModel()
                {
                    DonationCycles = GetDonationCycles
                };

                return View(model);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        public IActionResult Details()
        {
            return View();
        }

        public IActionResult Thanks()
        {
            return View();
        }

        public IActionResult Campaign()
        {
            try
            {
                var agent = Request.Headers["User-Agent"];
                Console.WriteLine(agent.ToString());
                ViewBag.Browser = agent.ToString();
                var model = new DonationViewModel()
                {
                    DonationCycles = GetDonationCycles
                };
                return View(model);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        public IActionResult Error()
        {
            return View();
        }

        [Authorize]
        public IActionResult Create()
        {
            try
            {
                var value = HttpContext.Session.GetString(SessionKey);
                if (!string.IsNullOrEmpty(value))
                {
                    var model = JsonConvert.DeserializeObject<Donation>(value);
                    return RedirectToAction("Payment", "Donation", new { Id = model.Id });
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Create(DonationViewModel donation)
        {
            try
            {
                var agent = Request.Headers["User-Agent"];
                Console.WriteLine(agent.ToString());
                ViewBag.Browser = agent.ToString();
                donation.DonationCycles = GetDonationCycles;

                if (donation.SelectedAmount == 0)
                {
                    ModelState.AddModelError("amount", _localizer["Select amount"]);
                    return View("Index", donation);
                }

                if (Math.Abs((decimal)donation.DonationAmount) <= 0)
                {
                    ModelState.AddModelError("amount", _localizer["Donation amount cannot be zero or less"]);
                    return View("Index", donation);
                }

                if (!ModelState.IsValid) { return View("Index", donation); }

                var model = new Donation
                {
                    CycleId = donation.CycleId,
                    DonationAmount = donation.DonationAmount,
                    SelectedAmount = donation.SelectedAmount,
                    TransactionDate = DateTime.Now,
                    Reason = donation.Reason,
                    IsCustom = donation.IsCustom
                };
                _donationService.Save(model);

                // If user is not authenticated, lets save the details on the session cache and we get them after authentication
                if (!User.Identity.IsAuthenticated)
                {
                    var value = HttpContext.Session.GetString(SessionKey);
                    if (string.IsNullOrEmpty(value))
                    {
                        var donationJson = JsonConvert.SerializeObject(model);
                        HttpContext.Session.SetString(SessionKey, donationJson);
                    }
                    return RedirectToAction("Login", "Account", new { returnUrl = Request.Path });
                }

                return RedirectToAction("Payment", "Donation", new { id = model.Id });
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GENERATE_ITEMS, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        [Authorize]
        public IActionResult CreateCampaign()
        {
            try
            {
                var value = HttpContext.Session.GetString(SessionKey);
                if (!string.IsNullOrEmpty(value))
                {
                    var model = JsonConvert.DeserializeObject<Donation>(value);
                    return Redirect("/Donation/Payment/campaign/" + model.Id);
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GENERATE_ITEMS, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateCampaign(DonationViewModel donation)
        {
            try
            {
                var agent = Request.Headers["User-Agent"];
                ViewBag.Browser = agent.ToString();
                donation.DonationCycles = GetDonationCycles;

                if (donation.SelectedAmount == 0) //Could be better
                {
                    ModelState.AddModelError("amount", _localizer["Select amount"]);
                    return View("Campaign_2017_08", donation);
                }

                if (Math.Abs((decimal)donation.DonationAmount) < 1)
                {
                    ModelState.AddModelError("amount", _localizer["Donation amount cannot be zero or less"]);
                    return View("Campaign_2017_08", donation);
                }

                if (!ModelState.IsValid) { return View("Campaign_2017_08", donation); }

                var model = new Donation
                {
                    CycleId = donation.CycleId,
                    DonationAmount = donation.DonationAmount,
                    SelectedAmount = donation.SelectedAmount,
                    Currency = "",
                    TransactionDate = DateTime.Now,
                    Reason = donation.Reason,
                    IsCustom = donation.IsCustom
                };
                _donationService.Save(model);

                // If user is not authenticated, lets save the details on the session cache and we get them after authentication
                if (!User.Identity.IsAuthenticated)
                {
                    var value = HttpContext.Session.GetString(SessionKey);
                    if (string.IsNullOrEmpty(value))
                    {
                        var donationJson = JsonConvert.SerializeObject(model);
                        HttpContext.Session.SetString(SessionKey, donationJson);
                    }
                    return RedirectToAction("Login", "Account", new { returnUrl = Request.Path });
                }

                return Redirect("/Donation/Payment/campaign/" + model.Id);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GENERATE_ITEMS, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        private List<SelectListItem> GetDonationCycles => _donationService
            .GetCycles()
            .Select(b => new SelectListItem
            {
                Value = ((int)b.Key).ToString(),
                Text = _localizer[b.Value]
            }).ToList();

        private Task<ApplicationUser> GetCurrentUserAsync() =>
            _userManager.GetUserAsync(HttpContext.User);
    }
}
