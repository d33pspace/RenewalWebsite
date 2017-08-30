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
        public IActionResult Error(ErrorViewModel model)
        {
            return View(model);
        }
    }
}