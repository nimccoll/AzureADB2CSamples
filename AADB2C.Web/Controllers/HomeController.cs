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
using AADB2C.Web.Models;
using AADB2C.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AADB2C.Web.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration _configuration = null;
        private static readonly HttpClient _client = new HttpClient();

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
            List<Claim> claims = new List<Claim>();
            if (User.Identity.IsAuthenticated)
            {
                foreach (Claim claim in User.Claims)
                {
                    claims.Add(claim);
                    if (claim.Type == "extension_jdrfRoutingId") ViewBag.RoutingId = claim.Value;
                    if (claim.Type == "extension_jdrfNonce") ViewBag.Nonce = claim.Value;
                }
                ViewBag.Claims = claims;
            }

            return View();
        }

        /// <summary>
        /// Calls a simple WebAPI that returns an array of string values. This method requires an authenticated
        /// user.
        /// </summary>
        /// <returns><see cref="Microsoft.AspNetCore.Mvc.IActionResult"/> A view that displays the values returned by the API.</returns>
        [Authorize]
        public IActionResult CallAPI()
        {
            // Retrieve an access token for the WebAPI
            string accessToken = GetAPIAccessToken().Result;
            if (string.IsNullOrEmpty(accessToken))
            {
                // If an empty access token is returned, the user must sign in again because their session has
                // expired. Redirect back to this method to force another sign in.
                return RedirectToAction("CallAPI");
            }
            else
            {
                // Add a "Bearer" authentication header value that includes the access token for the API
                // to the Authorization header of the HTTP request to the WebAPI.
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Call the WebAPI
                HttpResponseMessage apiResult = _client.GetAsync(_configuration.GetValue<string>("AzureADB2C:ApiUrl")).Result;

                if (apiResult.IsSuccessStatusCode)
                {
                    // Convert the response to an array of strings and add it to the ViewBag for display
                    string[] values = JsonConvert.DeserializeObject<string[]>(apiResult.Content.ReadAsStringAsync().Result);
                    ViewBag.Values = values;
                }
            }

            return View();
        }

        /// <summary>
        /// Calls a simple WebAPI that returns an array of string values. This method requires an authenticated
        /// user.
        /// </summary>
        /// <returns><see cref="Microsoft.AspNetCore.Mvc.IActionResult"/> A view that displays the values returned by the API.</returns>
        [Authorize]
        public IActionResult CallFunction()
        {
            // Retrieve an access token for the WebAPI
            string accessToken = GetAPIAccessToken().Result;
            if (string.IsNullOrEmpty(accessToken))
            {
                // If an empty access token is returned, the user must sign in again because their session has
                // expired. Redirect back to this method to force another sign in.
                return RedirectToAction("CallFunction");
            }
            else
            {
                // Add a "Bearer" authentication header value that includes the access token for the API
                // to the Authorization header of the HTTP request to the WebAPI.
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Call the WebAPI
                HttpResponseMessage apiResult = _client.GetAsync(_configuration.GetValue<string>("AzureADB2C:FunctionUrl")).Result;

                if (apiResult.IsSuccessStatusCode)
                {
                    // Convert the response to an array of strings and add it to the ViewBag for display
                    string[] values = JsonConvert.DeserializeObject<string[]>(apiResult.Content.ReadAsStringAsync().Result);
                    ViewBag.Values = values;
                }
            }

            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Retrieves the cached access token for the WebAPI. If an access token cannot be found in the 
        /// cache, return an empty access token.
        /// </summary>
        /// <returns>WebAPI access token. Empty if a cached access token cannot be found.</returns>
        private async Task<string> GetAPIAccessToken()
        {
            string accessToken = string.Empty;

            // The cache is built using the signed in user's identity so we must retrieve their name identifier
            // from the claims collection
            string signedInUserID = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Build the identifier for the token issuing authority. Values are retrieved from configuration.
            // Ex. https://login.microsoftonline.com/tfp/{your B2C tenant}.onmicrosoft.com/{your B2C sign-up signin-in policy name}/v2.0
            //string authority = $"{_configuration.GetValue<string>("AzureADB2C:Instance")}/{_configuration.GetValue<string>("AzureADB2C:Domain")}/{_configuration.GetValue<string>("AzureADB2C:SignUpSignInPolicyId")}/v2.0";
            string authority = $"{_configuration.GetValue<string>("AzureADB2C:Instance")}tfp/{_configuration.GetValue<string>("AzureADB2C:Domain")}/{_configuration.GetValue<string>("AzureADB2C:SignUpSignInPolicyId")}";

            // Build the redirect Uri
            // Ex. https://localhost:44340/signin-oidc
            string redirectUri = UriHelper.BuildAbsolute(Request.Scheme, Request.Host, _configuration.GetValue<string>("AzureADB2C:CallbackPath"));

            // Reconstruct the token cache based on the signed in User ID and the current HttpContext
            TokenCache userTokenCache = new MSALSessionCache(signedInUserID, this.HttpContext).GetMsalCacheInstance();

            // Create an instance of the ConfidentialClientApplication to retrieve the access token from the cache using
            // the authority, redirectUri, the token cache and the client ID and client secret of the web
            // application (from configuration)
            ConfidentialClientApplication cca = new ConfidentialClientApplication(_configuration.GetValue<string>("AzureADB2C:ClientId"), authority, redirectUri, new ClientCredential(_configuration.GetValue<string>("AzureADB2C:ClientSecret")), userTokenCache, null);

            // Retrieve the cached access token
            var accounts = await cca.GetAccountsAsync();
            try
            {
                AuthenticationResult result = await cca.AcquireTokenSilentAsync(_configuration.GetValue<string>("AzureADB2C:ApiScopes").Split(' '), accounts.FirstOrDefault(), authority, false);
                accessToken = result.AccessToken;
            }
            catch (Exception)
            {
                // The token was not found in the cache, force a sign out of the user
                // so they must re-authenticate
                await HttpContext.SignOutAsync();
            }

            return accessToken;
        }

        public IActionResult ResetPassword([FromRoute] string scheme)
        {
            scheme = scheme ?? AzureADB2CDefaults.AuthenticationScheme;

            var redirectUrl = Url.Content("~/");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items[AzureADB2CDefaults.PolicyKey] = _configuration.GetValue<string>("AzureADB2C:ResetPasswordPolicyId");
            return Challenge(properties, scheme);
        }
    }
}
