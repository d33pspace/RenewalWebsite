using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RenewalWebsite.Models;
using RenewalWebsite.Services;
using RenewalWebsite.Utility;
using RestSharp;

namespace RenewalWebsite.Controllers
{
    public class ContactController : Controller
    {
        private readonly ILoggerServicecs _loggerService;
        private readonly IUnsubscribeUserService _unsubscribeUserService;
        private EventLog log;

        public ContactController(ILoggerServicecs loggerService,
            IUnsubscribeUserService unsubscribeUserService)
        {
            _loggerService = loggerService;
            _unsubscribeUserService = unsubscribeUserService;
        }

        [Route("/unsubscribe")]
        public IActionResult Index(string email)
        {
            try
            {
                email = Convert.ToString(HttpContext.Request.Query["email"]);
                ViewBag.email = email;
            }
            catch (Exception ex)
            {

            }

            return View();
        }

        [Route("/preference")]
        public IActionResult Preference(string email)
        {
            try
            {
                email = Convert.ToString(HttpContext.Request.Query["email"]);
                ViewBag.email = email;
            }
            catch (Exception ex)
            {

            }

            return View();
        }

        public IActionResult ThankYou()
        {
            int id;
            try
            {
                id = Convert.ToInt32(HttpContext.Request.Query["id"]);
                if (id == 1)
                {
                    ViewBag.Message = "You have been unsubscribed from all Renewal communications.";
                }
                else
                {
                    ViewBag.Message = "Your communication preferences have been updated.";
                }
            }
            catch (Exception ex)
            {

            }

            return View();
        }

        [HttpPost]
        public JsonResult Unsubscribe(UnsubscribeUserViewModel model)
        {
            ResultModel result = new ResultModel();
            if (model != null)
            {
                try
                {
                    UnsubscribeUsers unsubscribeUser = _unsubscribeUserService.GetUnsubscribeUsersByEmail(model.email);
                    if (unsubscribeUser == null)
                    {
                        unsubscribeUser = new UnsubscribeUsers();
                        unsubscribeUser.email = model.email;
                        unsubscribeUser.isUnsubscribe = true;
                        _unsubscribeUserService.Insert(unsubscribeUser);
                    }
                    else
                    {
                        unsubscribeUser.isUnsubscribe = true;
                        _unsubscribeUserService.Update(unsubscribeUser);
                    }

                    var client = new RestClient("https://hooks.zapier.com/hooks/catch/2318707/kbcijy/");
                    var request = new RestRequest(Method.POST);
                    request.AddParameter("email", model.email);
                    // execute the request
                    IRestResponse response = client.Execute(request);

                    result.data = "You have been unsubscribed from all Renewal communications.";
                    result.status = "1";
                    return Json(result);
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
        public JsonResult ChangePreference(UnsubscribeUserViewModel model)
        {
            ResultModel result = new ResultModel();
            if (model != null)
            {
                try
                {
                    UnsubscribeUsers unsubscribeUser = _unsubscribeUserService.GetUnsubscribeUsersByEmail(model.email);
                    if (unsubscribeUser == null)
                    {
                        unsubscribeUser = new UnsubscribeUsers();
                        unsubscribeUser.email = model.email;
                        unsubscribeUser.language = model.language;
                        unsubscribeUser.isUnsubscribe = false;
                        _unsubscribeUserService.Insert(unsubscribeUser);
                    }
                    else
                    {
                        unsubscribeUser.language = model.language;
                        _unsubscribeUserService.Update(unsubscribeUser);
                    }

                    var client = new RestClient("https://hooks.zapier.com/hooks/catch/2318707/kbcwdc/");
                    var request = new RestRequest(Method.POST);
                    request.AddParameter("email", model.email);
                    request.AddParameter("language", model.language);
                    // execute the request
                    IRestResponse response = client.Execute(request);

                    result.data = "Your communication preferences have been updated.";
                    result.status = "1";
                    return Json(result);
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