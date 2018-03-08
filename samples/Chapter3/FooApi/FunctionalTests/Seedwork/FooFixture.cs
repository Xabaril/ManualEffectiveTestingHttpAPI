using Acheve.AspNetCore.TestHost.Security;
using FooApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace FunctionalTests.Seedwork
{
    public class FooFixture
    {
        public TestServer FooServer { get; private set; }

        public FooFixture()
        {
              var hostBuilder = new WebHostBuilder()
                .UseStartup<TestStartup>();

            FooServer = new TestServer(hostBuilder);
        }
    }

    [CollectionDefinition("Foo")]
    public class FooFixtureCollection
        : ICollectionFixture<FooFixture>
    {
    }

    class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            FooConfiguration.ConfigureServices(services)
                .AddAuthentication(defaultScheme: "TestServer")
                .AddTestServerAuthentication();
        }

        public void Configure(IApplicationBuilder app)
        {
            FooConfiguration.Configure(app, host => host.UseAuthentication());
        }
    }
}
