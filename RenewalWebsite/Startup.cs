using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RenewalWebsite.Data;
using RenewalWebsite.Models;
using RenewalWebsite.Filters;
using RenewalWebsite.Services;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using RenewalWebsite.Utility;

namespace RenewalWebsite
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Stripe settings
            services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));
            services.Configure<CurrencySettings>(Configuration.GetSection("CurrencySettings"));
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
            services.Configure<EmailNotification>(Configuration.GetSection("EmailErrorNotification"));

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // Session cache
            services.AddDistributedMemoryCache();
            services.AddSession();

            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication().AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                googleOptions.CallbackPath = new PathString("/signin-google");
            });

            services.AddAuthentication().AddFacebook(facebookOptions =>
            {
                facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
                facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                facebookOptions.CallbackPath = new PathString("/signin-facebook");
            });

            services.AddAuthentication().AddMicrosoftAccount(microsoftOptions =>
            {
                microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ClientId"];
                microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
                microsoftOptions.CallbackPath = new PathString("/signin-microsoft");
            });

            services.AddScoped<LanguageActionFilter>();

            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });

            services.AddMvc(options =>
            {
                options.Filters.Add(new RequireHttpsAttribute());
            })
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

            services.AddScoped<IViewRenderService, ViewRenderService>();

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
            services.AddTransient<IDonationService, DonationService>();
            services.AddTransient<ICurrencyService, CurrencyService>();
            services.AddTransient<ICampaignService, CampaignService>();
            services.AddTransient<ILoggerServicecs, LoggerServicecs>();
            services.AddTransient<IUnsubscribeUserService, UnsubscribeUserService>();
            services.AddTransient<IInvoiceHistoryService, InvoiceHistoryService>();
            services.AddTransient<ICountryService, CountryService>();
            services.AddTransient<CountrySeeder>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, ApplicationDbContext appDbContext)
        {
            app.Use(async (context, next) =>
            {
                await next();

                //After going down the pipeline check if we 404'd. 
                if (context.Response.StatusCode == StatusCodes.Status404NotFound)
                {
                    context.Request.Path = "/Error/Error404";
                    await next();
                }
                if (context.Response.StatusCode == StatusCodes.Status500InternalServerError)
                {
                    context.Request.Path = "/Error/Error500";
                    await next();
                }
                //else
                //{
                //    context.Request.Path = "/Error/Error500";
                //    await next();
                //}

            });

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var options = new RewriteOptions()
                .AddRedirectToHttps();

            app.UseSession();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
            }

            //app.UseStatusCodePagesWithRedirects("/error/{0}");
            app.UseStaticFiles();

            app.UseIdentity();

            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("en"),
                new CultureInfo("zh-CN"),
                new CultureInfo("zh")
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                // Indicate default culture here.
                DefaultRequestCulture = new RequestCulture("en-US"),
                //DefaultRequestCulture = new RequestCulture("en-US"),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = supportedCultures
            });

            // Add external authentication middleware below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

        }
    }
}