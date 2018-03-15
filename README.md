# Pruebas efectivas de nuestras HTTP APIs en .NET Core

Cuando decidimos escribir acerca "Testing efectivo de HTTP API en .NET Core" no teníamos pensado extendernos mucho. Es probable que con algunas entradas de blog hubieramos conseguido compartir nuestras ideas y trasmitir el mensaje de una forma relativamente clara. Pero al final, las ganas de compartir experiencias y diferentes soluciones a problemas comunes en nuestro día a día parece que ocupa mucho más de lo que nosotros pensabamos. Por eso, construimos este pequeño manual, que esperemos os sirva para introduciros en el camino del **testing funcional** de nuestras "HTTP APIs construidas con .NET Core 2.X".

## Introducción

Es probable que usted al leer esto se pregunte, "¿Porqué pruebas funcionales sobre nuestro HTTP API si ya tengo tests unitarios?" Pues bien, la respuesta es sencilla, porque son complementarias. Es decir, tener pruebas unitarias en nuestros desarrollos de software en este caso desarrollo de HTTP API, no impide que hagamos este tipo de pruebas funcionales. Las pruebas unitarias, son aisladas y muy rápidas (o así deberían ser) y nos permiten probar diferentes casuísticas de nuestros componentes de forma aislada. 

Por el contrario, las pruebas funcionales como aquí presentamos no son tan rápidas pero nos permiten probar en conjunto los diferentes componentes que forman nuestra solución, dándonos además un buen índice de cobertura de código. 

Hasta hace relativamente poco, ejecutar estas pruebas implicaba un cierto trabajo de despliegue, haciendo que las mismas no formaran finalmente parte del ciclo normal de un desarrollador (código, construcción y pruebas) por el setup requerido para ejecutarlas. Con la llegada de *TestServer* (*recuerde que no solamente está disponible en .NET Core sino también en Full FX con Web Api 2.X*), este proceso se ha simplificado y tenemos ya la posibilidad de correr este tipo de pruebas como si de pruebas unitarias se tratara y por lo tanto incluirlas dentro de ese flujo de desarrollo que todos solemos seguir.

## Contenido

El contenido de este manual es abierto y trataremos de que esté siempre lo más actualizado posible. Algo que por supuesto no siempre es sencillo por los ciclos de entrega de nuevas características que tenemos en **.NET Core**. En principio, nos basaremos en **.NET Core 2.X**, en concreto con la versión **2.0**, la última versión release cuando este manual se estaba escribiendo.

## Índice en castellano

1. [Introducción a TestServer](chapters/es/chapter1.md)
2. [Rutas y parámetros](chapters/es/chapter2.md)
3. [Autorización](chapters/es/chapter3.md)
4. [Trabajando con datos](chapters/es/chapter4.md)

## English Index

1. [Introduction to TestServer](chapters/en/chapter1.md)


Cada capítulo lleva asociado el código relativo a la explicación del mismo. Puede encontrarlos dentro de la carpeta **samples**.

## Agradecimientos

A todas las personas que han contribuido a la escritura y revisión de este manual:

1. Unai Zorrilla Castro (@unaizorrilla)
2. Luis Ruiz Pavon (@lurumad)
3. Jorge Rodriguez Galán  (@jrgcubano)
