
# Trabajando con datos

Otros de los puntos importantes cuando hacemos tests de integración tiene que ver el acceso a los datos. Hasta ahora en nuestros ejemplos, el controlador de nuestra API devolvía datos *dummy*, pero en aplicaciones reales normalmente necesitamos obtener información de algún almacenamiento ya sea una base de datos relacional, no-sql. En este capítulo veremos como trabajar de manera efectiva con datos en nuestros test de integración. En nuestro caso, hemos optado por usar EntityFrameworkCore para los ejemplos pero piense que lo que vamos a contarle es aplicable a muchos otros usos como Dapper.NET, ADO.NET, CosmosDb, etc.

Entonces, lo primero que vamos a hacer es crear nuestro contexto de acceso a datos:

```csharp
public class FooDbContext : DbContext
{
    public DbSet<Bar> Bars { get; set; }

    public FooDbContext(DbContextOptions<FooDbContext> options) : base(options)
    {
    }
}
```

Antes de continuar, me gustaría formularle la siguiente pregunta:

> ¿Dónde configuraría el contexto de acceso a datos? ¿En el alojamiento o en la API HTTP?

La respuesta es sencilla: *En el alojamiento*. Sin entrar a debatir si es una buena práctica o no, imagine que desea usar en sus tests una base de datos en memoria, si añadimos EntityFrameworkCore a los servicios de nuestra API HTTP y añadimos el proveedor para SQL Server, estaríamos evitando la posiblidad de que nuestros alojamientos pudieran usar el proveedor de EntityFrameworkCore que quisieran, que en este caso sería el proveedor de memoria.

A continuación vamos añadir EntityFrameworkCore a nuestros alojamientos.

En nuestro alojamiento web:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    FooConfiguration
        .ConfigureServices(services)
        .AddDbContext<FooDbContext>(options =>
        {
            options.UseSqlServer("connection_string", sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(Bar).Assembly.GetName().Name);
            });
        })
        .AddAuthentication()
        .AddJwtBearer();
}
```

En nuestro alojamiento de tests:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    FooConfiguration.ConfigureServices(services)
        .AddDbContext<FooDbContext>(options =>
        {
            options.UseSqlServer("connection_string", sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(Bar).Assembly.GetName().Name);
            });
        })
        .AddAuthentication(defaultScheme: "TestServer")
        .AddTestServerAuthentication();
}
```

Aunque para nuestro ejemplo vamos a usar el mismo proveedor, podemos usar el proveedor que queramos en cada alojamiento, ya que hemos delegado esa responsabilidad desde nuestra API HTTP. Asi por ejemplo podriamos tener SqlServer en nusetro alojamiento web y InMemory en nuestros tests, aunque esto tiene otros problemas, puesto que usar InMemory no daria completitud a nuestros tests al no ser en realidad un base de datos relacional ya que carece de las constraints típicas de estos modelos.

Para no complicar el ejemplo, vamos a usar directamente nuestro contexto de acceso a datos en nuestro controlador, es usted libre de modificar el ejemplo y usar un patrón repositorio o lo que usted crea conveniente en su caso.

```csharp
[Route("api/v{version:apiVersion}/[controller]")]
public class FooController
    :Controller
{
    private readonly FooDbContext _context;

    public FooController(FooDbContext context)
    {
        this._context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet()]
    [Authorize("GetPolicy")]
    public async Task<IActionResult> Get(int id)
    {
        var bar = await _context.Bars.FindAsync(id);

        return Ok(bar);
    }

    [HttpPost()]
    [Authorize("PostPolicy")]
    public async Task<IActionResult> Post([FromBody]Bar bar)
    {
        await _context.AddAsync(bar);
        return CreatedAtAction(nameof(Get), new { id = bar.Id });
    }
}
```

Ahora que tenemos configurada nuestra API y nuestros alojamientos, vamos a automatizar la creación de nuestra base de datos. Para ello vamos a hacer uso de la siguiente clase que se encargará de crear la base de datos y aplicar las migraciones. Para compartirla entre los alojamientos, vamos a añadirla a nuestra API HTTP, aunque en readlidad esta pieza suele ser un elemento común que solemos tener en nuestras librerías *Seedwork* .

```csharp
public static class IWebHostExtensions
{
    public static IWebHost MigrateDbContext<TContext>(this IWebHost webHost, Action<TContext, IServiceProvider> seeder) where TContext : DbContext
    {
        using (var scope = webHost.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var logger = services.GetRequiredService<ILogger<TContext>>();

            var context = services.GetService<TContext>();

            try
            {
                logger.LogInformation($"Migrating database associated with context {typeof(TContext).Name}");

                context.Database
                    .Migrate();

                seeder(context, services);

                logger.LogInformation($"Migrated database associated with context {typeof(TContext).Name}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred while migrating the database used on context {typeof(TContext).Name}");
            }
        }

        return webHost;
    }
}
```
Para hacer uso de ella, modificaremos el método *Main* la clase Program.cs de nuestro alojamiento web.

```csharp
public static void Main(string[] args)
{
    BuildWebHost(args)
        .MigrateDbContext<FooDbContext>((ctx,sp)=> { })
        .Run();
}
```

Modificaremos el constructor de la clase FooFixture en nuestro alojamiento de tests:

```csharp
public class FooFixture
{
    public TestServer FooServer { get; private set; }

    public FooFixture()
    {
            var hostBuilder = new WebHostBuilder()
            .UseStartup<TestStartup>();

        FooServer = new TestServer(hostBuilder);

        FooServer.Host.MigrateDbContext<FooDbContext>((ctx,sp)=> { });
    }
}
```

Una vez hecho esto, cuando ejecutemos nuestros tests, se creará automáticamente la base de datos y se aplicarán las migraciones, con lo que nuestros tests ya se ejecutan hasta la base de datos.

Ahora debemos afrontar dos retos, el primero tiene que ver con la semilla de datos. Para cada uno de nuestros tests necesitaremos contar un conjunto común de datos (Maestros) y con un conjunto de datos propios de nuestro test. Para este caso solemos contar con una clase llamada *Given* que describe el estado en el que debe estar la aplicación antes de comenzar nuestro escenario de test. Vamos a ver un ejemplo con el test que recupera un elemento de nuestra API HTTP.

Para poder añadir datos a nuestra semilla vamos a necesitar el contexto de acceso a datos. Usaremos el contexto de acceso a datos porque nos permitirá de una forma sencilla añadir los datos que nuestro test necesita para poder ejecutarse y al estar fuertemente tipado en caso de hacer una refactorización sobre el modelo nuestros tests no serán frágiles. Para poder obtener el contexto de acceso a datos vamos a añadir dos métodos a nuestra clase FooFixture:

```csharp
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
```

Ahora que podemos obtener el contexto de acceso a datos, vamos a usarlo para añadir un elemento en nuestro test y poder comprobar que nuestra API accede a la base de datos para recuperarlo. Vamos a modificar el test *get_bar_when_requested*

```csharp
[Fact]
public async Task get_bar_when_requested()
{
    var bar = new Bar();
    await Given.ExecuteDbContextAsync(async context =>
    {
        await context.AddAsync(bar);
        await context.SaveChangesAsync();
    });

    var response = await Given.FooServer
        .CreateHttpApiRequest<FooController>(foo=>foo.Get(bar.Id),new { version = 1 })
        .WithIdentity(new List<Claim>() {  new Claim("Permission","Read")})
        .GetAsync();

    response.EnsureSuccessStatusCode();
    
    var json = await response.Content.ReadAsStringAsync();
    var result = JsonConvert.DeserializeObject<Bar>(json);
    
    result.Id.Should().Be(bar.Id);
}
```

El test pasa, pero tenemos mucho código que no aporta nada a la legibilidad del test, como por ejemplo el código de semilla, que probablemente en un futuro necesitemos volver a reutilizar en alguno de nuestros test. Para ello vamos a mover el código de semilla a nuestra clase *Given* y le daremos un nombre más significativo:

```csharp
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
```

Ahora podemos modificar nuestro test para llamar a este nuevo método:

```csharp
[Fact]
public async Task get_bar_when_requested()
{
    var bar = await Given.ABarInTheDatabase();

    var response = await Given.FooServer
        .CreateHttpApiRequest<FooController>(foo=>foo.Get(bar.Id),new { version = 1 })
        .WithIdentity(new List<Claim>() {  new Claim("Permission","Read")})
        .GetAsync();

    response.EnsureSuccessStatusCode();
    
    var json = await response.Content.ReadAsStringAsync();
    var result = JsonConvert.DeserializeObject<Bar>(json);
    
    result.Id.Should().Be(bar.Id);
}
```

Por último, podríamos mover todo el código de la serialziación del objeto de respuesta de nuestra API a un método extensor de nuestro TestServer, pero eso se lo dejaré a usted a modo de reto.

Por otro lado tenemos que afrontar el reto de conseguir que cada uno de nuestros tests dejen la base de datos en el estado que estaba antes de ejecutarse para así evitar *side effects* entre ejecución y ejecución de cada test. Por ponerle un ejemplo, imagine que en unos de nuestros test quiere comprobar que se recuperan una cantidad de datos de la API HTTP. Para ello creará una semilla que inserte un número de datos conocido y llamará al método de la API para comprobar que así es. Pero tambiém podemos tener un test que compruebe que nuestra API inserta datos. Como el orden de los tests es aleatorio, podría darse que el test que comprueba que nuestra API inserta registros se ejecute antes que nuestro test que los recupera y obtener un resultado no esperado en nuestra aserción.

Seguramente que la forma más sencilla que se le ocurre de solventar este reto es la recreación de la base de datos en cada ejecución de nuestros tests, pero el inconveniente de esta aproximación es el tiempo de duración entre el borrado y la creación. Para una base de datos pequeña esta duración puede ser despreciable, pero seramos realistas, nuestras bases de datos suelen tener una gran cantidad de tablas con muchos datos maestros que hacen que se tome un largo tiempo en estos procesos que al final hacen que este tipo de tests dejen de aportar valor al equipo y acaban abandonandose.

Para solventar este reto disponemos un paquete de NuGet llamado **Respawn**. Respawn nos permitirá dejar la base de datos en el estado que estaba antes de ejecutar cada test sin borrarla y sin eliminar los datos de las tablas que le especifiquemos. Así pues vamos a añadir un poco de fontanería a nuestra clase FooFixture para usar Respawn. 

Lo primero será declaranos un variable estática del tipo CheckPoint

```csharp
private static Checkpoint CheckPoint = new Checkpoint();
```

A continuación debemos indicarle a Respawn que tablas no debe borrar, en nuestro caso por ejemplo la tabla de migraciónes de nuestro contexto de datos (Puede añadir aquí sus tablas maestras):

```csharp
CheckPoint.TablesToIgnore = new[] { "__EFMigrationsHistory" };
```

Y por último, añadir un método que nos permita resetar los datos:

```csharp
public Task ResetDatabase()
{
    return CheckPoint.Reset("connection_string");
}
```

Así quedaría nuestra clase FooFixture:

```csharp
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
        return CheckPoint.Reset("").Wait();
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
```

Ahora necesitamos algún mecanismo para que antes de ejecutarse cada test se eliminen los datos de nuestra base de datos. Para ello vamos a apoyarnos en los mecanismos que nos ofrece xUnit. En este caso vamos a usar un atributo *BeforeAfterTestAttribute* y en concreto vamos a sobreescribir el método *Before* para asegurarnos que antes de ejecutarse el test la base de datos esté limpia:

```csharp
public class ResetDatabaseAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
        FooFixture.ResetDatabase();
    }
}
```
Para hacer uso de este atributo vamos a decorar nuestros tests:

```csharp
[Fact]
[ResetDatabase]
public async Task get_bar_when_requested()
{
    var bar = await Given.ABarInTheDatabase();
    var response = await Given.FooServer
        .CreateHttpApiRequest<FooController>(foo=>foo.Get(bar.Id),new { version = 1 })
        .WithIdentity(new List<Claim>() {  new Claim("Permission","Read")})
        .GetAsync();

    response.EnsureSuccessStatusCode();
    var json = await response.Content.ReadAsStringAsync();
    var result = JsonConvert.DeserializeObject<Bar>(json);
    result.Id.Should().Be(bar.Id);
}
```

Si volvemos a ejecutar nuestro test y comprobamos nuestra base de datos, podremos observar como todas las tablas excepto las que hayamos indicado en el propiedad *TablesToIgnore* estarán vacías.

# Conclusiones

En este capítulo hemos visto como podemos trabajar con datos reales en nuestros tests de integración y como solventar los restos necesarios para conseguir un óptimo aislamiento entre la ejejcución de nuestros tests.