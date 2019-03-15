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
using AADB2C.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AADB2C.Web
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Expose access to the HttpContext
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

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
                        ctxt.ProtocolMessage.Scope += $" offline_access {Configuration.GetValue<string>("AzureADB2C:ApiScopes")}";
                        await Task.Yield();
                    },
                    // Retrieve and cache access tokens for the WebAPI to be used later by exchanging the
                    // authorization code for access tokens
                    OnAuthorizationCodeReceived = async ctxt =>
                    {
                        // Extract the code from the response notification
                        var code = ctxt.ProtocolMessage.Code;

                        // The cache is built using the signed in user's identity so we must retrieve their
                        // name identifier from the claims collection
                        string signedInUserID = ctxt.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;

                        // Build the identifier for the token issuing authority. Values are retrieved from
                        // configuration.
                        // Ex. https://login.microsoftonline.com/tfp/{your B2C tenant}.onmicrosoft.com/{your B2C sign-up signin-in policy name}/v2.0
                        string authority = $"{Configuration.GetValue<string>("AzureADB2C:Instance")}/{Configuration.GetValue<string>("AzureADB2C:Domain")}/{Configuration.GetValue<string>("AzureADB2C:SignUpSignInPolicyId")}/v2.0";

                        // Build the redirect Uri from the current HttpContext
                        // Ex. https://localhost:44340/signin-oidc
                        HttpRequest request = ctxt.HttpContext.Request;
                        string redirectUri = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);

                        // Construct a token cache based on the signed in User ID and the current HttpContext
                        TokenCache userTokenCache = new MSALSessionCache(signedInUserID, ctxt.HttpContext).GetMsalCacheInstance();

                        // Create an instance of the ConfidentialClientApplication to retrieve the access token
                        // via the authorization code using the authority, redirectUri, the token cache and the
                        // client ID and client secret of the web application (from configuration)
                        ConfidentialClientApplication cca = new ConfidentialClientApplication(Configuration.GetValue<string>("AzureADB2C:ClientId"), authority, redirectUri, new ClientCredential(Configuration.GetValue<string>("AzureADB2C:ClientSecret")), userTokenCache, null);

                        try
                        {
                            // Retrieve and cache the access token for the WebAPI we wish to invoke. The
                            // scope values for the WebAPI are pulled from configuration.
                            AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(code, Configuration.GetValue<string>("AzureADB2C:ApiScopes").Split(' '));

                            ctxt.HandleCodeRedemption(result.AccessToken, result.IdToken);
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
                    }
                };
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
            app.UseSession(); // Enable session
            app.UseAuthentication(); // Enable authentication
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
