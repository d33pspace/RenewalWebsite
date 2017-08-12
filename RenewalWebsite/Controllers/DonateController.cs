﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace RenewalWebsite.Controllers
{
    public class DonateController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Campaign_2017_08()
        {
            var agent = Request.Headers["User-Agent"];
            Console.WriteLine(agent.ToString());
            ViewBag.Browser = agent.ToString();

            return View();
        }

        public IActionResult WeChat_2017_08()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
