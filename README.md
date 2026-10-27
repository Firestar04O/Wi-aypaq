# Wiñaypaq

Proyecto 2D desarrollado con Unity `6000.3.19f1` y Universal Render Pipeline.

El estado completo, las decisiones pendientes y la hoja de ruta se encuentran
en [`HANDOFF.md`](HANDOFF.md).

## Abrir el juego

La escena principal es:

`Assets/Wiñaypaq/Scenes/ClockGameplay.unity`

Es la única escena habilitada en Build Settings y ocupa el índice `0`.

## Estructura

- `Assets/Wiñaypaq/Art`: arte importado y optimizado para runtime.
- `Assets/Wiñaypaq/Prefabs`: prefabs reutilizables del juego.
- `Assets/Wiñaypaq/Scenes`: escenas jugables.
- `Assets/Wiñaypaq/Scripts`: código runtime y herramientas de editor.
- `Assets/Wiñaypaq/Settings`: configuración de URP.
- `ArtSource`: exportaciones fuente y material histórico que Unity no importa.
- `ProjectBackups`: copias locales recuperables; esta carpeta está ignorada por Unity VCS.

## Flujo de arte

1. Conservar un archivo maestro `.aseprite` con capas nombradas.
2. Exportar fondos de lienzo completo y elementos interactivos recortados.
3. Usar nombres estables como `piece-01.png` y `seam-01-02.png`.
4. Mantener filtro Point, transparencia y 100 píxeles por unidad.
5. Dejar que `clock-gameplay.spriteatlas` realice el empaquetado final.

La posición original de los sprites migrados está registrada en
`Assets/Wiñaypaq/Art/Clock/clock-layout.json`.

## Validación

Desde Unity puede ejecutarse:

`Tools > Clock Jam > Validate Migrated Project`

La validación comprueba la fidelidad RGBA de los recortes, los prefabs, las
costuras, los materiales compartidos, los scripts y Build Settings.

