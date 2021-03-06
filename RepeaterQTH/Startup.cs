
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RepeaterQTH.Areas.Identity;
using RepeaterQTH.Components;
using RepeaterQTH.Data;
using RepeaterQTH.Data.Services;

namespace RepeaterQTH
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(
                    Configuration.GetConnectionString("DefaultConnection")));
          
            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services
                .AddScoped<AuthenticationStateProvider,
                    RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();
            //services.AddDatabaseDeveloperPageExceptionFilter();
            using StreamReader r = new StreamReader("connection.json");
            string json = r.ReadToEnd();
            var items = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    var googleAuthNSection =
                        Configuration.GetSection("Authentication:Google");

                    options.ClientId = items["Google-clientID"];
                    options.ClientSecret = items["Google-clientSecret"];
                })
                .AddTwitter(options =>
                {
                    options.ConsumerKey = items["TwitterAPIKey"];
                    options.ConsumerSecret = items["TwitterAPISecret"];
                    options.RetrieveUserDetails = true;
                })
                .AddMicrosoftAccount(options =>
                {
                    options.ClientId = items["MicrosoftClientID"];
                    options.ClientSecret = items["MicrosoftSecret"];
                });
            services.AddSingleton<RepeaterDirectoryService>();
            services.AddSingleton<LocationService>();
            services.AddSingleton<SearchData>();
            services.AddSingleton<PageHistoryState>();
            services.AddTransient<IEmailSender, EmailSender>();
            services.Configure<AuthMessageSenderOptions>(Configuration);
            services
                .AddCors(x => x.AddPolicy("externalRequests",
                    policy => policy
                        .WithOrigins("https://jsonip.com")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApplicationDbContext dataContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            dataContext.Database.Migrate();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
            app.UseCors("externalRequests");
        }
    }
}
