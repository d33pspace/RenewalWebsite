using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RenewalWebsite.Models;
using RenewalWebsite.Models.AccountViewModels;
using RenewalWebsite.Services;
using Microsoft.AspNetCore.Authentication;
using RenewalWebsite.Utility;
using RestSharp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;

namespace RenewalWebsite.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly IViewRenderService _viewRenderService;
        private readonly ILoggerServicecs _loggerService;
        private EventLog log;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<CurrencySettings> _currencySettings;
        private readonly IStringLocalizer<AccountController> _localizer;
        private readonly ICurrencyService _currencyService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ISmsSender smsSender,
            IViewRenderService viewRenderService,
            ILoggerServicecs loggerService,
            IHttpContextAccessor httpContextAccessor,
            IOptions<CurrencySettings> currencySettings,
            IStringLocalizer<AccountController> localizer,
            ICurrencyService currencyService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _viewRenderService = viewRenderService;
            _loggerService = loggerService;
            _httpContextAccessor = httpContextAccessor;
            _currencySettings = currencySettings;
            _currencyService = currencyService;
            _localizer = localizer;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = returnUrl;
                if (ModelState.IsValid)
                {
                    // This doesn't count login failures towards account lockout
                    // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded) { return RedirectToLocal(returnUrl); }

                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                    }

                    if (result.IsLockedOut) { return View("Lockout"); }
                    else
                    {
                        ModelState.AddModelError(string.Empty, _localizer["Invalid login attempt."]);
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.USER_LOGIN, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = returnUrl;
                if (ModelState.IsValid)
                {
                    var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true, PhoneNumberConfirmed = true };
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        var client = new RestClient("https://hooks.zapier.com/hooks/catch/2318707/z07xtw/");
                        var request = new RestRequest(Method.POST);
                        request.AddParameter("email", model.Email);
                        request.AddParameter("name", string.Empty);
                        request.AddParameter("language_preference", _currencyService.GetCurrentLanguage().TwoLetterISOLanguageName);
                        request.AddParameter("address", string.Empty);
                        request.AddParameter("server_location", _currencySettings.Value.ServerLocation);
                        request.AddParameter("ip_address", _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
                        request.AddParameter("time_zone", model.TimeZone);
                        // execute the request
                        IRestResponse response = client.Execute(request);

                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToLocal(returnUrl);
                    }
                    //AddErrors(result);
                    AddErrorsForRegisterAction(result);

                }
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.USER_REGISTER, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            try
            {
                // Request a redirect to the external login provider.
                var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
                var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return Challenge(properties, provider);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.USER_LOGIN, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        // GET: /Account/ExternalLoginCallback
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = "Error from external provider." };
                _loggerService.SaveEventLogAsync(log);
                ModelState.AddModelError(string.Empty, _localizer["Error from external provider:"] + remoteError);
                return View(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded) { return RedirectToLocal(returnUrl); }

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }

            if (result.IsLockedOut) { return View("Lockout"); }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            }
        }

        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Get the information about the user from the external login provider
                    var info = await _signInManager.GetExternalLoginInfoAsync();
                    if (info == null)
                    {
                        log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = "External login gets failed." };
                        _loggerService.SaveEventLogAsync(log);
                        return View("ExternalLoginFailure");
                    }
                    var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
                    var result = await _userManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        result = await _userManager.AddLoginAsync(user, info);
                        if (result.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            return RedirectToLocal(returnUrl);
                        }
                    }
                    AddErrors(result);
                }

                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.USER_LOGIN, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null) { return View("Error"); }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) { return View("Error"); }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                    {
                        // Don't reveal that the user does not exist or is not confirmed
                        return View("ForgotPasswordConfirmation");
                    }

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    string callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                    ForgotPasswordMailModel mailModel = new ForgotPasswordMailModel();
                    mailModel.Name = user.FullName;
                    mailModel.message = callbackUrl;

                    string template = string.Empty;
                    if (_currencyService.GetCurrentLanguage().TwoLetterISOLanguageName.ToLower().Equals("en"))
                    {
                        template = await _viewRenderService.RenderToStringAsync("Shared/_ForgotPasswordMail", mailModel);
                    }
                    else
                    {
                        template = await _viewRenderService.RenderToStringAsync("Shared/_ForgotPasswordMailInChinese", mailModel);
                    }
                    
                    await _emailSender.SendEmailAsync(model.Email, _localizer["Reset Password"], callbackUrl, user.FullName, template);
                    return View("ForgotPasswordConfirmation");
                }

                // If we got this far, something failed, redisplay form
                return View(model);
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.GET_ITEM, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            return code == null ? View("Error") : View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
                }
                var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
                }
                //AddErrors(result);
                AddErrorsForRegisterAction(result);
                return View();
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.USER_LOGIN, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = ex.Message });
            }
        }

        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            try
            {
                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user == null)
                {
                    return View("Error");
                }
                var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
                var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
                return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.USER_LOGIN, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = "Something went wrong, please try again." });
            }
        }

        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) { return View(); }

                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user == null) { return View("Error"); }

                // Generate the token and send it
                var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
                if (string.IsNullOrWhiteSpace(code)) { return View("Error"); }

                var message = _localizer["Your security code is:"] + code;
                if (model.SelectedProvider == "Email")
                {
                    await _emailSender.SendEmailAsync(await _userManager.GetEmailAsync(user), "Security Code", "", "", message);
                }
                else if (model.SelectedProvider == "Phone")
                {
                    await _smsSender.SendSmsAsync(await _userManager.GetPhoneNumberAsync(user), message);
                }

                return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });

            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.USER_LOGIN, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = "Something went wrong, please try again." });
            }
        }

        // GET: /Account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) { return View("Error"); }

            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) { return View(model); }

                // The following code protects for brute force attacks against the two factor codes.
                // If a user enters incorrect codes for a specified amount of time then the user account
                // will be locked out for a specified amount of time.
                var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
                if (result.Succeeded)
                {
                    return RedirectToLocal(model.ReturnUrl);
                }
                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, _localizer["Invalid code."]);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                log = new EventLog() { EventId = (int)LoggingEvents.USER_LOGIN, LogLevel = LogLevel.Error.ToString(), Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source };
                _loggerService.SaveEventLogAsync(log);
                return RedirectToAction("Error", "Error500", new ErrorViewModel() { Error = "Something went wrong, please try again." });
            }
        }

        // GET /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private void AddErrorsForRegisterAction(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                if (!string.IsNullOrEmpty(error.Description))
                {
                    if (error.Description.Contains("is already taken"))
                    {
                        const string MatchEmailPattern =
          @"(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
          + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
            + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
          + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})";
                        Regex rx = new Regex(MatchEmailPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        // Find matches.
                        MatchCollection matches = rx.Matches(error.Description);
                        // Report the number of matches found.
                        int noOfMatches = matches.Count;
                        // Report on each match.
                        string emailId = string.Empty;
                        foreach (Match match in matches)
                        {
                            emailId = match.Value.ToString();
                        }

                        if (_currencyService.GetCurrentLanguage().TwoLetterISOLanguageName.ToLower().Equals("en"))
                        {
                            string fullstring = _localizer["User name"] + " " + "'" + emailId + "'" + " " + _localizer["is already taken."];
                            error.Description = fullstring;
                        }
                        else
                        {
                            string fullstring = _localizer["this username has been registered."];
                            error.Description = fullstring;
                        }
                    }

                    if(error.Description.Contains("Invalid token"))
                    {
                        error.Description = _localizer["Invalid token."];
                    }
                }

                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion

        public IActionResult DeleteSubcription(int subscriptionId)
        {
            return View("Index");
        }

        public IActionResult AddSubscription()
        {
            return View();
        }
    }
}
