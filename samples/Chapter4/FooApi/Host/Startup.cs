using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FooApi;
using FooApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Host
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
            FooConfiguration
                .ConfigureServices(services)
                .AddDbContext<FooDbContext>(options =>
                {
                    options.UseSqlServer(@"Server=LRUIZ-LAPTOP\SQLEXPRESS;Database=Foo;Integrated Security=true", sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Bar).Assembly.GetName().Name);
                    });
                })
                .AddAuthentication()
                .AddJwtBearer();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            FooConfiguration.Configure(app, host =>
                 host.UseStaticFiles()
                 .UseAuthentication()
                 .UseExceptionHandler("/Home/Error"));
        }
    }
}