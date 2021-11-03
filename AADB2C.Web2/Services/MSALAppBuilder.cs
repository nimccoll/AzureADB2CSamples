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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using System;
using System.Threading.Tasks;

namespace AADB2C.Web2.Services
{
    // Static builder implementation used for creating an instance of IConfidentialClientApplication
    public static class MSALAppBuilder
    {
        private static IConfidentialClientApplication _cca;

        public static IConfidentialClientApplication BuildConfidentialClientApplication(string clientId, string clientSecret, string redirectUri, string authority)
        {
            if (_cca == null)
            {
                _cca = ConfidentialClientApplicationBuilder.Create(clientId)
                      .WithClientSecret(clientSecret)
                      .WithRedirectUri(redirectUri)
                      .WithAuthority(new Uri(authority))
                      .Build();

                _cca.AddDistributedTokenCache(services =>
                {
                    services.AddDistributedMemoryCache();
                    services.Configure<MsalDistributedTokenCacheAdapterOptions>(o =>
                    {
                        o.Encrypt = true;
                    });
                });
            }
            return _cca;
        }

        public static async Task RemoveAccount(string clientId, string clientSecret, string redirectUri, string authority, string accountId)
        {
            BuildConfidentialClientApplication(clientId, clientSecret, redirectUri, authority);

            var userAccount = await _cca.GetAccountAsync(accountId);
            if (userAccount != null)
            {
                await _cca.RemoveAsync(userAccount);
            }
        }
    }
}
