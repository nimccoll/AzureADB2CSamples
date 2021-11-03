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
using AADB2C.Web2.Models;
using AADB2C.Web2.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AADB2C.Web2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _client = new HttpClient();
        private readonly DateTime _epoch = new DateTime(1970, 1, 1);

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> CallAPI1()
        {
            // Retrieve an access token for the WebAPI
            string accessToken = await GetAPIAccessToken(_configuration.GetValue<string>("AzureADB2C:Api1Scope"));
            if (string.IsNullOrEmpty(accessToken))
            {
                // If an empty access token is returned, the user must sign in again because their session has
                // expired. Redirect back to this method to force another sign in.
                return RedirectToAction("CallAPI1");
            }
            else
            {
                // Call the WebAPI
                HttpResponseMessage apiResult = await CallAPI(_configuration.GetValue<string>("AzureADB2C:Api1Url"), accessToken);

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
        public async Task<IActionResult> CallAPI2()
        {
            // Retrieve an access token for the WebAPI
            string accessToken = await GetAPIAccessToken(_configuration.GetValue<string>("AzureADB2C:Api2Scope"));
            if (string.IsNullOrEmpty(accessToken))
            {
                // If an empty access token is returned, the user must sign in again because their session has
                // expired. Redirect back to this method to force another sign in.
                return RedirectToAction("CallAPI2");
            }
            else
            {
                // Call the WebAPI
                HttpResponseMessage apiResult = await CallAPI(_configuration.GetValue<string>("AzureADB2C:Api2Url"), accessToken);

                if (apiResult.IsSuccessStatusCode)
                {
                    ViewBag.Markdown = await apiResult.Content.ReadAsStringAsync();
                }
            }

            return View();
        }

        public async Task<IActionResult> Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult ResetPassword([FromRoute] string scheme)
        {
            scheme = scheme ?? AzureADB2CDefaults.AuthenticationScheme;

            var redirectUrl = Url.Content("~/");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items[AzureADB2CDefaults.PolicyKey] = _configuration.GetValue<string>("AzureADB2C:ResetPasswordPolicyId");
            return Challenge(properties, scheme);
        }

        /// <summary>
        /// Retrieves the cached access token for the WebAPI. If an access token cannot be found in the 
        /// cache, return an empty access token.
        /// </summary>
        /// <returns>WebAPI access token. Empty if a cached access token cannot be found.</returns>
        private async Task<string> GetAPIAccessToken(string scope)
        {
            string accessToken = string.Empty;

            // The cache is built using the signed in user's identity so we must retrieve their name identifier
            // from the claims collection
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Reconstruct the token cache based on the signed in User ID and the current HttpContext
            TokenSessionCache tokenSessionCache = new TokenSessionCache(userId, this.HttpContext);
            List<TokenResult> tokenCache = tokenSessionCache.GetTokenCacheInstance();
            TokenResult tokenResult = tokenCache.FirstOrDefault(t => t.scope.Contains(scope));

            if (tokenResult == null)
            {
                // The token was not found in the cache, force a sign out of the user
                // so they must re-authenticate
                await HttpContext.SignOutAsync();
            }
            else
            {
                // Check for access token expiration and get another using refresh token
                accessToken = tokenResult.access_token;
                DateTime expiresOn = _epoch + new TimeSpan(0, 0, tokenResult.expires_on);
                if (expiresOn < DateTime.UtcNow)
                {
                    string authority = $"{_configuration.GetValue<string>("AzureADB2C:Instance")}tfp/{_configuration.GetValue<string>("AzureADB2C:Domain")}/{_configuration.GetValue<string>("AzureADB2C:SignUpSignInPolicyId")}";
                    TokenResult newTokenResult = await TokenHelper.GetAccessTokenByRefreshToken(authority, _configuration.GetValue<string>("AzureADB2C:ClientId"), _configuration.GetValue<string>("AzureADB2C:ClientSecret"), tokenResult.refresh_token, scope);
                    accessToken = newTokenResult.access_token;
                    // Update token cache
                    for (int i = 0; i < tokenCache.Count; i++)
                    {
                        if (tokenCache[i].scope.Contains(scope))
                        {
                            tokenCache[i] = newTokenResult;
                            tokenSessionCache.Persist();
                            break;
                        }
                    }
                }
            }

            return accessToken;
        }

        private async Task<HttpResponseMessage> CallAPI(string url, string accessToken)
        {
            // Add a "Bearer" authentication header value that includes the access token for the API
            // to the Authorization header of the HTTP request to the WebAPI.
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Call the WebAPI
            HttpResponseMessage apiResult = await _client.GetAsync(url);

            return apiResult;
        }
    }
}
