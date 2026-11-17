# Mods y estructura de carpetas

## Dónde vive cada cosa

Hay dos mundos, y conviene no mezclarlos:

| | Contenido del juego base | Mods |
|---|---|---|
| Dónde | `Assets/_Project/` | `Assets/StreamingAssets/Mods/<mod>/` y la carpeta `Mods` del usuario |
| Qué es | Assets de Unity (ScriptableObjects, prefabs, escenas) | Archivos sueltos: `.json`, `.png`, `.wav` |
| Quién lo crea | El generador del menú **Dead Acres** y, más adelante, a mano | Cualquiera, sin Unity |
| Cuándo se lee | Al compilar el juego | Al empezar cada partida |

### Juego base (`Assets/_Project/`)

```
Scripts/     código (Runtime, Editor)
Tests/       tests de la lógica pura
Data/        datos generados: ítems, cultivos, tienda, oleadas, modos, dificultades, botín
Prefabs/     Placeables/, Enemies/, Animals/
Scenes/      MainMenu.unity y Greybox.unity
Art/         materiales generados; aquí irán modelos y texturas definitivos
Audio/       sonidos y música (.wav)
```

### Un mod

```
Mods/
  <IdDelMod>/
    mod.json        ficha del mod (nombre, versión, autor)
    Weapons/        armas extra                      ← SE CARGA YA
    Zombies/        zombis nuevos                    ← reservado
    MiniBosses/     mini jefes                       ← reservado
    Bosses/         jefes                            ← reservado
    Maps/           mapas                            ← reservado
    Textures/       texturas, por resolución         ← reservado
    Skins/          aspectos (Weapons, Zombies, Player) ← reservado
    Designs/        temas de la interfaz             ← reservado
    Audio/          sonidos y música                 ← reservado
```

"Reservado" significa que la carpeta tiene su sitio y su formato previsto (cada una lleva un
`LEEME.txt` que lo explica), pero **el juego todavía no la lee**. Hoy un mod solo puede
añadir armas.

## Dónde busca el juego los mods

1. `StreamingAssets/Mods/` — viaja con el juego. En el proyecto es
   `Assets/StreamingAssets/Mods/`; en el juego compilado, `<juego>_Data/StreamingAssets/Mods/`.
2. `Mods/` en la carpeta de datos del usuario (`Application.persistentDataPath`). En Linux:
   `~/.config/unity3d/<compañía>/<juego>/Mods/`. Sirve para instalar mods sin tocar el juego.

Las carpetas cuyo nombre empieza por guion bajo no se cargan. `_Plantilla` es una de ellas:
para probar, cópiala con otro nombre (por ejemplo `MiMod`) y vuelve a empezar una partida.

## Armas extra

Un `.json` por arma dentro de `Weapons/`. Ejemplo (`_Plantilla/Weapons/escopeta_recortada.json`):

```json
{
  "id": "escopeta_recortada",
  "name": "Escopeta recortada",
  "rarity": "rare",
  "price": 260,
  "damage": 15,
  "range": 14,
  "fireInterval": 0.3,
  "automatic": false,
  "pellets": 10,
  "spread": 9,
  "magazine": 2,
  "reloadSeconds": 1.8
}
```

- El arma entra en el surtido de la tienda con la probabilidad de su rareza, como las del juego.
- El precio es el que se escribe: a las armas de mods no se les aplica el recargo de las del juego.
- Por dentro su Id es `mod.<id del mod>.<id del arma>`, así que dos mods no chocan entre sí.
  Es también lo que se escribe en la partida guardada.
- Si un archivo está mal (falta `id` o `name`, daño o precio a cero), se ignora y la consola
  de Unity dice cuál y por qué.
- Si se carga una partida con un arma de un mod que ya no está instalado, esa arma
  desaparece del inventario y la carga avisa en la consola.

## Cómo se añadirá el resto

Cada tipo de contenido nuevo sigue el mismo camino que las armas: una clase de datos con
los campos del `.json` en `Scripts/Runtime/Mods/ModDefinitions.cs`, sus reglas de validación
en `ModRules` (con tests) y un método de carga en `ModContentLoader` que lo registra donde
el juego ya busca ese tipo de cosa.
