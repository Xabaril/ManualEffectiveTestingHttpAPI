using Acheve.AspNetCore.TestHost.Security;
using FooApi;
using FooApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FunctionalTests.Seedwork
{
    public class FooFixture
    {
        private static Checkpoint CheckPoint = new Checkpoint();
        public TestServer FooServer { get; private set; }

        public FooFixture()
        {
              var hostBuilder = new WebHostBuilder()
                .UseStartup<TestStartup>();

            FooServer = new TestServer(hostBuilder);

            FooServer.Host.MigrateDbContext<FooDbContext>((ctx,sp)=> { });

            CheckPoint.TablesToIgnore = new[] { "__EFMigrationsHistory" };
        }

        public async Task ExecuteScopeAsync(Func<IServiceProvider,Task> action)
        {
            using (var scope = FooServer.Host.Services.GetService<IServiceScopeFactory>().CreateScope())
            {
                await action(scope.ServiceProvider);
            }
        }

        public async Task ExecuteDbContextAsync(Func<FooDbContext, Task> action)
        {
            await ExecuteScopeAsync(sp => action(sp.GetService<FooDbContext>()));
        }

        public static void ResetDatabase()
        {
            CheckPoint.Reset(@"Server=LRUIZ-LAPTOP\SQLEXPRESS;Database=Foo;Integrated Security=true").Wait();
        }

        public async Task<Bar> ABarInTheDatabase()
        {
            var bar = new Bar();
            await ExecuteDbContextAsync(async context =>
            {
                await context.AddAsync(bar);
                await context.SaveChangesAsync();
            });
            return bar;
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
                .AddDbContext<FooDbContext>(options =>
                {
                    options.UseSqlServer(@"Server=LRUIZ-LAPTOP\SQLEXPRESS;Database=Foo;Integrated Security=true", sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Bar).Assembly.GetName().Name);
                    });
                })
                .AddAuthentication(defaultScheme: "TestServer")
                .AddTestServerAuthentication();
        }

        public void Configure(IApplicationBuilder app)
        {
            FooConfiguration.Configure(app, host => host.UseAuthentication());
        }
    }
}
