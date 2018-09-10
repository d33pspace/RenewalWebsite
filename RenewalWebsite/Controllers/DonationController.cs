using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RenewalWebsite.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using RenewalWebsite.Models;
using Stripe;
using RenewalWebsite.Helpers;
using Microsoft.Extensions.Logging;
using RenewalWebsite.Utility;
using RestSharp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using RenewalWebsite.Data;
using RenewalWebsite.SettingModels;

namespace RenewalWebsite.Controllers
{
    [Authorize]
    public class DonationController : Controller
    {
        private const string DonationCaption = "Renewal Center donation";
        private readonly IDonationService _donationService;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICampaignService _campaignService;
        private readonly ILoggerServicecs _loggerService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<CurrencySettings> _currencySettings;
        private readonly ICurrencyService _currencyService;
        private readonly ICountryService _countryService;
        private EventLog log;
        private readonly IStringLocalizer<DonationController> _localizer;

        public DonationController(
            UserManager<ApplicationUser> userManager,
            IDonationService donationService,
            IOptions<StripeSettings> stripeSettings,
            ICampaignService campaignService,
            ILoggerServicecs loggerServicer,
            IHttpContextAccessor httpContextAccessor,
            IOptions<CurrencySettings> currencySettings,
            ICurrencyService currencyService,
            IStringLocalizer<DonationController> localizer,
            ICountryService countryService,
            CountrySeeder countrySeeder)
        {
            _userManager = userManager;
            _donationService = donationService;
            _stripeSettings = stripeSettings;
            _campaignService = campaignService;
            _loggerService = loggerServicer;
            _httpContextAccessor = httpContextAccessor;
            _currencySettings = currencySettings;
            _currencyService = currencyService;
            _countryService = countryService;
            _localizer = localizer;
            countrySeeder.Seed();
        }

        [Route("Donation/Payment")]
        [HttpGet]
        public ActionResult payment()
        {
            return RedirectToAction("index", "home");
        }

        [Route("Donation/Payment/{id}")]
        public async Task<IActionResult> Payment(int id)
        {
            var user = await GetCurrentUserAsync();
            try
            {
                var donation = _donationService.GetById(id);
                var detail = (DonationViewModel)donation;
                List<CountryViewModel> countryList = GetCountryList();

                // Check for existing customer
                // edit = 1 means user wants to edit the credit card information
                if (!string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    try
                    {
                        var CustomerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                        StripeCustomer objStripeCustomer = CustomerService.Get(user.StripeCustomerId);
                        StripeCard objStripeCard = null;

                        if (objStripeCustomer.Sources != null && objStripeCustomer.Sources.TotalCount > 0 && objStripeCustomer.Sources.Data.Any())
                        {
                            objStripeCard = objStripeCustomer.Sources.Data.FirstOrDefault().Card;
                        }

                        if (objStripeCard != null && !string.IsNullOrEmpty(objStripeCard.Id))
                        {
                            CustomerRePaymentViewModel customerRePaymentViewModel = CustomerRepaymentModelData(user, donation, detail, countryList, objStripeCustomer, objStripeCard);
                            return View("RePayment", customerRePaymentViewModel);
                        }
                    }
                    catch (StripeException ex)
                    {
                        log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                        _loggerService.SaveEventLogAsync(log);
                        ModelState.AddModelError("CustomerNotFound", ex.Message);
                    }
                }

                CustomerPaymentViewModel customerPaymentViewModel = GetCustomerPaymentModel(user, donation, detail, countryList);
                return View("Payment", customerPaymentViewModel);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Payment(CustomerPaymentViewModel payment)
        {
            var user = await GetCurrentUserAsync();
            try
            {
                List<CountryViewModel> countryList = GetCountryList();
                payment.countries = countryList;
                payment.yearList = GeneralUtility.GetYeatList();

                if (!ModelState.IsValid) { return View(payment); }

                var customerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                var donation = _donationService.GetById(payment.DonationId);

                // Construct payment
                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    StripeCustomerCreateOptions customer = GetCustomerCreateOptions(payment, user);
                    var stripeCustomer = customerService.Create(customer);
                    user.StripeCustomerId = stripeCustomer.Id;
                }
                else
                {
                    //Check for existing credit card, if new credit card number is same as exiting credit card then we delete the existing
                    //Credit card information so new card gets generated automatically as default card.
                    //try
                    //{
                    //    var ExistingCustomer = customerService.Get(user.StripeCustomerId);
                    //    if (ExistingCustomer.Sources != null && ExistingCustomer.Sources.TotalCount > 0 && ExistingCustomer.Sources.Data.Any())
                    //    {
                    //        var cardService = new StripeCardService(_stripeSettings.Value.SecretKey);
                    //        foreach (var cardSource in ExistingCustomer.Sources.Data)
                    //        {
                    //            cardService.Delete(user.StripeCustomerId, cardSource.Card.Id);
                    //        }
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    log = new EventLog() { EventId = (int)LoggingEvents.GET_CUSTOMER, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                    //    _loggerService.SaveEventLogAsync(log);
                    //    return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
                    //}

                    StripeCustomerUpdateOptions customer = GetCustomerUpdateOption(payment);
                    try
                    {
                        var stripeCustomer = customerService.Update(user.StripeCustomerId, customer);
                        user.StripeCustomerId = stripeCustomer.Id;
                    }
                    catch (StripeException ex)
                    {
                        log = new EventLog() { EventId = (int)LoggingEvents.GET_CUSTOMER, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                        _loggerService.SaveEventLogAsync(log);
                        if (ex.Message.ToLower().Contains("customer"))
                        {
                            return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
                        }
                        else
                        {
                            ModelState.AddModelError("error", _localizer[ex.Message]);
                            return View(payment);
                        }
                    }
                }

                UpdateUserEmail(payment, user);
                await UpdateUserDetail(payment, user);

                // Add customer to Stripe
                if (EnumInfo<PaymentCycle>.GetValue(donation.CycleId) == PaymentCycle.OneTime)
                {
                    var model = (DonationViewModel)donation;
                    var charges = new StripeChargeService(_stripeSettings.Value.SecretKey);

                    // Charge the customer
                    var charge = charges.Create(new StripeChargeCreateOptions
                    {
                        Amount = Convert.ToInt32(donation.DonationAmount * 100),
                        Description = DonationCaption,
                        Currency = "usd",//payment.Currency.ToLower(),
                        CustomerId = user.StripeCustomerId,
                        StatementDescriptor = _stripeSettings.Value.StatementDescriptor,
                    });

                    if (charge.Paid)
                    {
                        var completedMessage = new CompletedViewModel
                        {
                            Message = donation.DonationAmount.ToString(),
                            HasSubscriptions = false
                        };
                        return RedirectToAction("Thanks", completedMessage);
                    }
                    return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
                }

                // Add to existing subscriptions and charge 
                donation.Currency = "usd"; //payment.Currency;
                var plan = _donationService.GetOrCreatePlan(donation);

                var subscriptionService = new StripeSubscriptionService(_stripeSettings.Value.SecretKey);
                var result = subscriptionService.Create(user.StripeCustomerId, plan.Id);
                if (result != null)
                {
                    CompletedViewModel completedMessage = new CompletedViewModel();
                    completedMessage = GetSubscriptionMessage(result, true);
                    return RedirectToAction("Thanks", completedMessage);
                }
            }
            catch (StripeException ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.INSERT_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                if (ex.Message.ToLower().Contains("customer"))
                {
                    return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
                }
                else
                {
                    ModelState.AddModelError("error", _localizer[ex.Message]);
                    return View(payment);
                }
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.INSERT_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = ex.Message });
            }

            return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
        }

        [Route("Donation/Payment/{id}/{edit?}")]
        [HttpGet]
        public async Task<IActionResult> Payment(int id, int edit)
        {
            var user = await GetCurrentUserAsync();
            try
            {
                var donation = _donationService.GetById(id);
                var detail = (DonationViewModel)donation;
                CustomerPaymentViewModel model = new CustomerPaymentViewModel();

                try
                {
                    var customerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                    var ExistingCustomer = customerService.Get(user.StripeCustomerId);
                    List<CountryViewModel> countryList = GetCountryList();
                    model = GetCustomerPaymentModel(user, donation, detail, countryList);
                    model.DisableCurrencySelection = "1"; // Disable currency selection for already created customer as stripe only allow same currency for one customer,
                }
                catch (StripeException ex)
                {
                    log = new EventLog() { EventId = (int)LoggingEvents.GET_CUSTOMER, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                    _loggerService.SaveEventLogAsync(log);
                    ModelState.AddModelError("CustomerNotFound", ex.Message);
                }

                return View("Payment", model);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GET_CUSTOMER, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RePayment(CustomerRePaymentViewModel payment)
        {
            var user = await GetCurrentUserAsync();
            try
            {
                if (!ModelState.IsValid)
                {
                    List<CountryViewModel> countryList = GetCountryList();
                    payment.countries = countryList;
                    return View(payment);
                }

                var customerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                var donation = _donationService.GetById(payment.DonationId);
                UpdateUserRepaymentEmail(payment, user);
                await UpdateRepaymentUserDetail(payment, user);

                // Add customer to Stripe
                if (EnumInfo<PaymentCycle>.GetValue(donation.CycleId) == PaymentCycle.OneTime)
                {
                    var model = (DonationViewModel)donation;
                    var charges = new StripeChargeService(_stripeSettings.Value.SecretKey);

                    // Charge the customer
                    var charge = charges.Create(new StripeChargeCreateOptions
                    {
                        Amount = Convert.ToInt32(donation.DonationAmount * 100),
                        Description = DonationCaption,
                        Currency = "usd", //payment.Currency.ToLower(),
                        CustomerId = user.StripeCustomerId,
                        StatementDescriptor = _stripeSettings.Value.StatementDescriptor,
                    });

                    if (charge.Paid)
                    {
                        var completedMessage = new CompletedViewModel
                        {
                            Message = donation.DonationAmount.ToString(),
                            HasSubscriptions = false
                        };
                        return RedirectToAction("Thanks", completedMessage);
                    }
                    return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
                }
                donation.Currency = "usd";//payment.Currency;
                // Add to existing subscriptions and charge 
                var plan = _donationService.GetOrCreatePlan(donation);

                var subscriptionService = new StripeSubscriptionService(_stripeSettings.Value.SecretKey);
                var result = subscriptionService.Create(user.StripeCustomerId, plan.Id);
                if (result != null)
                {
                    CompletedViewModel completedMessage = new CompletedViewModel();
                    completedMessage = GetSubscriptionMessage(result, true);
                    return RedirectToAction("Thanks", completedMessage);
                }
            }
            catch (StripeException ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.INSERT_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                if (ex.Message.ToLower().Contains("customer"))
                {
                    return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
                }
                else
                {
                    ModelState.AddModelError("error", ex.Message);
                    return View(payment);
                }
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.INSERT_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = ex.Message });
            }
            return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
        }

        public ActionResult Thanks(CompletedViewModel model)
        {
            if (model.Message1 != null)
            {
                model.Message1 = _localizer[model.Message1];
            }
            return View(model);
        }

        [Route("Donation/Payment/campaign")]
        [HttpGet]
        public ActionResult CampaignPayment()
        {
            return RedirectToAction("index", "home");
        }

        [Route("Donation/Payment/campaign/{id}")]
        public async Task<IActionResult> CampaignPayment(int id)
        {
            var user = await GetCurrentUserAsync();
            try
            {
                var donation = _campaignService.GetById(id);
                var detail = (DonationViewModel)donation;
                List<CountryViewModel> countryList = GetCountryList();

                // Check for existing customer
                // edit = 1 means user wants to edit the credit card information
                if (!string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    try
                    {
                        var CustomerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                        StripeCustomer objStripeCustomer = CustomerService.Get(user.StripeCustomerId);
                        StripeCard objStripeCard = null;

                        if (objStripeCustomer.Sources != null && objStripeCustomer.Sources.TotalCount > 0 && objStripeCustomer.Sources.Data.Any())
                        {
                            objStripeCard = objStripeCustomer.Sources.Data.FirstOrDefault().Card;
                        }

                        if (objStripeCard != null && !string.IsNullOrEmpty(objStripeCard.Id))
                        {
                            CustomerRePaymentViewModel customerRePaymentViewModel = CustomerRepaymentModelData(user, donation, detail, countryList, objStripeCustomer, objStripeCard);
                            return View("CampaignRePayment", customerRePaymentViewModel);
                        }
                    }
                    catch (StripeException sex)
                    {
                        log = new EventLog() { EventId = (int)LoggingEvents.GET_CUSTOMER, LogLevel = LogLevel.Error.ToString(), Message = sex.Message, StackTrace = sex.StackTrace, Source = sex.Source, EmailId = user.Email };
                        _loggerService.SaveEventLogAsync(log);
                        ModelState.AddModelError("CustomerNotFound", sex.Message);
                    }
                }

                CustomerPaymentViewModel customerPaymentViewModel = GetCustomerPaymentModel(user, donation, detail, countryList);
                return View("CampaignPayment", customerPaymentViewModel);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CampaignPayment(CustomerPaymentViewModel payment)
        {
            var user = await GetCurrentUserAsync();
            try
            {
                List<CountryViewModel> countryList = GetCountryList();
                payment.countries = countryList;
                payment.yearList = GeneralUtility.GetYeatList();
                if (!ModelState.IsValid)
                {
                    return View(payment);
                }

                var customerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                var donation = _campaignService.GetById(payment.DonationId);

                // Construct payment
                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    StripeCustomerCreateOptions customer = GetCustomerCreateOptions(payment, user);
                    var stripeCustomer = customerService.Create(customer);
                    user.StripeCustomerId = stripeCustomer.Id;
                }
                else
                {
                    //Check for existing credit card, if new credit card number is same as exiting credit card then we delete the existing
                    //Credit card information so new card gets generated automatically as default card.
                    //try
                    //{
                    //    var ExistingCustomer = customerService.Get(user.StripeCustomerId);
                    //    if (ExistingCustomer.Sources != null && ExistingCustomer.Sources.TotalCount > 0 && ExistingCustomer.Sources.Data.Any())
                    //    {
                    //        var cardService = new StripeCardService(_stripeSettings.Value.SecretKey);
                    //        foreach (var cardSource in ExistingCustomer.Sources.Data)
                    //        {
                    //            cardService.Delete(user.StripeCustomerId, cardSource.Card.Id);
                    //        }
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    log = new EventLog() { EventId = (int)LoggingEvents.INSERT_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                    //    _loggerService.SaveEventLogAsync(log);
                    //    return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
                    //}

                    StripeCustomerUpdateOptions customer = GetCustomerUpdateOption(payment);
                    try
                    {
                        var stripeCustomer = customerService.Update(user.StripeCustomerId, customer);
                        user.StripeCustomerId = stripeCustomer.Id;
                    }
                    catch (StripeException ex)
                    {
                        log = new EventLog() { EventId = (int)LoggingEvents.GET_CUSTOMER, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                        _loggerService.SaveEventLogAsync(log);
                        if (ex.Message.ToLower().Contains("customer"))
                        {
                            return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
                        }
                        else
                        {
                            ModelState.AddModelError("error", _localizer[ex.Message]);
                            return View(payment);
                        }
                    }
                }

                UpdateUserEmail(payment, user);
                await UpdateUserDetail(payment, user);

                // Add customer to Stripe
                if (EnumInfo<PaymentCycle>.GetValue(donation.CycleId) == PaymentCycle.OneTime)
                {
                    var model = (DonationViewModel)donation;
                    var charges = new StripeChargeService(_stripeSettings.Value.SecretKey);

                    // Charge the customer
                    var charge = charges.Create(new StripeChargeCreateOptions
                    {
                        Amount = Convert.ToInt32(donation.DonationAmount * 100),
                        Description = DonationCaption,
                        Currency = "usd",//payment.Currency.ToLower(),
                        CustomerId = user.StripeCustomerId,
                        StatementDescriptor = _stripeSettings.Value.StatementDescriptor,
                    });

                    if (charge.Paid)
                    {
                        var completedMessage = new CompletedViewModel
                        {
                            Message = donation.DonationAmount.ToString(),
                            HasSubscriptions = false
                        };
                        return RedirectToAction("Thanks", completedMessage);
                    }
                    return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
                }

                // Add to existing subscriptions and charge 
                donation.Currency = "usd"; //payment.Currency;
                var plan = _campaignService.GetOrCreatePlan(donation);

                var subscriptionService = new StripeSubscriptionService(_stripeSettings.Value.SecretKey);
                var result = subscriptionService.Create(user.StripeCustomerId, plan.Id);
                if (result != null)
                {
                    CompletedViewModel completedMessage = new CompletedViewModel();
                    completedMessage = GetSubscriptionMessage(result, true);
                    return RedirectToAction("Thanks", completedMessage);
                }
            }
            catch (StripeException ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.INSERT_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                if (ex.Message.ToLower().Contains("customer"))
                {
                    return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
                }
                else
                {
                    ModelState.AddModelError("error", ex.Message);
                    return View(payment);
                }
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.INSERT_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = ex.Message });
            }
            return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
        }

        [Route("Donation/Payment/campaign/{id}/{edit?}")]
        public async Task<IActionResult> CampaignPayment(int id, int edit)
        {
            var user = await GetCurrentUserAsync();
            try
            {
                var donation = _campaignService.GetById(id);
                var detail = (DonationViewModel)donation;
                CustomerPaymentViewModel model = new CustomerPaymentViewModel();
                try
                {
                    List<CountryViewModel> countryList = GetCountryList();
                    var customerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                    var ExistingCustomer = customerService.Get(user.StripeCustomerId);
                    model = GetCustomerPaymentModel(user, donation, detail, countryList);
                    model.DisableCurrencySelection = "1"; // Disable currency selection for already created customer as stripe only allow same currency for one customer,
                }
                catch (StripeException ex)
                {
                    log = new EventLog() { EventId = (int)LoggingEvents.GET_CUSTOMER, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                    _loggerService.SaveEventLogAsync(log);
                    ModelState.AddModelError("CustomerNotFound", ex.Message);
                }

                return View("CampaignPayment", model);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.INSERT_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CampaignRePayment(CustomerRePaymentViewModel payment)
        {
            var user = await GetCurrentUserAsync();
            try
            {
                List<CountryViewModel> countryList = GetCountryList();
                payment.countries = countryList;
                if (!ModelState.IsValid)
                {
                    return View(payment);
                }

                var customerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                var donation = _campaignService.GetById(payment.DonationId);

                UpdateUserRepaymentEmail(payment, user);
                await UpdateRepaymentUserDetail(payment, user);

                // Add customer to Stripe
                if (EnumInfo<PaymentCycle>.GetValue(donation.CycleId) == PaymentCycle.OneTime)
                {
                    var model = (DonationViewModel)donation;
                    var charges = new StripeChargeService(_stripeSettings.Value.SecretKey);

                    // Charge the customer
                    var charge = charges.Create(new StripeChargeCreateOptions
                    {
                        Amount = Convert.ToInt32(donation.DonationAmount * 100),
                        Description = DonationCaption,
                        Currency = "usd", //payment.Currency.ToLower(),
                        CustomerId = user.StripeCustomerId,
                        StatementDescriptor = _stripeSettings.Value.StatementDescriptor,
                    });

                    if (charge.Paid)
                    {
                        var completedMessage = new CompletedViewModel
                        {
                            Message = donation.DonationAmount.ToString(),
                            HasSubscriptions = false
                        };
                        return RedirectToAction("Thanks", completedMessage);
                    }
                    return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
                }
                donation.Currency = "usd";//payment.Currency;
                // Add to existing subscriptions and charge 
                var plan = _campaignService.GetOrCreatePlan(donation);

                var subscriptionService = new StripeSubscriptionService(_stripeSettings.Value.SecretKey);
                var result = subscriptionService.Create(user.StripeCustomerId, plan.Id);
                if (result != null)
                {
                    CompletedViewModel completedMessage = new CompletedViewModel();
                    completedMessage = GetSubscriptionMessage(result, true);
                    return RedirectToAction("Thanks", completedMessage);
                }
            }
            catch (StripeException ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.INSERT_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                if (ex.Message.ToLower().Contains("customer"))
                {
                    return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
                }
                else
                {
                    ModelState.AddModelError("error", ex.Message);
                    return View(payment);
                }
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.INSERT_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source, EmailId = user.Email };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = ex.Message });
            }
            return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
        }

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        private List<CountryViewModel> GetCountryList()
        {
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

            return countryList;
        }

        private void UpdateUserEmail(CustomerPaymentViewModel payment, ApplicationUser user)
        {
            if (user.FullName != payment.Name ||
                               user.AddressLine1 != payment.AddressLine1 ||
                               user.AddressLine2 != payment.AddressLine2 ||
                               user.City != payment.City ||
                               user.State != payment.State ||
                               user.Country != payment.Country ||
                               user.Zip != payment.Zip)
            {
                var client = new RestClient("https://hooks.zapier.com/hooks/catch/2318707/z0jmup/");
                var request = new RestRequest(Method.POST);
                request.AddParameter("email", user.Email);
                request.AddParameter("contact_name", payment.Name);
                request.AddParameter("language_preference", _currencyService.GetCurrentLanguage().TwoLetterISOLanguageName);
                request.AddParameter("salutation", payment.Name.Split(' ').Length == 1 ? payment.Name : payment.Name.Split(' ')[0]);
                request.AddParameter("last_name", payment.Name.Split(' ').Length == 1 ? "" : payment.Name.Split(' ')[(payment.Name.Split(' ').Length - 1)]);
                request.AddParameter("address_line_1", payment.AddressLine1);
                request.AddParameter("address_line_2", payment.AddressLine2);
                request.AddParameter("server_location", _currencySettings.Value.ServerLocation);
                request.AddParameter("ip_address", _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                request.AddParameter("time_zone", payment.TimeZone);
                request.AddParameter("city", payment.City);
                request.AddParameter("state", payment.State);
                request.AddParameter("zip", payment.Zip);
                request.AddParameter("country", payment.Country);
                // execute the request
                IRestResponse response = client.Execute(request);
            }
        }

        private void UpdateUserRepaymentEmail(CustomerRePaymentViewModel payment, ApplicationUser user)
        {
            if (user.FullName != payment.Name ||
               user.AddressLine1 != payment.AddressLine1 ||
               user.AddressLine2 != payment.AddressLine2 ||
               user.City != payment.City ||
               user.State != payment.State ||
               user.Country != payment.Country ||
               user.Zip != payment.Zip)
            {
                var client = new RestClient("https://hooks.zapier.com/hooks/catch/2318707/z0jmup/");
                var request = new RestRequest(Method.POST);
                request.AddParameter("email", user.Email);
                request.AddParameter("contact_name", payment.Name);
                request.AddParameter("language_preference", _currencyService.GetCurrentLanguage().TwoLetterISOLanguageName);
                request.AddParameter("salutation", payment.Name.Split(' ').Length == 1 ? payment.Name : payment.Name.Split(' ')[0]);
                request.AddParameter("last_name", payment.Name.Split(' ').Length == 1 ? "" : payment.Name.Split(' ')[(payment.Name.Split(' ').Length - 1)]);
                request.AddParameter("address_line_1", payment.AddressLine1);
                request.AddParameter("address_line_2", payment.AddressLine2);
                request.AddParameter("server_location", _currencySettings.Value.ServerLocation);
                request.AddParameter("ip_address", _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                request.AddParameter("time_zone", payment.TimeZone);
                request.AddParameter("city", payment.City);
                request.AddParameter("state", payment.State);
                request.AddParameter("zip", payment.Zip);
                request.AddParameter("country", payment.Country);
                // execute the request
                IRestResponse response = client.Execute(request);
            }
        }

        private CustomerPaymentViewModel GetCustomerPaymentModel(ApplicationUser user, Donation donation, DonationViewModel detail, List<CountryViewModel> countryList)
        {
            return new CustomerPaymentViewModel
            {
                Name = user.FullName,
                AddressLine1 = user.AddressLine1,
                AddressLine2 = user.AddressLine2,
                City = user.City,
                State = user.State,
                Country = string.IsNullOrEmpty(user.Country) ? _currencySettings.Value.ServerLocation == "China" ? "CN" : "" : user.Country,
                Zip = user.Zip,
                DonationId = donation.Id,
                Description = donation.Reason,
                Frequency = _localizer[detail.GetCycle(donation.CycleId.ToString())],
                Amount = (decimal)donation.DonationAmount,
                IsCustom = donation.IsCustom,
                countries = countryList,
                yearList = GeneralUtility.GetYeatList()
            };
        }

        private CustomerRePaymentViewModel CustomerRepaymentModelData(ApplicationUser user, Donation donation, DonationViewModel detail, List<CountryViewModel> countryList, StripeCustomer objStripeCustomer, StripeCard objStripeCard)
        {
            return new CustomerRePaymentViewModel()
            {
                Name = user.FullName,
                AddressLine1 = user.AddressLine1,
                AddressLine2 = user.AddressLine2,
                City = user.City,
                State = user.State,
                Country = string.IsNullOrEmpty(user.Country) ? _currencySettings.Value.ServerLocation == "China" ? "CN" : "" : user.Country,
                Zip = user.Zip,
                DonationId = donation.Id,
                Description = donation.Reason,
                Frequency = _localizer[detail.GetCycle(donation.CycleId.ToString())],
                Amount = (decimal)donation.DonationAmount,
                IsCustom = donation.IsCustom,
                countries = countryList,
                Last4Digit = objStripeCard.Last4,
                CardId = objStripeCard.Id,
                DisableCurrencySelection = string.IsNullOrEmpty(objStripeCustomer.Currency) ? "0" : "1"
            };
        }

        private static StripeCustomerCreateOptions GetCustomerCreateOptions(CustomerPaymentViewModel payment, ApplicationUser user)
        {
            return new StripeCustomerCreateOptions
            {
                Email = user.Email,
                Description = $"{user.Email} {user.Id}",
                SourceCard = new SourceCard
                {
                    Name = payment.Name,
                    Number = payment.CardNumber,
                    Cvc = payment.Cvc,
                    ExpirationMonth = payment.ExpiryMonth,
                    ExpirationYear = payment.ExpiryYear,
                    AddressLine1 = payment.AddressLine1,
                    AddressLine2 = payment.AddressLine2,
                    AddressCity = payment.City,
                    AddressState = payment.State,
                    AddressCountry = payment.Country,
                    AddressZip = payment.Zip
                }
            };
        }

        private static StripeCustomerUpdateOptions GetCustomerUpdateOption(CustomerPaymentViewModel payment)
        {
            return new StripeCustomerUpdateOptions
            {
                SourceCard = new SourceCard
                {
                    Name = payment.Name,
                    Number = payment.CardNumber,
                    Cvc = payment.Cvc,
                    ExpirationMonth = payment.ExpiryMonth,
                    ExpirationYear = payment.ExpiryYear,
                    AddressLine1 = payment.AddressLine1,
                    AddressLine2 = payment.AddressLine2,
                    AddressCity = payment.City,
                    AddressState = payment.State,
                    AddressCountry = payment.Country,
                    AddressZip = payment.Zip
                }
            };
        }

        private async Task UpdateUserDetail(CustomerPaymentViewModel payment, ApplicationUser user)
        {
            user.FullName = payment.Name;
            user.AddressLine1 = payment.AddressLine1;
            user.AddressLine2 = payment.AddressLine2;
            user.City = payment.City;
            user.State = payment.State;
            user.Country = payment.Country;
            user.Zip = payment.Zip;
            await _userManager.UpdateAsync(user);
        }

        private async Task UpdateRepaymentUserDetail(CustomerRePaymentViewModel payment, ApplicationUser user)
        {
            user.FullName = payment.Name;
            user.AddressLine1 = payment.AddressLine1;
            user.AddressLine2 = payment.AddressLine2;
            user.City = payment.City;
            user.State = payment.State;
            user.Country = payment.Country;
            user.Zip = payment.Zip;
            await _userManager.UpdateAsync(user);

        }

        private CompletedViewModel GetSubscriptionMessage(StripeSubscription result, bool HasSubscriptions)
        {
            var completedMessage = new CompletedViewModel
            {
                Message = result.StripePlan.Nickname.Split("_")[1],
                Message1 = result.StripePlan.Nickname.Split("_")[0],
                HasSubscriptions = HasSubscriptions
            };
            return completedMessage;
        }
    }
}