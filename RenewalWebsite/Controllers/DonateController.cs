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
        private readonly IOptions<StripeSettings> _stripeSettings;
        const string SessionKey = "sessionKey";
        
        public DonateController(UserManager<ApplicationUser> userManager, IDonationService donationService, IOptions<StripeSettings> stripeSettings)
        {
            _userManager = userManager;
            _donationService = donationService;
            _stripeSettings = stripeSettings;
        }
        
        public IActionResult Index()
        {
            var agent = Request.Headers["User-Agent"];
            Console.WriteLine(agent.ToString());
            ViewBag.Browser = agent.ToString();
            var model = new DonationViewModel(_donationService.DonationOptions)
            {
                DonationCycles = GetDonationCycles
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
            var model = new DonationViewModel(_donationService.DonationOptions)
            {
                DonationCycles = GetDonationCycles
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
            donation.DonationOptions = _donationService.DonationOptions;

            if (donation.SelectedAmount == 0) //Could be better
            {
                ModelState.AddModelError("amount", "Select amount");
            }


            if (Math.Abs(donation.GetAmount()) < 1)
            {
                ModelState.AddModelError("amount", "Donation amount cannot be zero or less");
            }

            if (!ModelState.IsValid)
            {
                donation.DonationCycles = GetDonationCycles;
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
