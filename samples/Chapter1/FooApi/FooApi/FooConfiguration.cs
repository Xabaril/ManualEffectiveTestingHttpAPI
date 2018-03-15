using Microsoft.AspNetCore.Builder;
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
                .AddMvc()
                .Services;

        public static void Configure(IApplicationBuilder app, Func<IApplicationBuilder, IApplicationBuilder> configureHost) =>
            configureHost(app)
                .UseMvc();
    }
}
