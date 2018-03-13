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
          IInvoiceHistoryService invoiceHistoryService)
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
                    Country = user.Country
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
        public ActionResult PaymentHistory()
        {
            SearchViewModel model = new SearchViewModel();
            model.FromDate = new DateTime(DateTime.Now.Year, 1, 1).ToString("dd-MMM-yyyy");
            model.ToDate = new DateTime(DateTime.Now.Year, 12, 31).ToString("dd-MMM-yyyy");
            return PartialView("_PaymentHistory", model);
        }

        [HttpPost]
        public async Task<ActionResult> GetPaymentHistory(SearchViewModel model)
        {
            var user = await GetCurrentUserAsync();
            DateTime FromDate = DateTime.ParseExact(model.FromDate, "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
            DateTime ToDate = DateTime.ParseExact(model.ToDate, "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);

            List<InvoiceHistory> invoicehistoryList = _invoiceHistoryService.GetInvoiceHistory(FromDate, ToDate, user.Email);

            return PartialView("_InvoiceHistory", invoicehistoryList);
        }

        [HttpPost]
        public async Task<FileResult> GetInvoicePdf(SearchViewModel model)
        {
            var user = await GetCurrentUserAsync();
            DateTime FromDate = DateTime.ParseExact(model.FromDate, "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
            DateTime ToDate = DateTime.ParseExact(model.ToDate, "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);

            List<InvoiceHistory> invoicehistoryList = _invoiceHistoryService.GetInvoiceHistory(FromDate, ToDate, user.Email);

            MemoryStream workStream = new MemoryStream();
            StringBuilder status = new StringBuilder("");
            DateTime dTime = DateTime.Now;
            //file name to be created   
            string strPDFFileName = string.Format("Invoice_History_" + dTime.ToString("dd-MMM-yyyys") + "-" + ".pdf");
            Document doc = new Document();
            doc.SetPageSize(PageSize.A4);
            doc.SetMargins(0f, 0f, 0f, 0f);
            //Create PDF Table with 5 columns  
            PdfPTable tableLayout = new PdfPTable(7);
            doc.SetMargins(15f, 15f, 10f, 10f);
            //Create PDF Table  

            PdfWriter.GetInstance(doc, workStream).CloseStream = false;
            doc.Open();

            //Add Content to PDF   
            doc.Add(Add_Content_To_PDF(tableLayout, invoicehistoryList));

            // Closing the document  
            doc.Close();

            byte[] byteInfo = workStream.ToArray();
            workStream.Write(byteInfo, 0, byteInfo.Length);
            workStream.Position = 0;

            return File(workStream.ToArray(), "application/pdf", strPDFFileName);
        }

        protected PdfPTable Add_Content_To_PDF(PdfPTable tableLayout, List<InvoiceHistory> invoicehistoryList)
        {

            float[] headers = { 40, 30, 30, 40, 35, 40, 40 }; //Header Widths  
            tableLayout.SetWidths(headers); //Set the pdf headers  
            tableLayout.WidthPercentage = 100; //Set the PDF File witdh percentage  
            tableLayout.HeaderRows = 1;
            //Add Title to the PDF file at the top  

            tableLayout.AddCell(new PdfPCell(new Phrase("Invoice History", new Font(Font.FontFamily.HELVETICA, 14, 1, new iTextSharp.text.BaseColor(0, 0, 0))))
            {
                Colspan = 12,
                Border = 0,
                PaddingBottom = 15,
                HorizontalAlignment = Element.ALIGN_CENTER,
            });


            ////Add header  
            AddCellToHeader(tableLayout, "Date");
            AddCellToHeader(tableLayout, "Currency");
            AddCellToHeader(tableLayout, "Amount");
            AddCellToHeader(tableLayout, "Exchange Rate");
            AddCellToHeader(tableLayout, "USD Amount");
            AddCellToHeader(tableLayout, "Method");
            AddCellToHeader(tableLayout, "Invoice Number");

            ////Add body  
            foreach (InvoiceHistory invoice in invoicehistoryList)
            {
                AddCellToBody(tableLayout, invoice.Date != null ? invoice.Date.ToString("dd-MMM-yyyy") : "", "");
                AddCellToBody(tableLayout, invoice.Currency, "center");
                AddCellToBody(tableLayout, string.Format("{0:C}", invoice.Amount).Replace("$", ""), "right");
                AddCellToBody(tableLayout, string.Format("{0:C}", invoice.ExchangeRate).Replace("$", ""), "right");
                AddCellToBody(tableLayout, string.Format("{0:C}", invoice.USDAmount).Replace("$", ""), "right");
                AddCellToBody(tableLayout, invoice.Method, "");
                AddCellToBody(tableLayout, invoice.InvoiceNumber, "");

            }

            return tableLayout;
        }

        // Method to add single cell to the Header  
        private static void AddCellToHeader(PdfPTable tableLayout, string cellText)
        {
            tableLayout.AddCell(new PdfPCell(new Phrase(cellText, new Font(Font.FontFamily.HELVETICA, 10, 1, iTextSharp.text.BaseColor.BLACK)))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 5,
                BackgroundColor = new iTextSharp.text.BaseColor(System.Drawing.Color.LightGray),
                BorderColor = new iTextSharp.text.BaseColor(System.Drawing.Color.Black)
            });
        }

        // Method to add single cell to the body  
        private static void AddCellToBody(PdfPTable tableLayout, string cellText, string align)
        {
            tableLayout.AddCell(new PdfPCell(new Phrase(cellText, new Font(Font.FontFamily.HELVETICA, 10, 1, iTextSharp.text.BaseColor.BLACK)))
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
                    user.FullName = profile.FullName;
                    user.AddressLine1 = profile.AddressLine1;
                    user.AddressLine2 = profile.AddressLine2;
                    user.State = profile.State;
                    user.Zip = profile.Zip;
                    user.City = profile.City;
                    user.Country = profile.Country;

                    await _userManager.UpdateAsync(user);

                    var client = new RestClient("https://hooks.zapier.com/hooks/catch/2318707/z0jmup/");
                    var request = new RestRequest(Method.POST);
                    request.AddParameter("email", user.Email);
                    request.AddParameter("name", profile.FullName);
                    request.AddParameter("address", profile.AddressLine1 + "<br/>" + profile.AddressLine2);
                    request.AddParameter("city", profile.City);
                    request.AddParameter("state", profile.State);
                    request.AddParameter("zip", profile.Zip);
                    request.AddParameter("country", profile.Country);
                    // execute the request
                    IRestResponse response = client.Execute(request);

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
                            ExpirationYear = card.ExpiryYear,
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
