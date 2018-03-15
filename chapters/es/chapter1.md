# Introducción a TestServer

Bueno, empecemos por el principio, que consiste simplemente en crear nuestro proyecto inicial. Podemos utilizar Visual Studio, Visual Studio Code o la interfaz de la línea de comandos (CLI) de .NET. En este paso ya tenemos que tomar alguna decisión importante para todo el ciclo de desarrollo que sigue. 

Si nos fijamos en las diferentes plantillas que tenemos a nuestra disposicion podemos ver como siempre se nos sugiere crear nuestros proyectos juntando la definición de nuestro HTTP API con el host para el mismo.

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

Bien, esta decisión ya puede ser equivocada de entrada. El alojamiento de una aplicación implica restricciones que no tienen porque estar relacionadas directamente con nuestro HTTP API. Además nos pueden dificultar tareas de vital importancia, como por ejemplo el propio testing de nuestra aplicación. 

Por regla general en nuestras HTTP APIs decidimos cuales son las políticas de seguridad que queremos, que controladores o acciones necesitan autorización y cuales no, así como las dependencias de las que disponemos. Por su parte, en el host decidimos otros elementos, como si permitimos o no un prefight de CORS, el manejo de elementos estáticos o cual es el mecanismo efectivo de autenticación. 

Separar el alojamiento y nuestro API nos posibilitará realizar un testing más sencillo y fiable, y otros tantos aspectos positivos como irá viendo a lo largo de este y los siguientes capítulos. Seguramente, está separación no le resulte nada nuevo, este tipo de estructuración ya se venía poniendo en práctica desde la llegada de Owin/Katana en las últimas versiones de Web API en .NET Full Framework.

## Separando conceptos

Bien, ¿Y entonces qué tipo de proyecto seleccionamos? Pues, empezaremos por **crear un proyecto** de **librería de clases** para *NetCoreApp* al que llamaremos *FooApi*. El proceso se puede realizar bien desde Visual Studio o directamente desde la CLI con **dotnet new classlib -f netcoreapp2.0**. Este proyecto se creará sin las dependencias necesarias de un proyecto de *ASP.NET Core*, por lo que tendremos que agregarlas. 

Con el fin de simplificar los ejemplos presentados usaremos el metapackage **Microsoft.AspNetCore.All**, aunque en sus proyectos, si lo desea, podrá reducir el número de dependencias seleccionando los paquetes que específicamente necesita.

> .NET Core 2.1 incluye un nuevo metapackage **Microsoft.AspNetCore.App** que reduce el numero de dependencias con respecto a **Microsoft.AspNetCore.All**.

``` PowerShell
Install-Package Microsoft.AspNetCore.All
```

En este proyecto crearemos nuestra HTTP API tal cual lo haríamos utilizando la plantilla por defecto. Por ejemplo creando un pequeño controlador sobre el que iremos trabajando agregando diferentes características típicas de una HTTP API.

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

El siguiente paso será definir que dependencias utilizaremos y como las configuramos. Normalmente haríamos esto en el *Startup* del proyecto de host, pero en este caso buscamos eliminar acoplamiento, por lo que creamos la configuración en nuestra librería. A esta clase de configuración le llamaremos "ApiConfiguration" y por ahora la definiremos como sigue:

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

Básicamente lo que hemos hecho es definir cuales son los elementos necesarios para que nuestro HTTP API funcione sin requerimientos impuestos por nuestro host. Llegados a este punto, ya podemos crear nuestro proyecto de host e indicarle como trabajar con nuestro API. Procederemos entonces a crear un proyecto de *ASP.NET Core Web* y le agregamos una referencia a nuestro *FooApi*. Dependiendo de la plantilla seleccionada, el *Startup* de este proyecto de host terminará siendo algo similar a:

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

Como puedes observar, este *Startup* inicial contiene otros muchos elementos que no solamente se refieren a nuestro API, como el soporte para ficheros estáticos, el uso de *BrowserLink* y potencialmente muchos otros elementos innecesarios para nuestro caso de uso.

A continuación, modificamos la clase *Startup* para incluir nuestra *FooConfiguration* definida anteriomente en *FooApi*, con lo que tendríamos algo similar al siguiente código:

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

Aunque esta configuración ahora mismo funciona, hay un par de detalles que pueden ser mejorados. Como seguramente sabrá *UseMvc* es un middleware terminal, por lo que si lo utilizamos en una configuración independiente, sería recomendable asegurarnos de que sea siempre la última línea ejecutada. Con un pequeño refactor en nuestra clase *FooConfiguration* tendríamos controlada esta situación.

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

Gracias a esta mejora, podremos modificar el método *Configure* para que ahora tenga el siguiente aspecto en nuestro proyecto de *host*.

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

Con este pequeño cambio podemos evitar los errores con respecto a los middleware terminales y nos queda una forma más limpia para definir los conceptos que nuestro API y host necesitan.

## Primeros tests

Con los pasos anteriores hemos dejado lista nuestra *FooApi*. Llega el momento de aplicar TDD para construir nuestros tests y guiar nuestro desarrollo. Crearemos un primer proyecto de xUnit usando las plantillas por defecto y lo llamaremos *FunctionalTests*. En el mismo empezaremos a definir el esqueleto básico de nuestros tests.

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

El objetivo básico que tenemos aquí es testar nuestro API llamando a nuestros controladores de la misma forma que los clientes de los mismos lo harían. Por supuesto, alojar nuestro HTTP API en un servidor y proceder a ejecutarla es algo posible pero no es ni mucho menos lo que buscamos por la cantidad de problemas que introduciría y las limitaciones técnicas que tendría. 

Para hacer que nuestros tests funcionales sean lo más simples posible y los podamos ejecutar como un test cualquiera, empezaremos por agregar el paquete **Microsoft.AspNetCore.TestHost**. Este paquete nos permitirá de una forma muy sencilla poder ejecutar peticiones a nuestro HTTP API sin necesidad de que el mismo esté alojado en ningun proceso.

```PowerShell
Install-Package Microsoft.AspNetCore.TestHost
```

En el siguiente código, podemos ver como hemos construido nuestro test *get_bar_when_requested* haciendo uso de la clase **TestServer** incluida en el paquete que hemos añadido anteriormente.

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

Como puede observar **TestServer** se inicializa definiendo un nuevo alojamiento. Un alojamiento cuyos requerimientos pueden ser diferentes al anterior, como iremos viendo, y que ahora podremos separar gracias a la decision que tomamos inicialmente. Este nuevo host toma una nueva clase de *Startup* que la hemos definido como sigue, y que por ahora es muy similar a la que teníamos en nuestro host original.

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

En el punto anterior hemos visto como usar **TestServer** para realizar nuestro primer test a *FooController*. En este paso realizaremos un pequeño cambio para evitar repetir código y al mismo tiempo mejorar los tiempos de ejecución ya que la creación de estos objetos **TestServer** es algo costosa. Para ello, utilizaremos el concepto de [Fixtures](https://xunit.github.io/docs/shared-context.html) en xUnit con el fin de poder compartir nuestro **TestServer** entre los diferentes tests que tengamos.

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

Ahora ya podemos hacer que nuestros tests compartan nuestro **TestServer**, lo que nos permitirá reducir la duplación de código y mejorar el tiempo de ejecución de los tests.

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

En este primer capítulo hemos visto los primeros pasos para realizar "Testing efectivo de nuestros HTTP APIs". A lo largo de los siguientes capítulos iremos viendo como profundizar en diferentes aspectos como trabajar con seguridad, datos y el manejo de las URLs. Puedes encontrar el código utilizado en este capítulo en la carpeta de "samples/Chapter1".

> [Continuar la lectura](./Chapter2.md)