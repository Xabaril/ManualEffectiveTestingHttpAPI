
# Introduction to TestServer

Well, let's start from the very beginning, which is basically create our initial project. For this purpose we can use Visual Studio, Visual Studio Code or the .NET command line interface (CLI). In this step we have to make some important decision for the whole development cycle that follows. 

If we take a close look at the different templates that we have at our disposal we can see how it is always suggested to create our projects by joining both the definition of our HTTP API together with our host.

```
> dotnet new 

ASP.NET Core Empty                                web              [C#], F#          Web/Empty
ASP.NET Core Web App (Model-View-Controller)      mvc              [C#], F#          Web/MVC
ASP.NET Core Web App                              razor            [C#]              Web/MVC/Razor Pages
ASP.NET Core with Angular                         angular          [C#]              Web/MVC/SPA
ASP.NET Core with React.js                        react            [C#]              Web/MVC/SPA
ASP.NET Core with React.js and Redux              reactredux       [C#]              Web/MVC/SPA
ASP.NET Core Web API                              webapi           [C#], F#          Web/WebAPI
``` 

Well, this decision may already be wrong in the first place. The hosting of an application implies restrictions that do not have to be directly related to our HTTP API. In addition they can make difficult for us to resolve tasks of vital importance, like for example the testing of our application.

As a general rule in our HTTP APIs we decide which security policies we want, which controllers or actions need authorization and which ones do not, as well as the dependencies that we have. On the other hand, in the host we decide other elements, as if we allow or not a prefight of CORS, the handling of static elements or what is the effective authentication mechanism.

Separating the hosting and our API will allow us to perform a simpler and more reliable testing, and as many positive aspects as you will see throughout this and the following chapters. Surely, this separation is not anything new, this type of projects structuring was already being implemented since the arrival of Owin/Katana in the latest versions of Web API in .NET Full Framework.

## Splitting Concepts

Well, then what kind of project do we select? So, we'll start by **creating a class library project** called *FooApi* for *NetCoreApp* either from Visual Studio or directly from the CLI with **dotnet new classlib -f netcoreapp2.0**. Of course, this project will be created without the necessary dependencies of an *ASP.NET Core* project, so we will have to add them.

In order to simplify the examples presented we will use the metapackage **Microsoft.AspNetCore.All** although in your projects, if you wish, you can reduce the number of dependencies by selecting the packages you specifically need.

> .NET Core 2.1 includes a new metapackage called **Microsoft.AspNetCore.App** that reduces the number of dependencies with respect to **Microsoft.AspNetCore.All**.

``` PowerShell
Install-Package Microsoft.AspNetCore.All
```

In this project, we will create our HTTP API as we would using the default template. For example creating a small controller on which we will be working adding different features.

``` csharp
    [Route("api/[controller]")]
    public class FooController : Controller
    {
        [HttpGet("")]
        public IActionResult Get(int id)
        {
            var bar = new Bar() { Id = id };

            return Ok(bar);
        }
    }
```

Now we move on to the next step where we will decide which dependencies we are going to use and how we will configure them. Normally we do this on the *Startup* of the host project, but in this case we seek to eliminate coupling, so we create the configuration in our library. We will call this kind of configuration *ApiConfiguration* and for now it will be as follows:

``` csharp
    public static class FooConfiguration
    {
        public static IServiceCollection ConfigureServices(IServiceCollection services) =>
            services
                .AddMvc()
                .Services;

        public static void Configure(IApplicationBuilder app)
        {
            app.UseMvc();
        }
    }
``` 

Basically what we have done is to define what are the necessary elements for our HTTP API to work without requirements imposed by our host. At this point, we can create our host project and looks forward to communicate this one with our API. To proceed, we can create a normal *ASP.NET Core Web* project and add a reference to our *FooApi* library. Depending on the selected template, the *Startup* of this host project will be something similiar to:

``` csharp
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
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
``` 

As you can see, this initial *Startup* contains many other elements that not only refer to our API, as support for static files, the use of *BrowserLink* and potentially many others unnecessary elements. 

Next, we modify our *Startup* class to include our previously defined *FooConfiguration* in *FooApi*, with which we would have something similar to the following code:

```csharp
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
                .ConfigureServices(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            FooConfiguration.Configure(app);
        }
    }
```

Although this configuration it simply works as it is, there a couple of details that can be improved. As you probably know *UseMvc* is a terminal middleware, so if we use it in a separate configuration, it would be advisable to make sure that it is always the last line executed. With a small refactor in our *FooConfiguration* we would have controlled a case like this.

```csharp
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
```

Thanks to this, we can modify the *Configure* method so that it now has the following appearance in our *host* project.

```csharp
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        FooConfiguration.Configure(app, host =>
        {
            if (env.IsDevelopment())
            {
                host.UseBrowserLink();
                host.UseDeveloperExceptionPage();
            }
            else
            {
                host.UseExceptionHandler("/Home/Error");
            }

            host.UseStaticFiles();

            return host;
        });
    }
```

With this small change we can avoid the errors regarding terminal middleware and we have a cleaner way to define the concepts that our API and host need.

## First Tests

With the previous steps we have left our *FooApi* ready to start developing over. It is time to apply TDD to build our tests and guide our development. We will create a first *xUnit* project using the default templates and call it *FunctionalTests*. In it we will begin to define the basic skeleton of our tests.

```csharp
    public class foo_api_should
    {
        [Fact]
        public async Task get_bar_when_requested()
        {
            ... 
        }
    }
```

The basic objective that we try to follow here is to test our API by calling our controllers in the same way that our clients will do it. Of course, hosting our HTTP API on a server and proceeding to execute it is possible but it is far away from what we are looking for due to the many problems it could imply and the technical limitations that it would have. 

With the idea of make our functional tests quite simple and thin so we can execute them as regular test, we will start by adding the package **Microsoft.AspNetCore.TestHost** to our test project. This package will allow us in a very simple way to execute requests to our HTTP API without the need to be hosted in any process.

```PowerShell
Install-Package Microsoft.AspNetCore.TestHost
```

In the following code, we can see how we have built our test *get_bar_when_requested* using the class **TestServer** of the package we have previously added.

```csharp
    [Fact]
    public async Task get_bar_when_requested()
    {
        var hostBuilder = new WebHostBuilder()
            .UseStartup<TestStartup>();

        var server = new TestServer(hostBuilder);

        var response = await server.CreateRequest("api/foo")
            .GetAsync();

        response.EnsureSuccessStatusCode();
    }
```

As you can see **TestServer** is initialized by defining a new host. A host whose requirements may be different from the previous one, as we will see, and which we can now separate thanks to the decision we made initially. This new host takes a new *Startup* class that we have defined it as follows, and that for now is very similar to the one we had in our original host.

```csharp
    class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            FooConfiguration.ConfigureServices(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            FooConfiguration.Configure(app, host => host);
        }
    }
```

## Improving the use of TestServer

In the previous step we have seen how to use **TestServer** to perform our first test to our *FooController*. In this step we will make a small change to avoid repeating all this code as well as improving the execution time since the creation of these **TestServer** objects are not lightweight. To do this, we will use the concept of [Fixtures] (https://xunit.github.io/docs/shared-context.html) in xUnit in order to share our **TestServer** among all the different tests we have.

```csharp
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
    public class FooFixtureCollection : ICollectionFixture<FooFixture>
    {
    }
```

Now we move on to the next step and make all our tests share **TestServer**. As commented previously this will allow us to reduce code duplication and improve the execution time of the tests.

```csharp
    [Collection("Foo")]
    public class foo_api_should
    {
        private readonly FooFixture Given;

        public foo_api_should(FooFixture fooFixture)
        {
            Given = fooFixture;
        }

        [Fact]
        public async Task get_bar_when_requested()
        {
            var response = await Given.FooServer.CreateRequest("api/foo")
                .GetAsync();

            response.EnsureSuccessStatusCode();
        }
    }
```

## Conclusions

In this chapter we have seen the first steps to perform effective "Testing of our HTTP APIs". Throughout the following chapters we will dig deeper into different elements such as working with security, data and URLs management. You can grap the chapter code from "samples/Chaper1" folder;