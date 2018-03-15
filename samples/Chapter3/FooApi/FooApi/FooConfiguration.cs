using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace FooApi
{
    public static class FooConfiguration
    {
        public static IServiceCollection ConfigureServices(IServiceCollection services) =>
            services
                .AddApiVersioning(setup=>
                {
                    setup.AssumeDefaultVersionWhenUnspecified = true;
                    setup.DefaultApiVersion = new ApiVersion(1, 0);
                })
                .AddAuthorization(setup=>
                {
                    setup.AddPolicy("GetPolicy", requirements =>
                    {
                        requirements.RequireClaim("Permission", new string[] { "Read" });
                    });

                    setup.AddPolicy("PostPolicy", requirements =>
                    {
                        requirements.RequireClaim("Permission", new string[] { "Write" });
                    });
                })
                .AddMvc()
                .Services;

        public static void Configure(IApplicationBuilder app, Func<IApplicationBuilder, IApplicationBuilder> configureHost) =>
            configureHost(app)
                .UseMvc();
    }
}
