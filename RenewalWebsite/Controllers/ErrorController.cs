using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RenewalWebsite.Models;

namespace RenewalWebsite.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult Error404(ErrorViewModel model)
        {
            return View("404");
        }

        public IActionResult Error500(ErrorViewModel model)
        {
            return View("500");
        }
    }
}