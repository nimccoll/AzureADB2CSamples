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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AADB2C.Functions
{
    public static class JWTToken
    {
        [FunctionName("JWTToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            ClaimsPrincipal claimsPrincipal = await ValidateToken(req, log);

            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            return new OkObjectResult(new string[] { "value1", "value2" });
        }

        private static async Task<ClaimsPrincipal> ValidateToken(HttpRequest request, ILogger log)
        {
            ClaimsPrincipal claimsPrincipal = null;
            string audience = Environment.GetEnvironmentVariable("Audience", EnvironmentVariableTarget.Process);
            string clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
            string tenantId = Environment.GetEnvironmentVariable("TenantId", EnvironmentVariableTarget.Process);
            string authority = Environment.GetEnvironmentVariable("Authority", EnvironmentVariableTarget.Process);
            ConfigurationManager<OpenIdConnectConfiguration> configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>($"{authority}/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
            ISecurityTokenValidator tokenValidator = new JwtSecurityTokenHandler();

            // For debugging/development purposes, one can enable additional detail in exceptions by setting IdentityModelEventSource.ShowPII to true.
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            // check if there is a jwt in the authorization header, return 'Unauthorized' error if the token is null.
            if (request.Headers.ContainsKey("Authorization") && !string.IsNullOrEmpty(request.Headers["Authorization"]))
            {
                // Pull OIDC discovery document from Azure AD. For example, the tenant-independent version of the document is located
                // at https://login.microsoftonline.com/common/.well-known/openid-configuration.
                OpenIdConnectConfiguration config = null;
                try
                {
                    config = await configurationManager.GetConfigurationAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.LogError("Retrieval of OpenId configuration failed with the following error: {0}", ex.Message);
                }

                if (config != null)
                {
                    // Support both v1 and v2 AAD issuer endpoints
                    IList<string> validissuers = new List<string>()
                    {
                        $"https://login.microsoftonline.com/{tenantId}/",
                        $"https://login.microsoftonline.com/{tenantId}/v2.0/",
                        $"https://login.windows.net/{tenantId}/",
                        $"https://login.microsoft.com/{tenantId}/",
                        $"https://sts.windows.net/{tenantId}/"
                    };

                    // Initialize the token validation parameters
                    TokenValidationParameters validationParameters = new TokenValidationParameters
                    {
                        // Application ID URI and Client ID of this service application are both valid audiences
                        ValidAudiences = new[] { audience, clientId },
                        ValidIssuers = validissuers,
                        IssuerSigningKeys = config.SigningKeys
                    };

                    try
                    {
                        // Validate token.
                        SecurityToken securityToken;
                        string accessToken = request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                        claimsPrincipal = tokenValidator.ValidateToken(accessToken, validationParameters, out securityToken);

                        // This check is required to ensure that the Web API only accepts tokens from tenants where it has been consented to and provisioned.
                        if (!claimsPrincipal.Claims.Any(x => x.Type == "http://schemas.microsoft.com/identity/claims/scope")
                            && !claimsPrincipal.Claims.Any(y => y.Type == "scp")
                            && !claimsPrincipal.Claims.Any(y => y.Type == "roles"))
                        {
                            claimsPrincipal = null;
                        }
                    }
                    catch (SecurityTokenValidationException stex)
                    {
                        log.LogError("Validation of security token failed with the following error: {0}", stex.Message);
                    }
                    catch (Exception ex)
                    {
                        log.LogError("Validation of security token failed with the following error: {0}", ex.Message);
                    }
                }
            }

            return claimsPrincipal;
        }
    }
}
