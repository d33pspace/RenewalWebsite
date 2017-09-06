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

namespace RenewalWebsite.Controllers
{
    public class DonateController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDonationService _donationService;
        private readonly ICampaignService _campaignService;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly IOptions<ExchangeRate> _exchangeSettings;
        private readonly IOptions<CampaignSettings> _campaignSettings;

        const string SessionKey = "sessionKey";

        public DonateController(UserManager<ApplicationUser> userManager,
            IDonationService donationService,
            IOptions<StripeSettings> stripeSettings,
            IOptions<ExchangeRate> exchangeSettings,
            IOptions<CampaignSettings> campaignSettings,
            ICampaignService campaignService)
        {
            _userManager = userManager;
            _donationService = donationService;
            _stripeSettings = stripeSettings;
            _exchangeSettings = exchangeSettings;
            _campaignSettings = campaignSettings;
            _campaignService = campaignService;
        }

        public IActionResult Index()
        {
            var agent = Request.Headers["User-Agent"];
            Console.WriteLine(agent.ToString());
            ViewBag.Browser = agent.ToString();
            var model = new DonationViewModel(_donationService.DonationOptions)
            {
                DonationCycles = GetDonationCycles,
                ExchangeRate = _exchangeSettings.Value.Rate
            };

            return View(model);
        }

        public IActionResult Details()
        {
            return View();
        }

        public IActionResult Thanks()
        {
            return View();
        }

        public IActionResult Campaign_2017_08()
        {
            //TODO: This code repeated
            var agent = Request.Headers["User-Agent"];
            Console.WriteLine(agent.ToString());
            ViewBag.Browser = agent.ToString();
            var model = new DonationViewModel(_campaignService.DonationOptions)
            {
                DonationCycles = GetDonationCycles,
                ExchangeRate = _exchangeSettings.Value.Rate
            };
            return View(model);
        }

        public IActionResult WeChat_2017_08()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Create()
        {
            var value = HttpContext.Session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(value))
            {
                var model = JsonConvert.DeserializeObject<Donation>(value);
                return RedirectToAction("Payment", "Donation", new { Id = model.Id });
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Create(DonationViewModel donation)
        {
            var agent = Request.Headers["User-Agent"];
            Console.WriteLine(agent.ToString());
            ViewBag.Browser = agent.ToString();

            donation.DonationOptions = _donationService.DonationOptions;
            donation.ExchangeRate = _exchangeSettings.Value.Rate;
            donation.DonationCycles = GetDonationCycles;

            if (donation.SelectedAmount == 0) //Could be better
            {
                ModelState.AddModelError("amount", "Select amount");
                return View("Index", donation);
            }


            if (Math.Abs(donation.GetAmount()) < 1)
            {
                ModelState.AddModelError("amount", "Donation amount cannot be zero or less");
                return View("Index", donation);
            }

            if (!ModelState.IsValid)
            {                
                return View("Index", donation);
            }

            var model = new Donation
            {
                CycleId = donation.CycleId,
                DonationAmount = donation.DonationAmount,
                SelectedAmount = donation.SelectedAmount,
                currency = "",
                TransactionDate = DateTime.Now
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

        [Authorize]
        public async Task<IActionResult> CreateCampaign()
        {
            var value = HttpContext.Session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(value))
            {
                var model = JsonConvert.DeserializeObject<Donation>(value);
                return Redirect("/Donation/Payment/campaign/" + model.Id);
                //return RedirectToAction("Payment/campaign", "Donation", new { Id = model.Id });
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCampaign(DonationViewModel donation)
        {
            var agent = Request.Headers["User-Agent"];
            Console.WriteLine(agent.ToString());
            ViewBag.Browser = agent.ToString();

            donation.DonationOptions = _campaignService.DonationOptions;
            donation.ExchangeRate = _exchangeSettings.Value.Rate;
            donation.DonationCycles = GetDonationCycles;

            if (donation.SelectedAmount == 0) //Could be better
            {
                ModelState.AddModelError("amount", "Select amount");
                return View("Campaign_2017_08", donation);
            }


            if (Math.Abs(donation.GetAmount()) < 1)
            {
                ModelState.AddModelError("amount", "Donation amount cannot be zero or less");
                return View("Campaign_2017_08", donation);
            }

            if (!ModelState.IsValid)
            {
                return View("Campaign_2017_08", donation);
            }

            var model = new Donation
            {
                CycleId = donation.CycleId,
                DonationAmount = donation.DonationAmount,
                SelectedAmount = donation.SelectedAmount,
                currency = "",
                TransactionDate = DateTime.Now
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

            //return RedirectToRoute("CustomRoute");
            //string action = "Payment/campaign";
            //return RedirectToAction(action.Replace("%2f","/"), "Donation", new { id = model.Id });
            return Redirect("/Donation/Payment/campaign/" + model.Id);
        }

        private List<SelectListItem> GetDonationCycles => _donationService
            .GetCycles()
            .Select(b => new SelectListItem
            {
                Value = ((int)b.Key).ToString(),
                Text = b.Value
            }).ToList();

        private Task<ApplicationUser> GetCurrentUserAsync() =>
            _userManager.GetUserAsync(HttpContext.User);
    }
}
