using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RenewalWebsite.Models;
using RenewalWebsite.Models.ManageViewModels;
using RenewalWebsite.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stripe;
using RenewalWebsite.Utility;
using Microsoft.Extensions.Localization;
using RestSharp;
using System.IO;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace RenewalWebsite.Controllers
{
    [Authorize]
    public class ManageController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly string _externalCookieScheme;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly ILoggerServicecs _loggerService;
        private readonly IInvoiceHistoryService _invoiceHistoryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<CurrencySettings> _currencySettings;
        private readonly ICurrencyService _currencyService;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IStringLocalizer<ManageController> _localizer;
        private readonly ICountryService _countryService;
        //private readonly IStringLocalizer<DonateController> _localizer;
        private EventLog log;

        public ManageController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          //IOptions<IdentityCookieOptions> identityCookieOptions,
          IEmailSender emailSender,
          ISmsSender smsSender,
          IOptions<StripeSettings> stripeSettings,
          ILoggerServicecs loggerService,
          IInvoiceHistoryService invoiceHistoryService,
          IHttpContextAccessor httpContextAccessor,
          IOptions<CurrencySettings> currencySettings,
          ICurrencyService currencyService,
          IHostingEnvironment hostingEnvironment,
          IStringLocalizer<ManageController> localizer,
          ICountryService countryService)
        //IStringLocalizer<DonateController> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            //_externalCookieScheme = identityCookieOptions.Value.ExternalCookieAuthenticationScheme;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _loggerService = loggerService;
            _stripeSettings = stripeSettings;
            //_localizer = localizer;
            _invoiceHistoryService = invoiceHistoryService;
            _httpContextAccessor = httpContextAccessor;
            _currencySettings = currencySettings;
            _currencyService = currencyService;
            _hostingEnvironment = hostingEnvironment;
            _localizer = localizer;
            _countryService = countryService;
        }

        //
        // GET: /Manage/Index
        [HttpGet]
        public async Task<IActionResult> Index(ManageMessageId? message = null, int tabId = 0)
        {
            try
            {
                // Optionaly use the region info to get default currency for user

                ViewData["StatusMessage"] =
                    message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                    : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                    : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                    : message == ManageMessageId.Error ? "An error has occurred."
                    : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                    : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                    : "";

                var user = await GetCurrentUserAsync();
                if (user == null)
                {
                    log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = "User not found." };
                    _loggerService.SaveEventLog(log);
                    return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = "User not found" });
                    //return View("Error");
                }

                List<CountryViewModel> countryList;
                if (_currencyService.GetCurrentLanguage().TwoLetterISOLanguageName.ToLower().Equals("en"))
                {
                    countryList = _countryService.GetAllCountry()
                                                         .Select(a => new CountryViewModel()
                                                         {
                                                             Code = a.ShortCode,
                                                             Country = a.CountryEnglish
                                                         }).OrderBy(a => a.Country).ToList();
                }
                else
                {
                    countryList = _countryService.GetAllCountry()
                                                         .Select(a => new CountryViewModel()
                                                         {
                                                             Code = a.ShortCode,
                                                             Country = a.CountryChinese
                                                         }).OrderBy(a => a.Country).ToList();
                }

                var model = new IndexViewModel
                {
                    HasPassword = await _userManager.HasPasswordAsync(user),
                    PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                    TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                    Logins = await _userManager.GetLoginsAsync(user),
                    BrowserRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user),
                    UserId = user.Id,
                    TokenId = user.StripeCustomerId,
                    Message = GetTempMessage(),
                    FullName = user.FullName,
                    AddressLine1 = user.AddressLine1,
                    AddressLine2 = user.AddressLine2,
                    State = user.State,
                    Zip = user.Zip,
                    City = user.City,
                    Country = user.Country,
                    countries = countryList
                };

                model.card = new CardViewModel();
                model.card.Name = user.FullName;

                try
                {
                    var CustomerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                    if (!string.IsNullOrEmpty(user.StripeCustomerId))
                    {
                        StripeCustomer objStripeCustomer = CustomerService.Get(user.StripeCustomerId);
                        if (objStripeCustomer.Sources != null && objStripeCustomer.Sources.TotalCount > 0 && objStripeCustomer.Sources.Data.Any())
                        {
                            var cardService = new StripeCardService(_stripeSettings.Value.SecretKey);
                            foreach (var cardSource in objStripeCustomer.Sources.Data)
                            {
                                model.card.Name = cardSource.Card.Name;
                                model.card.cardId = cardSource.Card.Id;
                                model.card.Last4Digit = cardSource.Card.Last4;
                            }
                        }
                    }
                }
                catch (StripeException sex)
                {
                    log = new EventLog() { EventId = (int)LoggingEvents.GET_CUSTOMER, LogLevel = LogLevel.Error.ToString(), Message = sex.Message };
                    _loggerService.SaveEventLog(log);
                    ModelState.AddModelError("CustomerNoFound", sex.Message);
                }
                ViewBag.TabId = tabId;
                return View(model);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message };
                _loggerService.SaveEventLog(log);
                return View(null);
            }
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel account)
        {
            ManageMessageId? message = ManageMessageId.Error;
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.RemoveLoginAsync(user, account.LoginProvider, account.ProviderKey);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    message = ManageMessageId.RemoveLoginSuccess;
                }
            }
            return RedirectToAction(nameof(ManageLogins), new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public IActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.PhoneNumber);
            await _smsSender.SendSmsAsync(model.PhoneNumber, "Your security code is: " + code);
            return RedirectToAction(nameof(VerifyPhoneNumber), new { PhoneNumber = model.PhoneNumber });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactorAuthentication()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                await _userManager.SetTwoFactorEnabledAsync(user, true);
                await _signInManager.SignInAsync(user, isPersistent: false);
                //_logger.LogInformation(1, "User enabled two-factor authentication.");
            }
            return RedirectToAction(nameof(Index), "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                await _userManager.SetTwoFactorEnabledAsync(user, false);
                await _signInManager.SignInAsync(user, isPersistent: false);
                //_logger.LogInformation(2, "User disabled two-factor authentication.");
            }
            return RedirectToAction(nameof(Index), "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        [HttpGet]
        public async Task<IActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, phoneNumber);
            // Send an SMS to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.AddPhoneSuccess });
                }
            }
            // If we got this far, something failed, redisplay the form
            ModelState.AddModelError(string.Empty, "Failed to verify phone number");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoneNumber()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.SetPhoneNumberAsync(user, null);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.RemovePhoneSuccess });
                }
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }

        //
        // GET: /Manage/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                var user = await GetCurrentUserAsync();
                if (user != null)
                {
                    var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        //_logger.LogInformation(3, "User changed their password successfully.");
                        return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    AddErrors(result);
                    return View(model);
                }
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.UPDATE_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message };
                _loggerService.SaveEventLog(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
                //return View(null);
            }
        }

        //
        // GET: /Manage/SetPassword
        [HttpGet]
        public IActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await GetCurrentUserAsync();
                if (user != null)
                {
                    var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction(nameof(Index), new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    AddErrors(result);
                    return View(model);
                }
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.SET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message };
                _loggerService.SaveEventLog(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
                //return View(null);
            }

        }

        //GET: /Manage/ManageLogins
        [HttpGet]
        public async Task<IActionResult> ManageLogins(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.AddLoginSuccess ? "The external login was added."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await _userManager.GetLoginsAsync(user);
            var otherLogins = _signInManager.GetExternalAuthenticationSchemesAsync().Result.ToList();
            ViewData["ShowRemoveButton"] = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkLogin(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.Authentication.SignOutAsync(_externalCookieScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action(nameof(LinkLoginCallback), "Manage");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return Challenge(properties, provider);
        }

        //
        // GET: /Manage/LinkLoginCallback
        [HttpGet]
        public async Task<ActionResult> LinkLoginCallback()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var info = await _signInManager.GetExternalLoginInfoAsync(await _userManager.GetUserIdAsync(user));
            if (info == null)
            {
                return RedirectToAction(nameof(ManageLogins), new { Message = ManageMessageId.Error });
            }
            var result = await _userManager.AddLoginAsync(user, info);
            var message = ManageMessageId.Error;
            if (result.Succeeded)
            {
                message = ManageMessageId.AddLoginSuccess;
                // Clear the existing external cookie to ensure a clean login process
                await HttpContext.Authentication.SignOutAsync(_externalCookieScheme);
            }
            return RedirectToAction(nameof(ManageLogins), new { Message = message });
        }

        public ActionResult AddNewCard()
        {
            NewCardViewModel card = new NewCardViewModel();
            return PartialView("_AddNewCard", card);
        }

        [HttpGet]
        public async Task<ActionResult> PaymentHistory()
        {
            var user = await GetCurrentUserAsync();
            CultureInfo us = new CultureInfo("en-US");
            SearchViewModel model = new SearchViewModel();
            model.FromDate = new DateTime((DateTime.Now.Year - 1), 1, 1).ToString("yyyy-MM-dd", us);
            model.ToDate = new DateTime((DateTime.Now.Year - 1), 12, 31).ToString("yyyy-MM-dd", us);

            DateTime FromDate = DateTime.ParseExact(model.FromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime ToDate = DateTime.ParseExact(model.ToDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            model.showUSD = false;

            List<InvoiceHistory> InvoiceHistory = _invoiceHistoryService.GetInvoiceHistory(FromDate, ToDate, user.Email);

            if (InvoiceHistory.Count > 0)
            {
                if (InvoiceHistory.Where(a => a.Currency.ToLower().Equals("cny")).Any() && InvoiceHistory.Where(a => a.Currency.ToLower().Equals("usd")).Any())
                {
                    model.displayUSD = false;
                    return PartialView("_PaymentHistory", model);
                }
                else if (InvoiceHistory.Where(a => a.Currency.ToLower().Equals("usd")).Any())
                {
                    model.displayUSD = false;
                    return PartialView("_PaymentHistory", model);
                }
                else
                {
                    model.displayUSD = true;
                    return PartialView("_PaymentHistory", model);
                }
            }

            return PartialView("_PaymentHistory", model);
        }

        [HttpPost]
        public async Task<ActionResult> GetPaymentHistory(SearchViewModel model)
        {
            var user = await GetCurrentUserAsync();
            DateTime FromDate = DateTime.ParseExact(model.FromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime ToDate = DateTime.ParseExact(model.ToDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            InvoiceHistoryModel invoiceHistoryModel = new InvoiceHistoryModel();
            invoiceHistoryModel.showUSDConversion = model.showUSD;
            invoiceHistoryModel.InvoiceHistory = _invoiceHistoryService.GetInvoiceHistory(FromDate, ToDate, user.Email);

            if (invoiceHistoryModel.InvoiceHistory.Count > 0)
            {
                if (invoiceHistoryModel.InvoiceHistory.Where(a => a.Currency.ToLower().Equals("cny")).Any() && invoiceHistoryModel.InvoiceHistory.Where(a => a.Currency.ToLower().Equals("usd")).Any())
                {
                    invoiceHistoryModel.displayConversion = true;
                    invoiceHistoryModel.showUSDOption = false;
                    invoiceHistoryModel.showUSDConversion = true;
                    invoiceHistoryModel.Type = 1;
                    return PartialView("_InvoiceHistory", invoiceHistoryModel);
                }
                else if (invoiceHistoryModel.InvoiceHistory.Where(a => a.Currency.ToLower().Equals("usd")).Any())
                {
                    invoiceHistoryModel.displayConversion = false;
                    invoiceHistoryModel.showUSDOption = false;
                    invoiceHistoryModel.showUSDConversion = false;
                    invoiceHistoryModel.Type = 2;
                    return PartialView("_InvoiceHistory", invoiceHistoryModel);
                }
                else
                {
                    invoiceHistoryModel.displayConversion = model.showUSD;
                    invoiceHistoryModel.showUSDOption = true;
                    invoiceHistoryModel.showUSDConversion = model.showUSD;
                    invoiceHistoryModel.Type = 3;
                    return PartialView("_InvoiceHistory", invoiceHistoryModel);
                }
            }

            return PartialView("_InvoiceHistory", invoiceHistoryModel);
        }

        [HttpPost]
        public async Task<JsonResult> DisplayUsdOption(SearchViewModel model)
        {
            var user = await GetCurrentUserAsync();
            DateTime FromDate = DateTime.ParseExact(model.FromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime ToDate = DateTime.ParseExact(model.ToDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            List<InvoiceHistory> InvoiceHistory = _invoiceHistoryService.GetInvoiceHistory(FromDate, ToDate, user.Email);

            if (InvoiceHistory.Count > 0)
            {
                if (InvoiceHistory.Where(a => a.Currency.ToLower().Equals("cny")).Any() && InvoiceHistory.Where(a => a.Currency.ToLower().Equals("usd")).Any())
                {
                    return Json(false);
                }
                else if (InvoiceHistory.Where(a => a.Currency.ToLower().Equals("usd")).Any())
                {
                    return Json(false);
                }
                else if (InvoiceHistory.Where(a => a.Currency.ToLower().Equals("cny")).Any())
                {
                    return Json(true);
                }
                else
                {
                    return Json(false);
                }
            }

            return Json(false);
        }

        [HttpPost]
        public async Task<FileResult> GetInvoicePdf(SearchViewModel model)
        {
            string language = _currencyService.GetCurrentLanguage().Name;
            var user = await GetCurrentUserAsync();
            DateTime FromDate = DateTime.ParseExact(model.FromDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            DateTime ToDate = DateTime.ParseExact(model.ToDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            List<InvoiceHistory> invoicehistoryList = _invoiceHistoryService.GetInvoiceHistory(FromDate, ToDate, user.Email);
            BaseFont baseFont;
            BaseFont baseFontEnglish;

            baseFontEnglish = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, false);
            baseFont = BaseFont.CreateFont(_hostingEnvironment.ContentRootPath + "\\wwwroot\\fonts\\simkai.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

            Font headerFont = new Font(baseFont, 14, 1, BaseColor.BLACK);
            Font font = new Font(baseFont, 10, 1, BaseColor.BLACK);
            Font fontEnglish = new Font(baseFontEnglish, 10, 1, BaseColor.BLACK);

            MemoryStream workStream = new MemoryStream();
            StringBuilder status = new StringBuilder("");
            DateTime dTime = DateTime.Now;
            //file name to be created   
            string strPDFFileName = string.Format("Invoice_History_" + dTime.ToString("dd-MMM-yyyy", new CultureInfo("en-US")) + "-" + ".pdf");
            Document doc = new Document();
            doc.SetPageSize(PageSize.A4);
            doc.SetMargins(0f, 0f, 0f, 0f);
            //Create PDF Table with 5 columns  
            PdfPTable tableLayout = null;
            //Create PDF Table  

            bool isAdd = true;
            if (invoicehistoryList.Where(a => a.Currency.ToLower().Equals("cny")).Any() && invoicehistoryList.Where(a => a.Currency.ToLower().Equals("usd")).Any())
            {
                isAdd = true;
            }
            else if (invoicehistoryList.Where(a => a.Currency.ToLower().Equals("usd")).Any())
            {
                isAdd = true;
            }
            else
            {
                if (model.showUSD == true)
                {
                    isAdd = true;
                }
                else
                {
                    isAdd = false;
                }
            }

            doc.SetMargins(15f, 15f, 130f, 15f);

            PdfWriter writer = PdfWriter.GetInstance(doc, workStream);
            writer.CloseStream = false;
            PDFHelper pDFHelper = new PDFHelper();
            pDFHelper.startDate = model.FromDate;
            pDFHelper.endDate = model.ToDate;
            pDFHelper.fullName = user.FullName;
            pDFHelper.logoPath = _hostingEnvironment.ContentRootPath + "\\wwwroot\\images\\Renewal Logo.jpg";
            pDFHelper.isAdd = isAdd;
            pDFHelper.sealImagePath = _hostingEnvironment.ContentRootPath + "\\wwwroot\\images\\renewal-seal-image.png";
            pDFHelper.RenewalHeader = _localizer["The Renewal Center"];
            pDFHelper.recordHeader = _localizer["A record of your giving from"];
            pDFHelper.To = _localizer["to"];
            pDFHelper.language = language;
            pDFHelper.fontPath = _hostingEnvironment.ContentRootPath + "\\wwwroot\\fonts\\simkai.ttf";
            writer.PageEvent = pDFHelper;

            writer.SetLanguage(language);

            //if (writer.PageNumber == 1)
            //{
            //    doc.SetMargins(15f, 15f, 130f, 110f);
            //}
            //else
            //{
            //    doc.SetMargins(15f, 15f, 10f, 10f);
            //}

            doc.Open();
            doc.Add(Add_Content_To_PDF(tableLayout, invoicehistoryList, model.showUSD, font, headerFont, model, isAdd, fontEnglish, language));

            //Add Content to PDF   
            //doc.Add();
            // Closing the document  
            doc.Close();

            byte[] byteInfo = workStream.ToArray();
            workStream.Write(byteInfo, 0, byteInfo.Length);
            workStream.Position = 0;

            return File(workStream.ToArray(), "application/pdf", strPDFFileName);
        }

        protected PdfPTable Add_Content_To_PDF(PdfPTable tableLayout, List<InvoiceHistory> invoicehistoryList, bool showUSD, Font font, Font headerFont, SearchViewModel model, bool isAdd, Font fontEnglish, string language)
        {
            string sealImagePath = _hostingEnvironment.ContentRootPath + "\\wwwroot\\images\\renewal-seal-image.png";
            bool displayConversion = false, showUSDConversion = false;
            int Type = 1;
            if (invoicehistoryList.Count > 0)
            {
                if (invoicehistoryList.Where(a => a.Currency.ToLower().Equals("cny")).Any() && invoicehistoryList.Where(a => a.Currency.ToLower().Equals("usd")).Any())
                {
                    displayConversion = true;
                    showUSDConversion = true;
                    Type = 1;
                }
                else if (invoicehistoryList.Where(a => a.Currency.ToLower().Equals("usd")).Any())
                {
                    displayConversion = false;
                    showUSDConversion = false;
                    Type = 2;
                }
                else
                {
                    displayConversion = model.showUSD;
                    showUSDConversion = model.showUSD;
                    Type = 3;
                }
            }

            int colspan = 0;
            if (Type == 1)
            {
                tableLayout = new PdfPTable(6);
                float[] headers = { 40, 30, 30, 40, 35, 40 };
                tableLayout.SetWidths(headers);
                colspan = 6;
            }
            else if (Type == 2)
            {
                tableLayout = new PdfPTable(4);
                float[] headers = { 40, 30, 30, 40 };
                tableLayout.SetWidths(headers);
                colspan = 4;
            }
            else
            {
                if (displayConversion == true)
                {
                    tableLayout = new PdfPTable(6);
                    float[] headers = { 40, 30, 30, 40, 35, 40 };
                    tableLayout.SetWidths(headers);
                    colspan = 6;
                }
                else
                {
                    tableLayout = new PdfPTable(4);
                    float[] headers = { 40, 30, 30, 40 };
                    tableLayout.SetWidths(headers);
                    colspan = 4;
                }
            }

            //tableLayout.PaddingTop = 200f;
            ////Add Title to the PDF file at the top  
            //tableLayout.AddCell(new PdfPCell(new Phrase(_localizer["Invoice History"], headerFont))
            //{
            //    Colspan = colspan,
            //    Border = 0,
            //    PaddingBottom = 15,
            //    HorizontalAlignment = Element.ALIGN_CENTER,
            //});


            ////Add header  
            AddCellToHeader(tableLayout, _localizer["Date"], language == "en-US" ? fontEnglish : font);
            AddCellToHeader(tableLayout, _localizer["Currency"], language == "en-US" ? fontEnglish : font);
            AddCellToHeader(tableLayout, _localizer["Amount"], language == "en-US" ? fontEnglish : font);
            if (displayConversion == true)
            {
                AddCellToHeader(tableLayout, _localizer["Exchange Rate"], language == "en-US" ? fontEnglish : font);
            }
            if (showUSDConversion == true)
            {
                AddCellToHeader(tableLayout, _localizer["USD Amount"], language == "en-US" ? fontEnglish : font);
            }
            AddCellToHeader(tableLayout, _localizer["Transaction Reference"], language == "en-US" ? fontEnglish : font);

            if (invoicehistoryList != null && invoicehistoryList.Count > 0)
            {
                ////Add body  
                foreach (InvoiceHistory invoice in invoicehistoryList)
                {
                    AddCellToBody(tableLayout, invoice.Date != null ? invoice.Date.ToString("yyyy-MM-dd", new CultureInfo("en-US")) : "", "center", fontEnglish);
                    AddCellToBody(tableLayout, _localizer[invoice.Currency], "center", language == "en-US" ? fontEnglish : font);
                    AddCellToBody(tableLayout, string.Format("{0:C}", invoice.Amount).Replace("$", "").Replace("¥", ""), "right", fontEnglish);
                    if (displayConversion == true)
                    {
                        AddCellToBody(tableLayout, invoice.ExchangeRate == null ? "" : string.Format("{0:C3}", invoice.ExchangeRate).Replace("$", "").Replace("¥", ""), "right", fontEnglish);
                    }
                    if (showUSDConversion == true)
                    {
                        AddCellToBody(tableLayout, string.Format("{0:C}", invoice.USDAmount).Replace("$", "").Replace("¥", ""), "right", fontEnglish);
                    }
                    AddCellToBody(tableLayout, invoice.InvoiceNumber, "center", fontEnglish);
                }

                if (Type == 1)
                {
                    tableLayout.AddCell(new PdfPCell(new Phrase(_localizer["Total"], language == "en-US" ? fontEnglish : font))
                    {
                        Colspan = 4,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                        BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                    });
                    tableLayout.AddCell(new PdfPCell(new Phrase(string.Format("{0:C}", invoicehistoryList.Sum(a => a.USDAmount)).Replace("$", "").Replace("¥", ""), fontEnglish))
                    {
                        Colspan = 1,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                        BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                    });
                    tableLayout.AddCell(new PdfPCell(new Phrase("", fontEnglish))
                    {
                        Colspan = 1,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                        BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                    });
                }
                else if (Type == 2)
                {
                    tableLayout.AddCell(new PdfPCell(new Phrase(_localizer["Total"], language == "en-US" ? fontEnglish : font))
                    {
                        Colspan = 2,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                        BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                    });
                    tableLayout.AddCell(new PdfPCell(new Phrase(string.Format("{0:C}", invoicehistoryList.Sum(a => a.Amount)).Replace("$", "").Replace("¥", ""), fontEnglish))
                    {
                        Colspan = 1,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 5,
                        BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                        BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                    });
                    tableLayout.AddCell(new PdfPCell(new Phrase("", fontEnglish))
                    {
                        Colspan = 1,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                        BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                    });
                }
                else
                {
                    if (displayConversion == true)
                    {
                        tableLayout.AddCell(new PdfPCell(new Phrase(_localizer["Total"], language == "en-US" ? fontEnglish : font))
                        {
                            Colspan = 2,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                            BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                        });
                        tableLayout.AddCell(new PdfPCell(new Phrase(string.Format("{0:C}", invoicehistoryList.Sum(a => a.Amount)).Replace("$", "").Replace("¥", ""), fontEnglish))
                        {
                            Colspan = 1,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                            BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                        });
                        tableLayout.AddCell(new PdfPCell(new Phrase(string.Format("{0:C}", invoicehistoryList.Sum(a => a.USDAmount)).Replace("$", "").Replace("¥", ""), fontEnglish))
                        {
                            Colspan = 2,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                            BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                        });
                        tableLayout.AddCell(new PdfPCell(new Phrase("", fontEnglish))
                        {
                            Colspan = 1,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5,
                            BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                            BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                        });
                    }
                    else
                    {
                        tableLayout.AddCell(new PdfPCell(new Phrase(_localizer["Total"], language == "en-US" ? fontEnglish : font))
                        {
                            Colspan = 2,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                            BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                        });
                        tableLayout.AddCell(new PdfPCell(new Phrase(string.Format("{0:C}", invoicehistoryList.Sum(a => a.Amount)).Replace("$", "").Replace("¥", ""), fontEnglish))
                        {
                            Colspan = 1,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5,
                            BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                            BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                        });
                        tableLayout.AddCell(new PdfPCell(new Phrase("", fontEnglish))
                        {
                            Colspan = 1,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5,
                            BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                            BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                        });
                    }
                }
            }
            else
            {
                tableLayout.AddCell(new PdfPCell(new Phrase(_localizer["No Record Found"], language == "en-US" ? fontEnglish : font))
                {
                    Colspan = colspan,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5,
                    BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White),
                    BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
                });
            }

            iTextSharp.text.Image footerImage = iTextSharp.text.Image.GetInstance(sealImagePath);
            footerImage.ScaleToFit(100f, 100f);
            PdfPCell pdfCellFooterImage = new PdfPCell(footerImage);
            pdfCellFooterImage.VerticalAlignment = Element.ALIGN_BOTTOM;
            pdfCellFooterImage.HorizontalAlignment = Element.ALIGN_LEFT;
            pdfCellFooterImage.Border = 0;
            pdfCellFooterImage.PaddingTop = 10f;
            pdfCellFooterImage.Colspan = 7;
            tableLayout.AddCell(pdfCellFooterImage);

            if (isAdd == true)
            {
                PdfPCell pdfCellFooter = new PdfPCell(new Phrase(_localizer["The Renewal Center is recognized as exempt under section 501(c)(3) of the Internal Revenue Code in the United States. Donors may deduct contributions as provided in section 170 of the Code."], language == "en-US" ? fontEnglish : font));
                pdfCellFooter.HorizontalAlignment = Element.ALIGN_CENTER;
                pdfCellFooter.Border = 0;
                pdfCellFooter.Colspan = 7;
                pdfCellFooter.BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.White);
                pdfCellFooter.BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black);
                tableLayout.AddCell(pdfCellFooter);
            }

            return tableLayout;
        }

        // Method to add single cell to the Header  
        private static void AddCellToHeader(PdfPTable tableLayout, string cellText, Font font)
        {
            tableLayout.AddCell(new PdfPCell(new Phrase(cellText, font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 5,
                BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.LightGray),
                BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
            });
        }

        // Method to add single cell to the body  
        private static void AddCellToBody(PdfPTable tableLayout, string cellText, string align, Font font)
        {
            tableLayout.AddCell(new PdfPCell(new Phrase(cellText, font))
            {
                HorizontalAlignment = string.IsNullOrEmpty(align) ? Element.ALIGN_LEFT : align.Equals("center") ? Element.ALIGN_CENTER : Element.ALIGN_RIGHT,
                Padding = 5,
                BackgroundColor = new iTextSharp.text.BaseColor(255, 255, 255),
                BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
            });
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            AddLoginSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        #endregion

        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveProfile(IndexViewModel profile)
        {
            ResultModel result = new ResultModel();
            try
            {
                var user = await GetCurrentUserAsync();

                if (user != null)
                {
                    if (user.FullName != profile.FullName ||
                   user.AddressLine1 != profile.AddressLine1 ||
                   user.AddressLine2 != profile.AddressLine2 ||
                   user.City != profile.City ||
                   user.State != profile.State ||
                   user.Country != profile.Country ||
                   user.Zip != profile.Zip)
                    {

                        var client = new RestClient("https://hooks.zapier.com/hooks/catch/2318707/z0jmup/");
                        var request = new RestRequest(Method.POST);
                        request.AddParameter("email", user.Email);
                        request.AddParameter("contact_name", profile.FullName);
                        request.AddParameter("language_preference", _currencyService.GetCurrentLanguage().TwoLetterISOLanguageName);
                        request.AddParameter("salutation", profile.FullName.Split(' ').Length == 1 ? profile.FullName : profile.FullName.Split(' ')[0]);
                        request.AddParameter("last_name", profile.FullName.Split(' ').Length == 1 ? "" : profile.FullName.Split(' ')[(profile.FullName.Split(' ').Length - 1)]);
                        request.AddParameter("address_line_1", profile.AddressLine1);
                        request.AddParameter("address_line_2", profile.AddressLine2);
                        request.AddParameter("server_location", _currencySettings.Value.ServerLocation);
                        request.AddParameter("ip_address", _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                        request.AddParameter("time_zone", profile.TimeZone);
                        request.AddParameter("city", profile.City);
                        request.AddParameter("state", profile.State);
                        request.AddParameter("zip", profile.Zip);
                        request.AddParameter("country", profile.Country);
                        // execute the request
                        IRestResponse response = client.Execute(request);
                    }

                    user.FullName = profile.FullName;
                    user.AddressLine1 = profile.AddressLine1;
                    user.AddressLine2 = profile.AddressLine2;
                    user.State = profile.State;
                    user.Zip = profile.Zip;
                    user.City = profile.City;
                    user.Country = profile.Country;
                    await _userManager.UpdateAsync(user);

                    result.data = "Profile updated successfully";
                    result.status = "1";
                }
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.UPDATE_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message };
                _loggerService.SaveEventLog(log);
                result.data = "Something went wrong, please try again";
                result.status = "0";
            }

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateCard(CardViewModel card)
        {
            ResultModel result = new ResultModel();
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                try
                {
                    var CardService = new StripeCardService(_stripeSettings.Value.SecretKey);
                    StripeCard objStripeCard = await CardService.GetAsync(user.StripeCustomerId, card.cardId);

                    StripeCardUpdateOptions updateCardOptions = new StripeCardUpdateOptions();
                    //updateCardOptions.Name = card.Name;
                    updateCardOptions.ExpirationMonth = card.ExpiryMonth;
                    updateCardOptions.ExpirationYear = card.ExpiryYear;

                    await CardService.UpdateAsync(user.StripeCustomerId, card.cardId, updateCardOptions);
                    result.data = "Card updated successfully";
                    result.status = "1";
                    return Json(result);
                }
                catch (StripeException ex1)
                {
                    log = new EventLog() { EventId = (int)LoggingEvents.UPDATE_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex1.Message };
                    _loggerService.SaveEventLog(log);
                    result.data = ex1.Message;
                    result.status = "0";
                }
                catch (Exception ex)
                {
                    log = new EventLog() { EventId = (int)LoggingEvents.UPDATE_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message };
                    _loggerService.SaveEventLog(log);
                    result.data = "Something went wrong, please try again";
                    result.status = "0";
                }
            }

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AddNewCard(NewCardViewModel card)
        {
            ResultModel result = new ResultModel();
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                try
                {
                    var customerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                    var ExistingCustomer = customerService.Get(user.StripeCustomerId);
                    if (ExistingCustomer.Sources != null && ExistingCustomer.Sources.TotalCount > 0 && ExistingCustomer.Sources.Data.Any())
                    {
                        var cardService = new StripeCardService(_stripeSettings.Value.SecretKey);
                        foreach (var cardSource in ExistingCustomer.Sources.Data)
                        {
                            cardService.Delete(user.StripeCustomerId, cardSource.Card.Id);
                        }
                    }

                    var customer = new StripeCustomerUpdateOptions
                    {
                        SourceCard = new SourceCard
                        {
                            Name = user.FullName,
                            Number = card.CardNumber,
                            Cvc = card.Cvc,
                            ExpirationMonth = card.ExpiryMonth,
                            ExpirationYear = card.NewExpiryYear,
                            //StatementDescriptor = _stripeSettings.Value.StatementDescriptor,
                            //Description = "",
                            AddressLine1 = user.AddressLine1,
                            AddressLine2 = user.AddressLine2,
                            AddressCity = user.City,
                            AddressState = user.State,
                            AddressCountry = user.Country,
                            AddressZip = user.Zip
                        }
                    };

                    var stripeCustomer = customerService.Update(user.StripeCustomerId, customer);
                    user.StripeCustomerId = stripeCustomer.Id;
                    result.data = "Card added successfully";
                    result.status = "1";
                    return Json(result);
                }
                catch (StripeException ex1)
                {
                    log = new EventLog() { EventId = (int)LoggingEvents.UPDATE_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex1.Message };
                    _loggerService.SaveEventLog(log);
                    result.data = ex1.Message;
                    result.status = "0";
                }
                catch (Exception ex)
                {
                    log = new EventLog() { EventId = (int)LoggingEvents.UPDATE_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message };
                    _loggerService.SaveEventLog(log);
                    result.data = "Something went wrong, please try again";
                    result.status = "0";
                }
            }

            return Json(result);
        }
    }
}
