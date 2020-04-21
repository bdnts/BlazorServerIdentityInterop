using System;
using BlazorServerIdentityInterop.Areas.Identity.Data;
using BlazorServerIdentityInterop.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(BlazorServerIdentityInterop.Areas.Identity.IdentityHostingStartup))]
namespace BlazorServerIdentityInterop.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<BlazorServerIdentityInteropContext>(options =>
                    options.UseSqlite(
                        context.Configuration.GetConnectionString("BlazorServerIdentityInteropContextConnection")));

                services.AddDefaultIdentity<BlazorServerIdentityInteropUser>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddEntityFrameworkStores<BlazorServerIdentityInteropContext>();
            });
        }
    }
}