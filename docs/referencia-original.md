# Referencia: lo que se ve del Dead Acres original

Notas tomadas de un gameplay del build de Unity del original (vídeo: https://youtu.be/uWWr1adIXBU, publicado en 2017). Sirven de especificación de partida; no sustituyen al GDD.

## Contexto

- Glowstick Entertainment lo lanzó en Early Access el 24 de noviembre de 2015. Recibió parches al menos hasta septiembre de 2017 (matchmaking, Linux). Después se rebautizó Farmocalypse, se anunció una reescritura en Unreal que no llegó a publicarse y se retiró de Steam.
- Las mecánicas se pueden recrear. El nombre, el personaje Jack, el logo, los modelos y la música son del estudio: si este proyecto se publica necesita nombre y assets propios.

## Mecánicas observadas

**Vista y HUD.** Primera persona, mundo voxel. Vida en %, munición, hotbar de 10 casillas, inventario de 6×5, reloj con día y hora.

**Día.**
- Azada para arar, semillas para plantar. Los cultivos vuelven a dar fruto tras cosechar, sin replantar.
- Cultivos vistos: fresa, papa, girasol, tomate, trigo, sandía. Árboles: manzano, naranjo, limonero.
- Animales: oveja (lana, con herramienta) y gallina (huevos, que además curan).
- Coches abandonados con botín: semillas, un arma mejor, luces portátiles.

**Jack, la tienda.**
- Llega de día y se va al anochecer; avisa cuando se hace tarde.
- Compra la cosecha y vende semillas, defensas y decoración. Su catálogo crece cada día.
- El tiempo se detiene mientras se comercia.

**Noche.**
- Oleadas crecientes de zombis. Van a por la caravana y rompen luces, antorchas y árboles por el camino. Atacan a los animales (hay oveja zombi).
- Una cripta en el mapa suelta zombis más fuertes.
- Cambia la música al anochecer.

**Defensas y objetos.**
- Antorchas, fogata, luces que solo funcionan cerca de una fuente de energía, generador.
- Torretas automáticas que giran y disparan solas.
- Espantapájaros con TNT, muros de madera, valla, puerta.
- Colocación con rotación (Q/E), reparación, herramienta para quitar lo propio, cofre de almacenamiento.
- Decoración sin función: molino, pozo, flores.

**Otros.** Arma con cargador y, al parecer, munición infinita. La comida cura. Los ítems muestran categoría y nivel requerido. Se puede guardar la partida.

## Lo que no se sabe

Casi todos los números. Solo se oyen tres precios: muro 5, fogata 100, torreta 200 de oro. Tiempos de crecimiento, rendimientos, vida de enemigos y fórmula de oleadas hay que definirlos y balancearlos aquí. Los valores que hay ahora en `DefaultDataGenerator` son provisionales.
