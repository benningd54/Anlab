using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AnlabMvc.Controllers
{
    public class ErrorController : Controller
    {
        [HttpGet]
        public IActionResult Index(int id)
        {
            return View(id);
        }

        public IActionResult Error()
        {
            return View();
        }

        public IActionResult LoginError()
        {
            TempData["ErrorMessage"] = "Do not use Google to log in to a UC Davis account";
            return View();
        }
    }
}
