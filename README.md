# Pruebas efectivas de nuestras API HTTP en .NET Core 2.X

Cuando decidimos escribir acerca "Testing efectivo de HTTP API en .NET Core" no teníamos pensado extendernos mucho, es probable que con algunas entradas de blog pudiéramos compartir nuestras ideas de una forma relativamente clara, pero, al final, las ganas de compartir experiencias y diferentes soluciones a problemas comunes en nuestro día a dia parece que ocupa mucho más de lo que nosotros pensamos. Por eso, construímos este pequeño manual, que esperemos sirva para introducir a quien lo lea en como realizar testing funcional de las HTTP API construídas con .NET Core 2.X.


## Introducción

Es probable que usted al leer esto se pregunte : ¿Porqué pruebas funcionales sobre nuestro HTTP API si ya tengo tests unitarios? Pues bien, la respuesta es sencilla, porque son complementarias. Es decir, tener pruebas unitarias en nuestros desarrollos de software, en este caso desarrollo de HTTP API, no impide que hagamos este tipo de pruebas funcionales. Las pruebas unitarias, son aisladas y muy rápidas ( o así deberían ser ) y nos permiten probar diferentes casuísticas de nuestros componentes de forma aislada. Por el contrario, las pruebas funcionales como aquí presentamos no son tan rápidas pero nos permiten probar de forma integrada los diferentes componentes que forma nuestra solución, dándonos ademas un buen indice de cobertura. Hasta hace relativamente poco, ejecutar estas pruebas implicaba un cierto trabajo de despliegue haciendo que las mismas no formaran parte del ciclo normal de un desarrollador ( codigo, construcción y pruebas ) por el setup necesario para ejecutarlas. Con la llegada de TestServer, *recuerde que no solamente está disponible en .NET Core sino tambien en Full FX con Web Api 2.X* ,este proceso se ha simplificado y tenemos ya la posiblidad de correr este tipo de pruebas como si de pruebas unitarias se tratara y por lo tanto incluirlas dentro de ese flujo de desarrollo que todos solmemos seguir.


## Contenido

El contenido de este manual es abierto y trataremos de que esté siempre lo más actualizado posible. Algo que por supuesto no siempre es sencillo por los ciclos de entrega de nuevas características que tenemos en **.NET Core**. En principio, nos basaremos en **.NET Core 2.X**, en concreto con la versione **2.0**, la última versión release cuando este manual se estaba escribiendo.

* [Introducción a TestServer](chapter1.md)
* [Rutas y parámetros](chapter2.md)
* [Autorización](chapter3.md)
* [Trabajando con datos ](chapter4.md)

Cada capítulo lleva aparejado el código completo relativo a la explicación del mismo. Puede encontrarlos dentro de la carpeta Samples en este mismo repositorio.

## Agradecimientos

A todas las personas que han contribuido a la escritura y revision de este manual, en especial a:

1. Unai Zorrilla  Castro
2. Luis Ruiz Pavon
