# Fuentes de arte del reloj

`OriginalCanvasExports` conserva las 39 exportaciones de lienzo completo que
existían antes de la migración. No se usan directamente durante el juego.

`LegacyExports` contiene la versión anterior del arte y se mantiene únicamente
como referencia histórica.

El arte runtime está en `Assets/Wiñaypaq/Art/Clock`. Las piezas, costuras y la
figura completa fueron recortadas de manera determinista, sin modificar sus
píxeles RGBA. Sus offsets de composición se guardan en `clock-layout.json`.

Para trabajo futuro, el archivo maestro de Aseprite debería guardarse en esta
carpeta y versionarse junto con el proyecto.

