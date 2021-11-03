//===============================================================================
// Microsoft FastTrack for Azure
// Azure Active Directory B2C Authentication Samples
//===============================================================================
// Copyright � Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using AADB2C.Web2.Models;
using AADB2C.Web2.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AADB2C.Web2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Expose access to the HttpContext
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Add the AzureADB2C authentication middleware and populate its values from the 
            // AzureADB2C section of the configuration file
            services.AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
                .AddAzureADB2C(options => Configuration.GetSection("AzureADB2C").Bind(options));

            // Configure the OpenIDConnect authentication middleware which is leveraged by the
            // AzureADB2C authentication middleware
            services.Configure<OpenIdConnectOptions>(AzureADB2CDefaults.OpenIdScheme, options =>
            {
                // Set the response type to retrieve both an authorization code and an ID token
                options.ResponseType = $"code id_token";

                // Add event handlers to the OpenIdConnect events that we want to respond to during
                // the authentication handshake
                options.Events = new OpenIdConnectEvents
                {
                    // Add the offline_access scope and the scope values for the WebAPI we want to invoke
                    // from configuration to the Scope property of the protocol message before we redirect
                    // to the identity provider. This will ensure our authorization code has the appropriate
                    // scope to retrieve access tokens for the WebAPI.
                    OnRedirectToIdentityProvider = async ctxt =>
                    {
                        ctxt.ProtocolMessage.Scope += $" offline_access {Configuration.GetValue<string>("AzureADB2C:DefaultScope")}";
                        
                        // If a different B2C policy is being executed, we must update the Authority and IssuerAddress
                        if (ctxt.Properties.Items.ContainsKey("Policy"))
                        {
                            string policy = ctxt.Properties.Items["Policy"].ToLower();
                            string defaultPolicy = Configuration.GetValue<string>("AzureADB2C:SignUpSignInPolicyId").ToLower();
                            ctxt.ProtocolMessage.IssuerAddress = ctxt.ProtocolMessage.IssuerAddress.ToLower().Replace(defaultPolicy, policy);
                        }
                        await Task.Yield();
                    },
                    // Retrieve and cache access tokens for the WebAPI to be used later by exchanging the
                    // authorization code for access tokens
                    OnAuthorizationCodeReceived = async ctxt =>
                    {
                        // Extract the code from the response notification
                        var code = ctxt.ProtocolMessage.Code;
                        var idToken = ctxt.ProtocolMessage.IdToken;
                        string userId = ctxt.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;

                        // Build the identifier for the token issuing authority. Values are retrieved from
                        // configuration.
                        // Ex. https://login.microsoftonline.com/tfp/{your B2C tenant}.onmicrosoft.com/{your B2C sign-up signin-in policy name}/v2.0
                        string policy = Configuration.GetValue<string>("AzureADB2C:SignUpSignInPolicyId");
                        if (ctxt.Properties.Items.ContainsKey("Policy"))
                        {
                            policy = ctxt.Properties.Items["Policy"];
                        }
                        string authority = $"{Configuration.GetValue<string>("AzureADB2C:Instance")}tfp/{Configuration.GetValue<string>("AzureADB2C:Domain")}/{policy}";

                        // Build the redirect Uri from the current HttpContext
                        // Ex. https://localhost:44340/signin-oidc
                        HttpRequest request = ctxt.HttpContext.Request;
                        string redirectUri = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);

                        try
                        {
                            string accessToken = await GetAccessTokens(userId, ctxt.HttpContext, code, authority, redirectUri, Configuration.GetValue<string>("AzureADB2C:ClientId"), Configuration.GetValue<string>("AzureADB2C:ClientSecret"), Configuration.GetValue<string>("AzureADB2C:ApiScopes"));

                            ctxt.HandleCodeRedemption(accessToken, idToken);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError($"Retrieval of access token by authorization code failed with the following error: {ex.Message}");
                            Trace.TraceError($"Stack Trace: {ex.StackTrace}");
                            if (ex.InnerException != null)
                            {
                                Trace.TraceError($"Inner exception is: {ex.InnerException.Message}");
                                Trace.TraceError($"Inner exception Stack Trace: {ex.InnerException.StackTrace}");
                            }
                            throw;
                        }
                    },
                    OnMessageReceived = context =>
                    {
                        // If an error has been raised, then remember the return URL for use by the OnRemoteFailure event.
                        if (!string.IsNullOrEmpty(context.ProtocolMessage.Error) &&
                            context.ProtocolMessage.Error.Equals("access_denied") &&
                            context.ProtocolMessage.ErrorDescription.StartsWith("AADB2C90118"))
                        {
                            context.HttpContext.Items["redirect_uri"] = context.Properties.RedirectUri;
                        }

                        return Task.FromResult(0);
                    },
                    OnRemoteFailure = context =>
                    {
                        // Handle the error that is raised when a user has requested to recover a password.
                        if (!string.IsNullOrEmpty(context.Failure.Message) &&
                            context.Failure.Message.Contains("access_denied") &&
                            context.Failure.Message.Contains("AADB2C90118"))
                        {
                            context.Response.Redirect($"/Home/ResetPassword");
                            context.HandleResponse();
                        }

                        // Handle any other error that is raised.
                        if (!string.IsNullOrEmpty(context.Failure.Message) &&
                            context.Failure.Message.Contains("access_denied") &&
                            context.Failure.Message.Contains("AADB2C90091"))
                        {
                            context.Response.Redirect("/");
                            context.HandleResponse();
                        }

                        return Task.FromResult(0);
                    },
                    OnTokenValidated = context =>
                    {
                        if (context.Properties.Items.ContainsKey("Policy"))
                        {
                            string policy = context.Properties.Items["Policy"].ToLower();
                            if (policy == Configuration.GetValue<string>("AzureADB2C:ResetPasswordPolicyId").ToLower())
                            {
                                context.Response.Redirect("/AzureADB2C/Account/SignIn");
                                context.HandleResponse();
                            }
                        }
                        return Task.FromResult(0);
                    }
                };
            });

            services.AddControllersWithViews();

            // Adds a default in-memory implementation of IDistributedCache.
            services.AddDistributedMemoryCache();

            // Adds session state ensuring that the session cookie is accessible from JavaScript
            // for SPA implementations and making sure the session cookie is essential and sent with every
            // request. This is key to ensuring the session state based token cache functions properly.
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = false; // Allow session cookie to be accessed via JavaScript
                options.Cookie.IsEssential = true; // Make sure the session cookie is sent on every request
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();
            app.UseSession();
            app.UseAuthentication(); // Enable authentication
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private async Task<string> GetAccessTokens(string userId, HttpContext httpContext, string authorizationCode, string authority, string redirectUri, string clientId, string clientSecret, string scopes)
        {
            string accessToken = string.Empty;
            string refreshToken = string.Empty;
            TokenSessionCache tokenSessionCache = new TokenSessionCache(userId, httpContext);
            List<TokenResult> tokenCache = tokenSessionCache.GetTokenCacheInstance();
            string[] apiScopes = scopes.Split(" ");

            if (apiScopes.Length > 0)
            {
                for (int i = 0; i < apiScopes.Length; i++)
                {
                    if (i == 0)
                    {
                        TokenResult tokenResult = await TokenHelper.GetAccessTokenByAuthorizationCode(authority, clientId, clientSecret, authorizationCode, redirectUri, apiScopes[i]);
                        tokenCache.Add(tokenResult);
                        accessToken = tokenResult.access_token;
                        refreshToken = tokenResult.refresh_token;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(refreshToken))
                        {
                            TokenResult tokenResult = await TokenHelper.GetAccessTokenByRefreshToken(authority, clientId, clientSecret, refreshToken, apiScopes[i]);
                            tokenCache.Add(tokenResult);
                        }
                    }
                }
            }

            if (tokenCache.Count > 0)
            {
                tokenSessionCache.Persist();
            }

            return accessToken;
        }
    }
}
