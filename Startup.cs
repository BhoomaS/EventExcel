using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MemberSummary.Services;
using System;
using Google;
using MemberSummary.Models;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MemberSummary
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // Add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            //services.AddIdentity<ApplicationUser, IdentityRole>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddDefaultTokenProviders();
            services.AddIdentity<MemberSummary.Models.ApplicationUser, MemberSummary.Models.ApplicationRole>()
     .AddEntityFrameworkStores<ApplicationDbContext>()
     .AddDefaultTokenProviders();

            // Register the IAuthManager with AuthManager implementation
            //IServiceCollection serviceCollection = services.AddScoped<IAuthManager, AuthManager>();
            services.AddScoped<IAuthManager, AuthManager>();
            services.AddControllersWithViews();
            //MemberSummary.Models.Config.Init(Configuration);
            Config.Init(Configuration);
            services.AddAuthentication()
        .AddCookie();

            services.AddAuthorization();
        }




        // Configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
                await next();
            });
            app.UseAuthentication(); // <-- Important for Identity
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
           pattern: "{controller=Account}/{action=Index}/{id?}");
            });



        }
    }
}
