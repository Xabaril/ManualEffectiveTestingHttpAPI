
# Authorización

La autorización es otro de los elementos habituales cuando desarrollamos HTTP API, probarlas siempre es un dolor de cabeza porqie tenemos que simular diferentes usuarios con diferentes claims para diferentes acciones. Separar nuestro alojamiento de nuestro API sin duda nos ayudará a simplificar los problemas de testing que tendriamos si no lo hubieramos hecho asi. Para mostrar como trabajar con nuestros tests y API que hagan uso de la autorización modificaremos nuestro ejemplo anterior tal y como vemos a continuación.

```csharp
    [Route("api/v{version:apiVersion}/[controller]")]
    public class FooController
        :Controller
    {
        [HttpGet()]
        [Authorize("GetPolicy")]
        public IActionResult Get(int id)
        {
            var bar = new Bar() { Id = id };

            return Ok(bar);
        }

        [HttpPost()]
        [Authorize("PostPolicy")]
        public IActionResult Post([FromBody]Bar bar)
        {
            return CreatedAtAction(nameof(Get), new { id = bar.Id });
        }
    }
```

Por supuesto, modificaremos también la configuración de nuestro API para dar de alta los requerimientos de las políticas que acabamos de usar.

```csharp
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
```

Fijémonos en este punto que nuestro API solamente habla de la autorización y de los requerimientos que tenemos para que un API pueda ser ejecutada, pero no habla para nada de como un usuario es autenticado o los flows de autenticación usados, puesto que esto es una cuestion solamente referida a nuestro host. Es en nuestro alojamiento donde fijamos estos mecanismos de autenticación, como por ejemplo podria ser en nuestro host por defecto con los siguientes cambios de código.

```csharp
    public void ConfigureServices(IServiceCollection services)
        {
            FooConfiguration
                .ConfigureServices(services)
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
```

Fíjese como se ha agregado los elementos referidos a autenticacion *AddAuthentication y AddJwtBearer* , en este caso sin configurar puesto que por ahora no es necesario, y el el middleware *UseAutentication* en nuestro alojamiento por defecto. Probablemente este alojamiento estaria configurado con algún servidor de identidad como Identity Server en el que tendrian un conjunto de usuarios con el que podriamos probar etc. Testar nuestro API dependiendo de estos elementos probablemente seria muy duro, tendriamos que tener un juego de usuarios dados de alta, el servidor de identidad corriendo etc etc Parece por lo tanto que esto deberia ser un punto dónde tendriamos que trabajar para hacer una experiencia mucho más sencilla.

Para mejorar esta esperiencia, y puesto que tenemos diferentes host, para nuestro proyecto y para nuestros tests podemos sin más crear un manejador de autenticación diferente que podremos configurar como nos interese para cada una de nuestras pruebas.

> Puede leer más acerca de la autenticación en .NET Core en [AuthenticationHandler<TOptions>](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1?view=aspnetcore-2.0) y en authenticación con [*schemas* especificos](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme?tabs=aspnetcore2x)

En las siguientes lineas podemos ver un ejemplo de un *AuthenticationHandler* que nos permite incluir un conjunto de claims, en este caso hardcoded, más adelante veremos como hacer esto más flexible.

```csharp
    public class MyTestsAuthenticationHandler
        : AuthenticationHandler<MyTestOptions>
    {
        public MyTestsAuthenticationHandler(IOptionsMonitor<MyTestOptions> options, 
            ILoggerFactory logger,
            UrlEncoder encoder, 
            ISystemClock clock) 
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name,"HttpAPITesting"),
                new Claim("Permission","Read")
            };

            var identity = new ClaimsIdentity(
               claims: claims,
               authenticationType: Scheme.Name,
               nameType: ClaimTypes.Name,
               roleType: ClaimTypes.Role);

            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                new AuthenticationProperties(),
                Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class MyTestOptions
        : AuthenticationSchemeOptions
    {
    }
```

En este código simplemente estamos agregando dos claims, Name y Permission ha nuestro contexto de peticion con el que se ejecutarán nuestras pruebas. Para agregar este AuthenticationHandler seguimos los mismos pasos que con otros muchos como AddJwtBearer etc, en nuestra clase *TestStartup*

```csharp
    class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            FooConfiguration.ConfigureServices(services)
                .AddAuthentication(defaultScheme:"TestServer")
                .AddScheme<MyTestOptions,MyTestsAuthenticationHandler>("TestServer",_=> { });
        }

        public void Configure(IApplicationBuilder app)
        {
            FooConfiguration.Configure(app, host => host.UseAuthentication());
        }
    }
```

Si ejecutamos nuestro test *get_bar_when_requested* veremos como nuestra prueba vuelve a funcionar, sin embargo, el test *post_new_bar* no funcionará, 403 Forbidden, porque el valor de la claim *Permission* no es el adecuado en la política seleccionada. 

Evidentemente podriamos mejorar nuestro *MyTestAuthenticationHandler* para que los claims introducidos se pudieran de forma dinámica establecer en cada test según lo necesitáramos. No obstante, la ya conocida *Acheve.TestHost* nos otorga esta funcionalidad de una forma sencilla con lo que nos podemos ahorrar este codigo, aunque conocer como funciona seguramente le ayudará en otros muchos casos.

Modificamos nuestro TestStartup para usar *Acheve.TestHost*, note como esta librería nos provee del método **AddTestServerAuthentication** :

```csharp
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
```

Ahora, solamente tenemos que establecer para cada test cuales son las claims con las que queremos ejecutar nuestra peticion. Para ello disponemos del método *WithIdentity* , gracias al cual esta tarea resulta tremendamente sencilla.

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
            var response = await Given.FooServer
                .CreateHttpApiRequest<FooController>(foo=>foo.Get(1),new { version = 1 })
                .WithIdentity(new List<Claim>() {  new Claim("Permission","Read")})
                .GetAsync();

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task get_forbideen_if_not_authenticated_when_requested()
        {
            var response = await Given.FooServer
                .CreateHttpApiRequest<FooController>(foo => foo.Get(1), new { version = 1 })
                .WithIdentity(new List<Claim>() { new Claim("Permission", "NonReadClaim") })
                .GetAsync();

            response.StatusCode
                .Should()
                .Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task post_new_bar()
        {
            var bar = new Bar() { Id = 1 };
            var response = await Given.FooServer
                .CreateHttpApiRequest<FooController>(foo => foo.Post(bar), new { version = 1 })
                .WithIdentity(new List<Claim>() { new Claim("Permission", "Write") })
                .PostAsync();

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task post_get_forbidden_if_not_authenticated_when_requested()
        {
            var bar = new Bar() { Id = 1 };
            var response = await Given.FooServer
                .CreateHttpApiRequest<FooController>(foo => foo.Post(bar), new { version = 1 })
                .WithIdentity(new List<Claim>() { new Claim("Permission", "NonWriteClaim") })
                .PostAsync();

            response.StatusCode
                .Should()
                .Be(HttpStatusCode.Forbidden);
        }
    }
```

# Conclusiones

En este capítulo hemos visto como enfrentarnos al manejo de de los mecanismos de authorización en nuestras HTTP API de una forma sencilla con el uso de *TestServer* y *Acheve.TestHost*.

> [Continuar la lectura](./Chapter4.md)