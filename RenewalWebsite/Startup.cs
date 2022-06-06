using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RenewalWebsite.Data;
using RenewalWebsite.Filters;
using RenewalWebsite.Models;
using RenewalWebsite.Services;
using RenewalWebsite.SettingModels;
using RenewalWebsite.Utility;
using System;
using System.Globalization;


namespace TestWebsite
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
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

            configuration = builder.Build();
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));
            services.Configure<CurrencySettings>(Configuration.GetSection("CurrencySettings"));
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
            services.Configure<EmailNotification>(Configuration.GetSection("EmailErrorNotification"));
            services.Configure<LockoutSettings>(Configuration.GetSection("LockoutSettings"));

            string maxAttampt = Configuration["LockoutSettings:MaxAttempt"];
            services.Configure<IdentityOptions>(
                options =>
                {
                    // Default Lockout settings.
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(60);
                    options.Lockout.MaxFailedAccessAttempts = Convert.ToInt32(maxAttampt);
                    options.Lockout.AllowedForNewUsers = true;
                });

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

            //services.AddAuthentication().AddGoogle(googleOptions =>
            //{
            //    googleOptions.ClientId = Configuration["Authentication:Google:ClientId"];
            //    googleOptions.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
            //    googleOptions.CallbackPath = new PathString("/signin-google");
            //    googleOptions.Events = new OAuthEvents
            //    {
            //        OnRemoteFailure = ctx =>
            //        {
            //            var state = ctx.Request.Query["state"].FirstOrDefault();
            //            if (state != null)
            //            {
            //                var options = ctx.HttpContext.RequestServices.GetRequiredService<IOptions<GoogleOptions>>();
            //                try
            //                {
            //                    var properties = options.Value.StateDataFormat.Unprotect(state);

            //                }
            //                catch (Exception)
            //                {

            //                }
            //            }
            //            ctx.Response.Redirect("/Account/Login");
            //            ctx.HandleResponse();
            //            return Task.FromResult(0);
            //        }
            //    };
            //});
            //facebook signin
            //services.AddAuthentication().AddFacebook(facebookOptions =>
            //{
            //    facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
            //    facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
            //    facebookOptions.CallbackPath = new PathString("/signin-facebook");
            //    facebookOptions.Events = new OAuthEvents
            //    {
            //        OnRemoteFailure = ctx =>
            //        {
            //            var state = ctx.Request.Query["state"].FirstOrDefault();
            //            if (state != null)
            //            {
            //                var options = ctx.HttpContext.RequestServices.GetRequiredService<IOptions<GoogleOptions>>();
            //                try
            //                {
            //                    var properties = options.Value.StateDataFormat.Unprotect(state);

            //                }
            //                catch (Exception)
            //                {

            //                }
            //            }
            //            ctx.Response.Redirect("/Account/Login");
            //            ctx.HandleResponse();
            //            return Task.FromResult(0);
            //        }
            //    };
            //});

            //microsoft signin
            //services.AddAuthentication().AddMicrosoftAccount(microsoftOptions =>
            //{
            //    microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ClientId"];
            //    microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
            //    microsoftOptions.CallbackPath = new PathString("/signin-microsoft");
            //    microsoftOptions.Events = new OAuthEvents
            //    {
            //        OnRemoteFailure = ctx =>
            //        {
            //            var state = ctx.Request.Query["state"].FirstOrDefault();
            //            if (state != null)
            //            {
            //                var options = ctx.HttpContext.RequestServices.GetRequiredService<IOptions<GoogleOptions>>();
            //                try
            //                {
            //                    var properties = options.Value.StateDataFormat.Unprotect(state);

            //                }
            //                catch (Exception)
            //                {

            //                }
            //            }
            //            ctx.Response.Redirect("/Account/Login");
            //            ctx.HandleResponse();
            //            return Task.FromResult(0);
            //        }
            //    };
            //});
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

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(Configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
            var options = new RewriteOptions()
              .AddRedirectToHttps();


            app.UseSession();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                //       app.UseBrowserLink();
            }
            else
            {
                //  app.UseExceptionHandler("/Home/Error");
                app.UseExceptionHandler("/Shared/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //   app.UseHttpsRedirection();
            app.UseStaticFiles();

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
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
