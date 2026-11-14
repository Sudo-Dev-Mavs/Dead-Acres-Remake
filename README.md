# Dead Acres Remake

Remake en Unity del juego Dead Acres (Glowstick, 2015): granja de día y defensa en primera persona de noche.

- Unity **6000.3.25f1** (6.3 LTS) con URP. El repo ya es un proyecto de Unity completo (`Assets/`, `Packages/`, `ProjectSettings/`).
- Referencia de mecánicas observadas en el original: `docs/referencia-original.md`.
- El GDD (`gdd-vertical-slice.md`) todavía no está en el repo; conviene añadirlo en `docs/`.

## Puesta en marcha

1. En Unity Hub: **Add → Add project from disk** y elige la carpeta del repo. Ábrelo con 6000.3.25f1. La primera apertura tarda: Unity genera `Library/` y los `.meta`.
2. Menú **Dead Acres → Crear escena greybox**. Genera los datos en `Assets/_Project/Data/`, los materiales y `Assets/_Project/Scenes/Greybox.unity`, ya cableada.
3. Pulsa **Play**.
4. **Window → General → Test Runner → EditMode → Run All**.
5. Haz commit de lo que Unity haya generado, **incluidos todos los `.meta`**. Los GUID de los `.meta` son los que enlazan escenas y assets con los scripts: deben generarse en una sola máquina y subirse.

### Controles (escena greybox)

| Acción | Tecla |
|---|---|
| Moverse / correr / saltar | WASD / Shift / Espacio |
| Elegir casilla de la hotbar | 1–0 o rueda del ratón |
| Usar lo que llevas (arar con la azada, plantar con semillas) | Clic izquierdo |
| Cosechar | E |
| "Listo": terminar el día | Enter |
| Soltar el cursor (clic para recuperarlo) | Esc |

## Estructura

```
Assets/_Project/
  Scripts/Runtime/   DeadAcres.Runtime.asmdef
    Core/Events      canales de eventos ScriptableObject (Observer)
    Core/Session     GameSession: dificultad y modo elegidos
    DayNight         DayNightClock (lógica pura) + DayNightCycle (adaptador) + hora de reloj + iluminación
    Difficulty       DifficultyProfile (Easy/Medium/Hard/Hell)
    GameMode         GameModeRules, PlacementLimiter (puro) + PlacementLimits (adaptador)
    Combat           Health (con multiplicadores de dificultad)
    Items            ItemDefinition, ItemStack, Inventory (puro)
    Farming          FarmField + FarmCommands (puros), CropDefinition/CropCatalog, FarmArea + FarmAreaView
    Player           input, movimiento, cámara, inventario del jugador, interacción
    UI               HudView
  Scripts/Editor/    generador de datos, generador de escena greybox y de HUD
  Tests/EditMode/    tests NUnit de la lógica pura
  Data/, Scenes/, Art/   generados desde el menú Dead Acres
Assets/Settings/     assets de URP de la plantilla oficial
```

## Convenciones

- La lógica de juego vive en clases C# puras (testeables y, en co-op, ejecutables solo en el host). Los MonoBehaviour solo hacen de adaptadores.
- Los datos y el balance van en ScriptableObjects; no se hardcodean valores de diseño.
- No se hace polling en `Update` si existe un evento que lo resuelva.
- `[SerializeField] private` en lugar de campos públicos.

### Preparado para co-op (sin implementarlo todavía)

- Todo cambio de estado pasa por las clases puras (`FarmField`, `FarmCommands`, `Inventory`, `DayNightClock`). Los componentes del jugador solo expresan intenciones; en co-op esas llamadas serán peticiones al host.
- No hay singletons ni "el jugador": cada jugador tiene su `PlayerInventory` y su `PlayerInteractor`; solo el local tiene `PlayerInputHandler`.
- Ítems y cultivos tienen un `Id` de texto estable. Es lo que se guardará y lo que viajará por red, nunca la referencia al asset.
- Las vistas (`FarmAreaView`, `HudView`) solo escuchan eventos.
