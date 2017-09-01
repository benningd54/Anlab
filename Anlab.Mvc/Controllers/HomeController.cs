﻿using System;
using Microsoft.AspNetCore.Mvc;

namespace AnlabMvc.Controllers
{
    public class HomeController : ApplicationController
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult SamplingAndPreparation()
        {
            ViewData["Message"] = "Si .";

            return View();
        }

        public IActionResult TestException()
        {
            throw new Exception("Test exception. If this was a real exception, you would need to run in circles, scream and shout.");
        }

        public IActionResult TestNotFound(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }
            return NotFound(id);
        }
    }
}
