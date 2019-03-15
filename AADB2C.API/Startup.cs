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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AADB2C.API
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
            // Add the JwtBearer authentication middleware and populate its values from the 
            // AzureADB2C section of the configuration file
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
              .AddJwtBearer(jwtOptions =>
              {
                  // Build the identifier for the token issuing authority. Values are retrieved from
                  // configuration. The WebAPI will only accept tokens issued by this authority.
                  // Ex. https://login.microsoftonline.com/tfp/{your B2C tenant}.onmicrosoft.com/{your B2C sign-up signin-in policy name}/v2.0
                  jwtOptions.Authority = $"{Configuration.GetValue<string>("AzureADB2C:Instance")}/{Configuration.GetValue<string>("AzureADB2C:Domain")}/{Configuration.GetValue<string>("AzureADB2C:SignUpSignInPolicyId")}/v2.0";

                  // Set the Audience and the ValidAudience values to the Client ID of the WebAPI (from
                  // configuration). The WebAPI will only accept tokens that were issued for this audience value.
                  jwtOptions.Audience = Configuration.GetValue<string>("AzureAdB2C:ClientId");
                  jwtOptions.TokenValidationParameters.ValidAudience = Configuration.GetValue<string>("AzureAdB2C:ClientId");

                  // Add an event handler for the OnAuthenticationFailed event for logging and debugging purposes
                  jwtOptions.Events = new JwtBearerEvents
                  {
                      OnAuthenticationFailed = async args =>
                      {
                          // Log the error
                          Trace.TraceWarning($"Authentication to the WebAPI failed with the following error: {args.Exception.Message}");
                          Trace.TraceWarning($"Stack Trace: {args.Exception.StackTrace}");
                          if (args.Exception.InnerException != null)
                          {
                              Trace.TraceWarning($"Inner exception is: {args.Exception.InnerException.Message}");
                              Trace.TraceWarning($"Inner exception Stack Trace: {args.Exception.InnerException.StackTrace}");
                          }

                          // For debugging purposes only!
                          var s = $"AuthenticationFailed: {args.Exception.Message}";
                          args.Response.ContentLength = s.Length;
                          args.Response.Body.Write(System.Text.Encoding.UTF8.GetBytes(s), 0, s.Length);

                          await Task.Yield();
                      }
                  };
              });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
