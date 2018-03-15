
# Rutas y parámetros

Si ha llegado hasta aquí ya tendrá un esqueleto fundamental sobre el que iremos profundizando. Con el fin de ir trabajando sobre algo cada vez mas similar a lo que se encontrará en sus proyectos reales iremos ampliando el ejemplo base agregando nuevas funcionalidades en las que iremos tratando diferentes casuísticas. En este caso hablaremos sobre como gestionar las rutas de nuestros controladores en nuestros tests y como trabajar con los parámetros asociados a las diferentes acciones a las que invoquemos.

Para ilustrar los diferentes elementos vamos a aumentar el código de nuestro HTTP API, con el fin de que podamos reflejar diferentes elementos. Los cambios introducidos implican agregar los siguienes paquetes a nusetro *FooApi*.

```PowerShell
Install-Package Microsoft.AspNetCore.Mvc.Versioning
```

```csharp
    [Route("api/v{version:apiVersion}/[controller]")]
    public class FooController
        :Controller
    {
        [HttpGet()]
        public IActionResult Get(int id)
        {
            var bar = new Bar() { Id = id };

            return Ok(bar);
        }

        [HttpPost()]
        public IActionResult Post([FromBody]Bar bar)
        {
            return CreatedAtAction(nameof(Get), new { id = bar.Id });
        }
    }
```

Al código de partida hemos agregado un nuevo método asi como un cambio en la ruta para reflejar versionado de nuestro API. Con estos cambios nso vamos a nuestros tests y podemos reescribirlos tambien como sigue:

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
            var response = await Given.FooServer.CreateRequest("api/v1/foo?id=1")
                .GetAsync();

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task post_new_bar()
        {
            var response = await Given.FooServer.CreateRequest("api/v1/foo")
                .And(message =>
                {
                    var content = JsonConvert.SerializeObject(new Bar() { Id = 1 });
                    message.Content = new StringContent(content,Encoding.UTF8,"application/json");

                }).PostAsync();

            response.EnsureSuccessStatusCode();
        }
    }
```

Como puede observar, ahora que ya tenemos un par de test simples ya hay algunas cosas que nos hacen ver que tendremos algunos *code smells*. El primero de ellos se refiere al propio uso de las rutas, en ambos tests estamos repitiendo la ruta utilizada *api/v1/foo* y eso nos puede implicar como se imaginará problemas con en cualquier otro sitio donde tengamos magic strings y código repetido.

Un primer intento para arreglar esto suele basarse en la creación de una pequeña clase dónde podamos gestionar estas rutas, como podria ser la siguiente, que por supuesto tambien es susceptible de ser mejorada:

```csharp
    static class FooAPI
    {
        static string BASEURI = "api/v1/foo";

        public static class Get
        {
            public static string Bar(int id)
            {
                return $"{BASEURI}?id={id}";
            }
        }

        public static class Post
        {
            public static string Bar()
            {
                return BASEURI;
            }
        }
    }
```

Ahora en nuestros tests, eliminaremos las *magic strings* en favor del uso de estas clases:

```csharp
    [Fact]
    public async Task get_bar_when_requested()
    {
        var response = await Given.FooServer.CreateRequest(FooAPI.Get.Bar(1))
            .GetAsync();

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task post_new_bar()
    {
        var response = await Given.FooServer.CreateRequest(FooAPI.Post.Bar())
            .And(message =>
            {
                var content = JsonConvert.SerializeObject(new Bar() { Id = 1 });
                message.Content = new StringContent(content,Encoding.UTF8,"application/json");

            }).PostAsync();
 
        response.EnsureSuccessStatusCode();
    }
```
Otro de los elementos con el que nos encontramos  es que en todos los métodos donde estemos pasando tipos complejos como es el caso de nuestro **Bar** tendremos que andar repitiendo el trabajo de serialización. Esto al igual que hemos hecho con las rutas tambien podria refactorizarse usando Builders y disponiéndolos en nuestro *Given*, no obstante intentaremos presentar el uso de una librería,*Acheve.TestHost*, que nos podria ayudar a simplificar nuestros tests. Instalaremos por lo tanto esta librería en nuestro proyecto de tests


```PowerShell
Install-Package Acheve.TestHost
```

Esta librería nos aporta un nuevo método de extensión para nuestro *TestServer* llamado *CreateHttpApiRequest* gracias al cual podremos tener nuestros tests anteriores de una forma mas simple. En este momento *CreateHttpApiRequest* solamente es válido para HTTP API usando Attribute Routing, por lo tanto no funcionará de forma correcta para API HTTP que se basen en *conventional routes*. Ahora podemos utilizar este método *CreateHttpApiRequest* y librarnos de tener que conocer las rutas y como los difernetes elementos se mapean a los diferentes segmentos que podamos tener en las mismas asi como el trabajo de serialización de nuestros objetos puesto que el se encarga de todos estos aspectos.

```csharp
    [Fact]
    public async Task get_bar_when_requested()
    {
        var response = await Given.FooServer
            .CreateHttpApiRequest<FooController>(foo=>foo.Get(1),new { version = 1 })
            .GetAsync();

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task post_new_bar()
    {
        var bar = new Bar() { Id = 1 };
        var response = await Given.FooServer
            .CreateHttpApiRequest<FooController>(foo => foo.Post(bar), new { version = 1 })
            .PostAsync();

        response.EnsureSuccessStatusCode();
    }
```

Fíjese como se usa el tipo anónimo para especificar aquellos *tokens* que no pueden ser obtenidos de la propia definición de la acción llamada en nuestro controlador.

# Conclusiones

En este capítulo hemos visto como enfrentarnos al manejo de las diferentes rutas de nuestro controladores en los tests de nuestras HTTP API para simplificar nuestros tests e intentar luchar contra diferentes *code smell* que nos pueden aparecer.

