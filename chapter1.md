



# Introducción

Bueno, empecemos por el principio, que consiste simplemente en crear nuestro proyecto inicial, da igual que sea en Visual Studio, Code o con la CLI de .NET. En este paso ya tenemos que tomar alguna decisión importante para todo el ciclo de desarrollo que sigue. Si nos fijamos en las diferentes plantillas que tenemos a nuestra disposicion podemos ver como siempre se sugiere crear nuestros proyectos juntando tanto la propia definición de nuestro HTTP API junto a nuestro host.

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

Bien, esta decisión ya puede ser equivocada de entrada. El alojamiento de una aplicación implica restricciones que no tienen porque estar relacionados directamente con nuestro HTTP API y que además también nos dificultarán algunas tareas como por ejemplo el propio testing de nuestra aplicación. Por regla general en nuestras HTTP Api decidimos cuales son las politicas de seguridad que queremos, que controladores o acciones necesitan autorizacion y cuales no, asi como las dependencias de las que disponemos. Por su parte en el host decidimos otros elementos, como si permitimos o no un prefight de CORS, el manejo de elementos estáticos o cual es el mecanismo efectivo de autenticación. Separar estos elementos, el alojamiento y nuestro API,  nos aportará como irá viendo a lo largo de este 'libro' muchos elementos positivos como favorecer un testing mucho más sencillo. Seguramente, esto que  estamos comentando no le resulte nada nuevo, puesto que esta práctica ya comenzamos a favorecerla con la llegada de Owin / Katana en las últimas versiones de Web API en .NET Full Framework.

## Separando conceptos

Bien, entonces ¿que tipo de proyecto seleccionamos?. Empezaremos  por crear un proyecto de libreria de clases, *FooApì* para *NetCoreApp* bien desde Visual Studio o directamente desde la CLI con un **dotnet new classlib -f netcoreapp2.0**. Por supuesto, este proyecto se creará sin las dependencias necesarias para poder crear proyectos de *ASP.NET Core* por lo que tendremos que agregarlas. Con el fin de simplificar los ejemplos presentados usaremos el metapackage **Microsoft.AspNetCore.All** aunque por supuesto, en sus proyectos si lo desea podra reducir el número de dependencias seleccionando los paquetes que específicamente necesita.

> .NET Core 2.1 incluye un nuevo metapackage **Microsoft.AspNetCore.App** que reduce el numero de dependencias con respecto a **Microsoft.AspNetCore.All**.

``` PowerShell
Install-Package Microsoft.AspNetCore.All
```

En este proyecto crearemos nuestra HTTP API tal cual lo haríamos en la plantilla por defecto. Por ejemplo creando un pequeño controlador sobre el que iremos trabajando agregando diferentes características típicas de una HTTP API.

``` csharp
    [Route("api/[controller]")]
    public class FooController
        :Controller
    {
        [HttpGet("")]
        public IActionResult Get(int id)
        {
            var bar = new Bar() { Id = id };

            return Ok(bar);
        }
    }
```

Una vez terminado este controlador, el siguiente paso será definir que dependencias tomamos y como las configuramos, lo que haríamos en el *Startup* del proyecto de host, pero que ahora tendremos que crear puesto que partimos de una librería vacía. A esta clase de configuración le llamaremos ApiConfiguration y por ahora  será como sigue.

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

Básicamente lo que hemos hecho es definir cuales son los elementos necesarios para que nuestro HTTP API funcione, nada más, sin requerimientos impuestos por nuestro host. Ahora, llegados hasta aqui, ya podemos crerar nuestro proyecto de host e indicarle como trabajar con nuestro API. Procederemos entonces a crear un proyecto normal de *ASP.NET Core Web* y agregarle una referencia a nuestro *FooApi*. Dependiendo de la plantilla seleccionada, el startup de este proyecto de host es algo similar a:

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

Como puedes observar, este startup inicial contiene otros muchos elementos que no solamente se refieren a nuestro API, el soporte para ficheros estáticos, el uso de BrowserLink y potencialmente muchos otros son ejemplos de los elementos que comentamos inncesarios en el párrafo anterior. Bien, procedemos entonces a modificar esta clase de Startup para incluir la configuración de nuestro *FooApi* con lo que tendremos algo similar al siguiente código:

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

Aunque esta configuración ahora mismo funciona, en realidad hay un par de detalles que pueden ser mejorados. Como seguramente sabrá *UseMvc* es un middleware terminal y por lo tanto si lo ponemos como en nuestro caso en *FooConfiguration* entonces tendremos que asgurarnos que sea siempre la ultima linea ejecutada. Podemos hacer un pequeño refactoring en nuestra clase *FooConfiguration* para intentar evitar estas equivocaciones.

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

Gracias a esto, podremos modificar el método *Configure* para que ahora tenga el siguiente aspecto en nuestro proyecto de *host*. Con este pequeño cambio podemos evitar los errores con respecto a los middleware terminales y nos queda una forma más limpia para definir los conceptos que nuestros HttpAPI y host necesitan.

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

## Primeros tests

Con los pasos anteriores hemos dejado lista nuestra **FooApi**, llega entonces ahora el momento de empezar a construir tests sobre la misma. Crearemos entocnes un primer proyecto, *FunctionalTests*, de xUnit usando las plantillas por defecto que tenemos y en el mismo empezaremos a escribir el esqueleto básico de nuestros tests.

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

El objetivo básico que tenemos aquí es testar nuestro api llamando a nuestros controladores de la misma forma que los clientes de los mismos lo harán. Por supuesto, alojar nuestro HTTP API en un servidor y proceder a ejecutarla es algo posible pero no es ni mucho menos lo que buscamos por la cantidad de problemas que introduciría y las limitaciones técnicas que tendría. Para hacer nuestros tests funcionales y que estos sean simples y los podamos ejecutar como un test cualquiera empezaremos por agregar el paquete **Microsoft.AspNetCore.TestHost**, este paquete nos permitirá de una forma muy sencilla poder ejecutar peticiones a nuestro HTTP API sin necesidad de que el mismo esté alojado en ningun proceso.

```PowerShell
Install-Package Microsoft.AspNetCore.TestHost
```

El siguiente código muestra el método *get_bar_when_requested* modificado para hacer uso de la clase **TestServer** ofrecida por el paquete **Microsoft.AspNetCore.TestHost** que hemos instalado en nuestro proyecto de tests.

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

Como puede observar **TestServer** se inicializa definiendo un nuevo alojamiento, una alojamiento cuyo requerimientos pueden ser diferentes al anterior, como iremos viendo, y que ahora podremos separar gracias a la decision que tomamos inicialmente. Este nuevo host toma una nueva clase de Startup que la hemos definido como sigue, y que por ahora es muy similar a la que teníamos en nuestro host original.

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

## Mejorando el uso de TestServer

En el punto anterior hemos visto como usar **TestServer** para realizar nuestro primer test a *FooController* . En este paso haremos algun pequeño cambio para evitar repetir este código asi como mejorar los tiempos de ejecución ya que la creación de estos objetos **TestServer** no son livianos. Para ello, utilizaremos el concepto de [Fixtures](https://xunit.github.io/docs/shared-context.html) en xUnit con el fin de poder compartir nuestro **TestServer** entre los diferentes  tests que tengamos.

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
    public class FooFixtureCollection
        : ICollectionFixture<FooFixture>
    {
    }
```

Ahora ya podemos hacer que nuestros tests compartar nuestro **TestServer** y ahorrarnos asi repetir código y mejorar el tiempo de ejecución de los mismos.

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

## Conclusiones

En este primer capítulo de este hemos visto los primeros pasos para realizar testing efectivo de nuestros HTTP API, a lo largo de los siguientes capítulos iremos viendo como profundizar en diferentes elementos básicos como trabajar con seguridad, datos y el manejo de las url's. El código asociado a este capítulo puede descargarse de aquí.