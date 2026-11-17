# Dead Acres Remake

Remake en Unity del juego Dead Acres (Glowstick, 2015): granja de día y defensa en primera persona de noche.

- Unity **6000.3.25f1** (6.3 LTS) con URP. El repo ya es un proyecto de Unity completo (`Assets/`, `Packages/`, `ProjectSettings/`).
- Referencia de mecánicas observadas en el original: `docs/referencia-original.md`.
- El GDD (`gdd-vertical-slice.md`) todavía no está en el repo; conviene añadirlo en `docs/`.

## Puesta en marcha

1. En Unity Hub: **Add → Add project from disk** y elige la carpeta del repo. Ábrelo con 6000.3.25f1. La primera apertura tarda: Unity genera `Library/` y los `.meta`.
2. Menú **Dead Acres → Crear escena greybox**. Genera los datos en `Assets/_Project/Data/`, los materiales, los prefabs y dos escenas ya cableadas en `Assets/_Project/Scenes/`: `MainMenu.unity` y `Greybox.unity` (la de juego, que queda abierta).
3. Pulsa **Play**: arranca siempre en el menú principal, esté abierta la escena que esté. Para probar una escena suelta, desmarca **Dead Acres → Play empieza en el menú principal**.
4. **Window → General → Test Runner → EditMode → Run All**.
5. Haz commit de lo que Unity haya generado, **incluidos todos los `.meta`**. Los GUID de los `.meta` son los que enlazan escenas y assets con los scripts: deben generarse en una sola máquina y subirse.

### Controles

| Acción | Teclado/ratón | Mando |
|---|---|---|
| Moverse / mirar | WASD / ratón | stick izq. / stick der. |
| Saltar / correr | Espacio / Shift | A / L3 |
| Usar lo que llevas: disparar, arar (azada), plantar (semillas), cosechar (hoz dentada), colocar, recoger (martillo), reparar (cinta) o comer | Clic izq. | RT |
| Rotar al colocar | Q / E | cruceta izq./der. |
| Hablar con Jack, recoger huevos y lana, abrir y cerrar puertas | F | X |
| "Listo" (terminar el día) | Enter | cruceta arriba |
| Recargar | R | Y |
| Inventario (mover entre hotbar y mochila) | Tab | Select |
| Hotbar | 1-9 y rueda | LB / RB |
| Pausa | Esc o P | Start |

Estas son las teclas por defecto: todas las acciones de juego se pueden cambiar, para teclado/ratón y para mando, en **Opciones → Controles**.

En el editor, Esc suelta el cursor y un clic en la vista Game lo recupera.

Opciones (en el menú principal y en la pausa): volumen general, de efectos y de música; sensibilidad del ratón y del mando; invertir el eje vertical; campo de visión; pantalla completa, resolución, sincronía vertical y límite de FPS; y la pestaña de controles. Se guardan en las preferencias del jugador, no en la partida.

Menú principal: el panel va a la izquierda y detrás se ve una partida de demostración (caravana, muros, torretas, huerto y zombis que llegan y caen). Tiene tres botones: **Jugar**, **Opciones** y **Salir**. Jugar va por páginas, todas con "Volver": modo (Campaña, Endless, One Life o Cargar partida) → dificultad (Fácil, Media, Difícil, Infierno) → ranura de guardado.

Modos: en **Campaña** se gana al superar la décima noche; **Endless** no tiene final; **One Life** es una campaña en la que el jugador y la caravana tienen 1 de vida (cualquier golpe acaba la partida) y perder borra la partida guardada.

Guardado: hay **5 ranuras en total**, para todos los modos. Al empezar una partida eliges la ranura y, si quieres, le pones nombre; si la ranura ya tenía una partida, el menú avisa de que se sobrescribirá y pide un segundo clic. La partida se guarda sola en su ranura cada amanecer a partir del segundo día (día, oro, inventario, parcelas y cultivos, todo lo colocado, animales, vida, autos ya registrados). "Cargar partida" retoma desde ese amanecer; lo hecho después se pierde. Los archivos son `save_slot_1.json` a `save_slot_5.json` en `Application.persistentDataPath`. No se guardan el estado de las puertas ni el producto a medias de los animales. Si se da Play directamente en la escena de juego (sin pasar por el menú) no hay ranura y no se guarda.

Pausa (Esc o P): detiene el juego entero y permite volver al menú principal.

Sonido: efectos y dos bucles de música (día y noche) provisionales, sintetizados con `Tools/generate_placeholder_audio.py`. Para cambiar uno, sustituye su `.wav` en `Assets/_Project/Audio/` conservando el nombre. Lo que pasa en el mundo suena con posición (zombis que mueren, torretas, TNT, golpes a la caravana): se oye más flojo y hacia su lado cuanto más lejos esté. Lo del propio jugador (disparos, recarga, pasos, avisos) suena sin posición.

Se puede arar, plantar y colocar en todo el suelo, salvo en una franja de 3 bloques alrededor de la caravana (la losa marrón). Empiezas solo con la pistola, la azada, la hoz dentada y unas semillas de fresa: el martillo, los muros y todo lo demás se compra a Jack.

Para cosechar hace falta la hoz dentada: sin ella en la mano no se puede. De noche los cultivos crecen a una décima de la velocidad del día.

De noche llegan zombis desde todas las direcciones. Van a por la caravana, se desvían a por el jugador, los animales y las torretas si los tienen cerca y rompen los muros que les cortan el paso. Se pierde si cae la caravana o el jugador; en Campaña se gana al superar la décima noche. Al amanecer, los zombis que queden se queman.

El día dura 2 minutos. La noche dura como mucho 5: el reloj nunca se detiene a esperar a que mates al último. Si despejas la oleada antes de los 2 minutos, llega un grupo pequeño de refuerzo (hasta dos por noche) y lo que queda de noche pasa al triple de velocidad; si la despejas después, amanece en el acto. Los zombis que sigan vivos al amanecer empiezan a arder: se alejan de la granja y desaparecen al llegar al borde, salvo que te tengan cerca, en cuyo caso te atacan hasta que mueran ellos o tú. Con la partida terminada, Enter la reinicia.

La cripta (el edificio de piedra con luz morada, a unos 50 m de la caravana) suelta zombis fuertes desde la segunda noche: uno más cada noche. Aguantan cuatro veces más que uno normal, pegan más y son más lentos.

Autos abandonados: hay cinco repartidos por el mapa. F los registra una sola vez: oro y tres cosas al azar (semillas, cinta, antorchas, muros, alguna luz). En toda la partida sale como mucho un arma entre los cinco autos, nunca por encima de rareza Raro, y no está asegurada: solo un auto elegido al azar puede tenerla, y le sale más o menos la mitad de las veces.

Armas: empiezas con la pistola; el resto sale en la tienda según su rareza. Hay pistola automática, revólver Magnum .44, Uzi, cinco escopetas (monotiro, de corredera, doble cañón, semiautomática y automática), tres rifles de asalto (AK-47, M4 y SIG XM7), rifle de caza y francotirador pesado. Las automáticas disparan mientras mantienes el clic; las escopetas sueltan varios perdigones en cono. La munición es infinita, pero cada arma tiene su cargador y su tiempo de recarga: se recarga sola al vaciarse, o con R (Y en el mando). El HUD muestra las balas junto al nombre del arma.

Cada noche los zombis tienen un 20 % más de vida y pegan un 15 % más que la primera. Un solo golpe nunca quita más del 99 % de la vida máxima al jugador ni a la caravana.

Defensas: antorchas (luz sin energía), luces portátiles, torretas, espantapájaros con TNT (explota al acercarse un zombi) y muros.

Energía: las luces portátiles y las torretas necesitan una fuente a su alcance (la caravana llega a 6 bloques; un generador, a 9; el generador pequeño, a 5) y a la que le quede capacidad: la caravana aguanta 10 de consumo, cada generador 20 y el pequeño 8. El HUD muestra "Energía usada/disponible"; si no alcanza, lo último que se colocó se queda sin energía (luz roja en la torreta).

Torretas: normal (consume 2), doble cañón (4), escopeta (3, daña a todo lo que tenga delante), lanzallamas (4, muy corto alcance y daño continuo), francotiradora (3, largo alcance), automática (5, mucha cadencia) y eléctrica (6: el rayo salta hasta a 4 zombis más y pierde un 30 % de daño en cada salto). Repararlas con cinta cuesta además algo de oro, más cuanto mejor es la torreta. Al acercarte a una torreta aparece un cartel con su vida, su consumo y si tiene energía.

Al arar, plantar o colocar, el objetivo es el punto del suelo que miras; si miras más lejos de lo que alcanzas (6 m), queda en el borde del alcance en esa dirección. El martillo recoge lo colocado (si está dañado, antes hay que repararlo) y la cinta americana lo repara.

Comida: con fruta, verdura o un huevo en la mano, clic izquierdo te lo comes y recuperas vida (con la vida completa no se gasta).

Animales: empiezas con una oveja y dos gallinas, cada una por su lado a unos 25-30 m de la caravana; Jack vende más. Cuando tienen producto listo les aparece un cubo amarillo encima: F para recoger el huevo o la lana. Son colocables: el martillo los recoge para moverlos. Pasean cerca de donde se colocan sin atravesar vallas ni muros, y de noche los zombis los atacan si pasan cerca, así que conviene hacerles un corral.

Árboles frutales (manzano, naranjo, limonero): se plantan como cualquier semilla, tardan más y dan más fruta.

Más colocables: valla (baja: frena a los zombis y se dispara por encima), puerta (F la abre y la cierra), fogata (luz sin energía, más que la antorcha), pozo y molino (decoración; el molino ocupa 2×2).

Jack está junto a la caravana de día y se va por la noche. Compra la cosecha y cada día trae objetos distintos elegidos al azar (siempre al menos dos tipos de semillas): 4 el primer día, 6 el segundo, 9 el tercero y, desde el cuarto, normalmente 12, aunque hay días flojos con entre 6 y 11. Cada objeto tiene una rareza —Común, Raro, Ultrararo, Épico o Legendario— según cuánto ayuda: cuanto más raro, menos veces aparece, aunque los raros se dejan ver más según pasan los días. Con la tienda abierta el reloj se detiene (Esc o F para cerrar).

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
    Items            ItemDefinition, ItemRarity, ItemStack, Inventory (puro: hotbar 9 + mochila), ItemCatalog (búsqueda por Id)
    Audio            AudioLibrary (clips por función), GameAudio (escucha eventos), WorldSound (3D), VolumeSteps (puro)
    Settings         GameSettings (preferencias), GameInput (acciones y teclas), StepCycle (puro)
    Saving           SaveData (datos planos), SaveMapper + SaveSummary (puros), SaveFile (5 ranuras en JSON),
                     SaveGameController
    Building         PlacementGrid (ocupación, pura), FarmWorld, PlacementController + fantasma,
                     PowerMap (pura) + PowerSource / PowerConsumer / PoweredLight, NoBuildZone, Gate, Spinner
    Animals          ProductionTimer (puro) + Animal
    Loot             LootTable (tirada con semilla), LootContainer (autos abandonados), LootDirector (cupo de armas)
    Defenses         Turret, TntScarecrow
    Farming          FarmField + FarmCommands (puros), CropDefinition/CropCatalog, FarmArea + FarmAreaView
    Economy          Wallet + ShopCommands + ShopStock (puros), ShopCatalog, Shopkeeper (Jack)
    Enemies          WaveRunner + WaveMath (puros), WaveConfig, WaveDirector, ZombieBrain
    Match            MatchState (puro) + MatchDirector: victoria y derrota
    Player           PlayerInputReader (Input System), FirstPersonController, PlayerInventory, PlayerWallet,
                     PlayerInteractor, PlayerWeapon, PlayerBuildTools (martillo, cinta y comer)
    UI               HudView, ShopView, InventoryView, UiModal, UiFactory, ListPaging (puro),
                     MainMenuView, MenuDemoDirector + MenuCameraOrbit (fondo del menú), PauseMenuView,
                     OptionsPanel, MenuUi
  Scripts/Editor/    generador de datos, generador de escena greybox y de HUD
  Tests/EditMode/    tests NUnit de la lógica pura
  Audio/             .wav provisionales
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
