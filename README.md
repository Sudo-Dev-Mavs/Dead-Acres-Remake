# Dead Acres Remake

Remake en Unity del juego Dead Acres (Glowstick, 2015): granja de día y defensa en primera persona de noche.

- Unity **6000.3.25f1** (6.3 LTS) con URP. El repo ya es un proyecto de Unity completo (`Assets/`, `Packages/`, `ProjectSettings/`).
- Referencia de mecánicas observadas en el original: `docs/referencia-original.md`.
- El GDD (`gdd-vertical-slice.md`) todavía no está en el repo; conviene añadirlo en `docs/`.

## Puesta en marcha

1. En Unity Hub: **Add → Add project from disk** y elige la carpeta del repo. Ábrelo con 6000.3.25f1. La primera apertura tarda: Unity genera `Library/` y los `.meta`.
2. Menú **Dead Acres → Crear escena greybox**. Genera los datos en `Assets/_Project/Data/`, los materiales, un prefab de muro y `Assets/_Project/Scenes/Greybox.unity`, ya cableada.
3. Pulsa **Play**.
4. **Window → General → Test Runner → EditMode → Run All**.
5. Haz commit de lo que Unity haya generado, **incluidos todos los `.meta`**. Los GUID de los `.meta` son los que enlazan escenas y assets con los scripts: deben generarse en una sola máquina y subirse.

### Controles

| Acción | Teclado/ratón | Mando |
|---|---|---|
| Moverse / mirar | WASD / ratón | stick izq. / stick der. |
| Saltar / correr | Espacio / Shift | A / L3 |
| Usar lo que llevas: disparar, arar, plantar o colocar | Clic izq. | RT |
| Rotar al colocar | Q / E | cruceta izq./der. |
| Cosechar / hablar con Jack | F | X |
| "Listo" (terminar el día) | R | Start |
| Hotbar | 1-9 y rueda | LB / RB |

En el editor, Esc suelta el cursor y un clic en la vista Game lo recupera.

De noche llegan zombis desde todas las direcciones. Van a por la caravana, persiguen al jugador si lo tienen cerca y rompen los muros que les cortan el paso. Se pierde si cae la caravana o el jugador; en Campaña se gana al superar la quinta noche. Al amanecer, los zombis que queden se queman. Con la partida terminada, Enter la reinicia.

Jack está junto a la caravana de día y se va por la noche. Compra la cosecha y vende semillas y muros; su catálogo crece cada día. Con la tienda abierta el reloj se detiene (Esc o F para cerrar).

## Estructura

```
Assets/_Project/
  Scripts/Runtime/   DeadAcres.Runtime.asmdef
    Core/Events      canales de eventos ScriptableObject (Observer)
    Core/Session     GameSession: dificultad y modo elegidos
    DayNight         DayNightClock (lógica pura) + DayNightCycle (adaptador) + hora de reloj + iluminación
    Difficulty       DifficultyProfile (Easy/Medium/Hard/Hell)
    GameMode         GameModeRules (límites, noches) + PlacementLimiter
    Combat           Health (con multiplicadores de dificultad), DestroyOnDeath
    Items            ItemDefinition, ItemStack, Inventory (puro: hotbar 9 + mochila)
    Building         PlacementGrid (ocupación, pura), FarmWorld, PlacementController + fantasma
    Farming          FarmField + FarmCommands (puros), CropDefinition/CropCatalog, FarmArea + FarmAreaView
    Economy          Wallet + ShopCommands (puros), ShopCatalog, Shopkeeper (Jack)
    Enemies          WaveRunner + WaveMath (puros), WaveConfig, WaveDirector, ZombieBrain
    Match            MatchState (puro) + MatchDirector: victoria y derrota
    Player           PlayerInputReader (Input System), FirstPersonController, PlayerInventory, PlayerWallet,
                     PlayerInteractor, PlayerWeapon
    UI               HudView, ShopView, UiFactory
  Scripts/Editor/    generador de datos, generador de escena greybox y de HUD
  Tests/EditMode/    tests NUnit de la lógica pura
  Data/, Scenes/, Art/, Prefabs/   generados desde el menú Dead Acres
Assets/Settings/     assets de URP de la plantilla oficial
```

## Convenciones

- La lógica de juego vive en clases C# puras (testeables y, en co-op, ejecutables solo en el host). Los MonoBehaviour solo hacen de adaptadores.
- Los datos y el balance van en ScriptableObjects; no se hardcodean valores de diseño.
- No se hace polling en `Update` si existe un evento que lo resuelva.
- `[SerializeField] private` en lugar de campos públicos.

### Preparado para co-op (sin implementarlo todavía)

- Todo cambio de estado pasa por las clases puras (`FarmField`, `FarmCommands`, `Inventory`, `DayNightClock`). Los componentes del jugador solo expresan intenciones; en co-op esas llamadas serán peticiones al host.
- No hay singletons ni "el jugador": cada jugador tiene su `PlayerInventory` y su `PlayerInteractor`; solo el local tiene `PlayerInputReader`.
- Cultivos y objetos comparten una sola rejilla (`FarmWorld.Grid`) y un solo contador de límites.
- Ítems y cultivos tienen un `Id` de texto estable. Es lo que se guardará y lo que viajará por red, nunca la referencia al asset.
- Las vistas (`FarmAreaView`, `HudView`) solo escuchan eventos.
