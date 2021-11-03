//===============================================================================
// Microsoft FastTrack for Azure
// Azure Active Directory B2C Authentication Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using AADB2C.WebJQuery.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace AADB2C.WebJQuery.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration _configuration = null;

        /// <summary>
        /// Constructor overload that will allow access to the application configuration settings.
        /// An instance of the IConfiguration interface will be injected by the ASP.Net Core dependency
        /// injection framework.
        /// </summary>
        /// <param name="configuration"><see cref="Microsoft.Extensions.Configuration.IConfiguration"/></param>
        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // Load configuration values into the ViewBag
            ViewBag.ClientId = _configuration.GetValue<string>("AzureADB2C:ClientId");
            ViewBag.Authority = $"{_configuration.GetValue<string>("AzureADB2C:Instance")}/{_configuration.GetValue<string>("AzureADB2C:Domain")}/{_configuration.GetValue<string>("AzureADB2C:SignUpSignInPolicyId")}/v2.0";
            ViewBag.ApiScopes = _configuration.GetValue<string>("AzureADB2C:ApiScopes");
            ViewBag.ApiUrl = _configuration.GetValue<string>("AzureADB2C:ApiUrl");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
