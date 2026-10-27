# Handoff de Wiñaypaq

Actualizado: 25 de octubre de 2026

## Resumen ejecutivo

Wiñaypaq es un prototipo 2D en Unity de un rompecabezas textil contrarreloj.
El jugador arrastra fragmentos desde sus posiciones de origen hasta el lugar que
les corresponde en una figura. El recorrido del sol representa el paso del día y
la ira de Inti amenaza periódicamente piezas que todavía no fueron colocadas.

El proyecto ya pasó por una reorganización técnica y de arte. La escena actual
es funcional, compila y sirve como prueba de concepto, pero todavía no representa
la dificultad, claridad ni duración deseadas para el juego final. El siguiente
objetivo no debería ser producir muchos niveles, sino construir una rebanada
vertical: un único rompecabezas más elaborado, comprensible y cercano a la
experiencia final.

## Estado técnico actual

- Versión de Unity: `6000.3.19f1`.
- Renderizado: Universal Render Pipeline.
- Escena principal: `Assets/Wiñaypaq/Scenes/ClockGameplay.unity`.
- Es la única escena habilitada en Build Settings y ocupa el índice `0`.
- El proyecto produce una build de Windows funcional.
- Existe una prueba de humo ejecutada sin excepciones.
- No hay un repositorio Git en la carpeta actual. Se utilizaba Unity Version
  Control, pero su cliente no estaba configurado en la máquina durante la
  migración. Antes de seguir creciendo, conviene establecer control de versiones.

Build de validación existente:

`Builds/Validation/Winaypaq.exe`

Respaldo anterior a la reorganización:

`ProjectBackups/pre-production-cleanup-20261012.zip`

Informe de validación:

`ProjectBackups/validation-report.txt`

## Organización del proyecto

- `Assets/Wiñaypaq/Art`: recursos optimizados que utiliza Unity.
- `Assets/Wiñaypaq/Prefabs`: prefabs reutilizables.
- `Assets/Wiñaypaq/Scenes`: escenas jugables.
- `Assets/Wiñaypaq/Scripts`: código de ejecución y herramientas de editor.
- `Assets/Wiñaypaq/Settings`: ajustes de URP.
- `ArtSource`: exportaciones originales y material que Unity no debe importar.
- `ProjectBackups`: respaldos locales ignorados por el control de versiones.

La ilustración original se dividió en sprites recortados sin alterar sus píxeles.
Las posiciones de reconstrucción están registradas en:

`Assets/Wiñaypaq/Art/Clock/clock-layout.json`

El atlas de sprites es:

`Assets/Wiñaypaq/Art/Clock/clock-gameplay.spriteatlas`

## Sistemas existentes

### Interacción de piezas

`ClockJamPiece.cs` permite seleccionar y arrastrar una pieza. Al soltarla:

- se coloca si el `Transform` de la pieza está dentro de `snapRadius` respecto a
  `snapTarget`;
- si falla, vuelve suavemente a su posición de origen;
- una pieza correctamente colocada desactiva su colisionador;
- cada pieza puede atravesar los estados `Idle`, `Targeted`, `Burning`,
  `Dragging` y `Placed`.

El retorno automático ya coincide parcialmente con el juego de referencia. La
limitación importante es que actualmente se compara el origen completo de la
pieza, no un punto guía independiente.

### Rompecabezas y costuras

`ClockJamPuzzleManager.cs` registra las piezas, cuenta las colocadas y completa
el nivel cuando todas están en su destino. Las costuras aparecen cuando las dos
piezas vecinas asociadas ya fueron colocadas.

### Ciclo solar

`ClockJamSolarClock.cs` simula un día de 06:00 a 18:00. La duración actual es de
180 segundos. Emite eventos continuos de tiempo y eventos al cruzar cada hora.

`SolarPathController.cs` mueve el sol mediante puntos de control y una curva.
`ShadowSweepController.cs` orienta la sombra a lo largo del día.

### Ataque y quema

`ClockJamWrathController.cs` elige al azar una pieza disponible cada hora. La
pieza recibe una advertencia; si el jugador la toma durante esa advertencia,
interrumpe el ataque. Si no lo hace, la pieza se quema, desaparece y reaparece en
su origen después de un tiempo.

Este sistema funciona técnicamente, pero debe considerarse provisional. En su
estado actual no comunica con suficiente claridad:

- por qué esa pieza fue elegida;
- qué debe hacer el jugador para salvarla;
- cuánto tiempo queda;
- qué consecuencia estratégica tiene dejarla quemarse;
- cómo se relaciona la amenaza con la posición del sol.

### Fin y reinicio

`ClockJamLevelController.cs` detiene el reloj al resolver el rompecabezas,
reemplaza las piezas por la figura completa y reinicia el nivel cuando termina el
día.

## Herramientas existentes

En el menú `Tools > Clock Jam` existen utilidades de migración, construcción de
escenario y validación. La opción `Validate Migrated Project` comprueba:

- fidelidad RGBA de los recortes;
- cantidad y referencias de piezas y costuras;
- prefab y material compartido;
- posiciones reconstruidas;
- pivotes del sol y la sombra;
- scripts faltantes;
- escena de Build Settings;
- tamaño aproximado de la escena.

La migración ya fue ejecutada. No debe repetirse sin un respaldo, porque está
pensada para reconstruir la organización actual a partir de las fuentes
originales.

## Evaluación del prototipo

El prototipo cumple su función: demuestra que se puede arrastrar, encajar,
reconstruir la figura, revelar costuras y competir contra un ciclo solar. No es
necesario convertir esta escena en contenido final.

Todavía no valida:

- que el rompecabezas mantenga interés durante varios minutos;
- que piezas más irregulares sean cómodas de manipular;
- que la dificultad crezca de forma justa;
- que el jugador entienda la quema sin explicación externa;
- que crear nuevos rompecabezas sea suficientemente rápido.

## Hallazgos del juego de referencia

- Cada pieza posee un pequeño círculo o punto guía.
- El encaje depende de hacer coincidir ese punto con un destino específico.
- Si el punto no coincide, la pieza regresa a su lugar inicial.
- Los rompecabezas de referencia tienen bastantes más piezas y requieren varios
  minutos.
- Los cortes siguen regiones y cambios de color, por lo que la dirección artística
  actual va por buen camino.

Estos hallazgos sugieren mejorar el sistema de autoría y la interacción antes de
producir más arte.

## Mejora prioritaria 1: puntos de encaje explícitos

Cada pieza debería tener dos referencias distintas:

1. `PieceAnchor`: punto local colocado sobre el pequeño círculo de la pieza.
2. `TargetAnchor`: punto correspondiente en el tablero.

Al soltar la pieza se debe medir la distancia mundial entre ambos puntos, no la
distancia entre el origen de la pieza y el destino. Si están dentro de la
tolerancia, la pieza se desplaza por la diferencia entre los dos puntos para que
coincidan exactamente. Si no, retorna al origen.

Mejoras asociadas:

- visualizar el radio de encaje con gizmos solo en el editor;
- permitir ajustar el ancla con una manija en la vista de escena;
- agregar una animación corta, sonido y destello al encajar;
- dar una señal diferente al fallo antes del retorno;
- mantener el círculo visible mientras se arrastra, si forma parte del lenguaje
  visual del juego;
- evaluar soporte táctil y prioridad de selección cuando las piezas se solapan.

## Mejora prioritaria 2: generador de rompecabezas por máscara

No conviene recortar y posicionar manualmente decenas de piezas por nivel. El
flujo recomendado es crear un importador dentro de Unity.

### Entradas de arte

Por cada rompecabezas:

- `illustration.png`: imagen completa terminada;
- `piece-mask.png`: imagen del mismo tamaño, sin antialiasing, donde cada pieza
  está pintada con un color plano único;
- archivo maestro `.aseprite` fuera de `Assets`, conservando capas y fuentes;
- opcionalmente, una referencia visual del resultado armado.

### Trabajo del generador

La herramienta debería:

1. leer todos los colores únicos de la máscara;
2. encontrar la región de cada color;
3. recortar la pieza conservando transparencia y desplazamiento original;
4. crear o actualizar el sprite;
5. generar un `PolygonCollider2D` simplificado;
6. crear el punto de destino y un ancla inicial editable;
7. instanciar el prefab de pieza;
8. distribuir las posiciones de origen;
9. guardar los datos en un `PuzzleDefinition`;
10. poder regenerar el nivel sin perder ajustes manuales de anclas y dificultad.

Un script de Aseprite puede ayudar a exportar las capas, pero Unity es el mejor
lugar para la herramienta principal porque conoce los prefabs, colisionadores,
destinos y datos jugables. La automatización debe eliminar el trabajo mecánico,
no sustituir las decisiones artísticas sobre los cortes.

### Datos recomendados

Crear un `ScriptableObject` llamado, por ejemplo, `PuzzleDefinition`, con:

- identificador y nombre del nivel;
- ilustración y máscara fuente;
- datos por pieza: color ID, sprite, ancla local, destino, origen y rotación;
- tolerancia de encaje;
- relaciones de vecindad/costuras;
- duración del día;
- parámetros de peligro y dificultad.

Esto evitará que toda la configuración quede incrustada en una escena grande y
permitirá cargar distintos rompecabezas con los mismos sistemas.

## Mejora prioritaria 3: definir la quema

Antes de pulir partículas o animaciones, debe decidirse qué aporta esta mecánica.
Actualmente actúa como una demora temporal aleatoria. La siguiente versión debe
darle al jugador información y una decisión clara.

Tres direcciones posibles, que no deberían mezclarse sin probarlas por separado:

### Rescate bajo presión

Inti señala una pieza y el jugador debe tomarla o llevarla a una zona segura
antes de que termine una cuenta regresiva. Es la dirección más cercana al código
actual.

### Riesgo predecible por el sol

La sombra o un rayo recorre el tablero y amenaza físicamente las piezas que toca.
El peligro deja de ser aleatorio y el jugador puede anticiparlo observando el
reloj solar.

### Quema deliberada

El jugador decide sacrificar o transformar una pieza para revelar una pista,
cambiar su forma o desbloquear otra región. Esta opción convierte la quema en una
herramienta, pero requiere rediseñar el ciclo principal.

Para cualquiera de ellas hacen falta:

- anticipación visual inequívoca;
- señal sonora;
- indicador del tiempo restante;
- una acción de respuesta fácil de descubrir;
- una consecuencia que afecte decisiones, no solo haga esperar;
- introducción gradual antes de combinarla con rompecabezas difíciles.

## Diseño de dificultad

La dificultad puede controlarse con variables independientes:

- cantidad de piezas;
- irregularidad y tamaño de las piezas;
- similitud de colores o texturas;
- distancia entre origen y destino;
- solapamiento en la zona inicial;
- rotación inicial;
- visibilidad del punto guía;
- radio de encaje;
- presión del tiempo;
- frecuencia y severidad de la quema.

No conviene aumentar todas a la vez. El primer rompecabezas complejo debería usar
formas más interesantes y más piezas, pero conservar anclas visibles, rotación
simple y una amenaza tolerante.

## Próxima meta: rebanada vertical

Crear un solo nivel representativo con estas condiciones:

- entre 12 y 20 piezas;
- duración objetivo de 3 a 5 minutos para un jugador nuevo;
- cortes siguiendo formas y regiones de color;
- puntos de encaje independientes;
- retorno automático al fallar;
- inicio, progreso, victoria y reinicio completos;
- una versión comprensible de la quema;
- sonido y feedback básico, no necesariamente arte final;
- jugable con ratón y con entrada táctil simulada o real.

No crear un segundo nivel hasta haber observado a varias personas jugar este sin
recibir instrucciones.

## Hoja de ruta recomendada

### Fase 0 — Decisiones de diseño

- Escribir el ciclo jugable en una página.
- Elegir una sola dirección para la quema.
- Definir condición de victoria, fracaso y reinicio.
- Definir qué representa el paso del día en la narrativa.

### Fase 1 — Interacción fundamental

- Implementar `PieceAnchor` y `TargetAnchor`.
- Mejorar selección, arrastre, retorno y feedback.
- Probar piezas grandes, pequeñas, cóncavas y superpuestas.
- Separar parámetros de diseño de los detalles visuales.

### Fase 2 — Herramienta de autoría

- Crear `PuzzleDefinition`.
- Crear importador de ilustración más máscara.
- Generar sprites, destinos, colisionadores y prefabs.
- Añadir una ventana de validación del nivel.
- Documentar el formato de entrega para arte.

### Fase 3 — Rebanada vertical

- Dibujar y generar un rompecabezas de 12–20 piezas.
- Integrar la versión elegida de la quema.
- Añadir tutorial no verbal, sonido y final de nivel.
- Hacer una build distribuible.

### Fase 4 — Pruebas

Registrar al menos:

- tiempo total de resolución;
- número de intentos fallidos por pieza;
- piezas que nadie identifica;
- momento en que el jugador comprende la quema;
- frecuencia con la que la amenaza se siente injusta;
- abandonos o momentos de frustración.

### Fase 5 — Producción

Solo después de validar la rebanada vertical:

- establecer progresión de niveles;
- producir más ilustraciones;
- añadir guardado y selección de nivel;
- optimizar para la plataforma objetivo;
- reemplazar efectos temporales por presentación final.

## Criterios de aceptación inmediatos

El siguiente hito puede considerarse terminado cuando:

- una pieza se encaja mediante su ancla, incluso si su pivote está lejos;
- una pieza incorrectamente soltada siempre retorna sin quedar bloqueada;
- un artista puede generar un rompecabezas nuevo a partir de una ilustración y
  una máscara sin recortar manualmente cada PNG;
- regenerar el contenido no destruye ajustes manuales;
- el jugador entiende qué pieza está en peligro y qué acción debe realizar;
- una persona nueva completa el nivel sin explicación oral;
- la validación y una build limpia terminan sin errores.

## Riesgos y deuda pendiente

- Falta control de versiones operativo.
- La lógica de gameplay usa referencias públicas directas y datos incrustados en
  la escena; será difícil escalar a muchos niveles sin `PuzzleDefinition`.
- La elección aleatoria de la pieza amenazada puede sentirse injusta.
- El encaje actual usa el pivote de la pieza y no el círculo guía observado.
- Los colisionadores deben verificarse con piezas complejas y entrada táctil.
- No hay pruebas automáticas de gameplay.
- No hay sistema de guardado, selección de nivel ni accesibilidad.
- Los parámetros actuales son valores de prototipo y no están balanceados.

## Primera tarea recomendada al retomar

Implementar y probar el sistema `PieceAnchor`/`TargetAnchor` en el nivel actual,
sin cambiar todavía el arte. Después crear el esqueleto de `PuzzleDefinition` y
un generador mínimo que procese una máscara de tres o cuatro colores. Este camino
valida el flujo antes de invertir tiempo en un rompecabezas grande.

## Comandos y comprobaciones de entrega

1. Abrir `Assets/Wiñaypaq/Scenes/ClockGameplay.unity`.
2. Ejecutar `Tools > Clock Jam > Validate Migrated Project`.
3. Entrar en Play Mode y comprobar arrastre, retorno, quema, victoria y reinicio.
4. Crear una build de desarrollo para la plataforma objetivo.
5. Revisar la consola y no aceptar errores ni referencias faltantes.

