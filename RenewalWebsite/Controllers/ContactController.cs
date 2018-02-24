using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RenewalWebsite.Models;
using RenewalWebsite.Services;
using RenewalWebsite.Utility;

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

        [Route("/contact")]
        public IActionResult Index()
        {
            string email;
            string lang;
            try
            {
                email = Convert.ToString(HttpContext.Request.Query["email"]);
                lang = Convert.ToString(HttpContext.Request.Query["lang"]);
                ViewBag.email = email;
                ViewBag.lang = lang;
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
                    if(unsubscribeUser == null)
                    {
                        unsubscribeUser = new UnsubscribeUsers();
                        unsubscribeUser.email = model.email;
                        unsubscribeUser.language = model.language;
                        unsubscribeUser.isUnsubscribe = true;
                        _unsubscribeUserService.Insert(unsubscribeUser);
                    }
                    else
                    {
                        unsubscribeUser.isUnsubscribe = true;
                        _unsubscribeUserService.Update(unsubscribeUser);
                    }

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
                        if (model.language == "en")
                        {
                            unsubscribeUser.language = "zh";
                        }
                        else
                        {
                            unsubscribeUser.language = "en";
                        }
                        unsubscribeUser.language = model.language;
                        unsubscribeUser.isUnsubscribe = false;
                        _unsubscribeUserService.Insert(unsubscribeUser);
                    }
                    else
                    {
                        if (unsubscribeUser.language == "en")
                        {
                            unsubscribeUser.language = "zh";
                        }
                        else
                        {
                            unsubscribeUser.language = "en";
                        }
                        _unsubscribeUserService.Update(unsubscribeUser);
                    }

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

        [HttpPost]
        public JsonResult SendFeedBack(UnsubscribeUserViewModel model)
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
                        unsubscribeUser.feedback = model.feedback;
                        _unsubscribeUserService.Insert(unsubscribeUser);
                    }
                    else
                    {
                        unsubscribeUser.feedback = model.feedback;
                        _unsubscribeUserService.Update(unsubscribeUser);
                    }

                    result.data = "Your special instructions or feebback has been submitted.";
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