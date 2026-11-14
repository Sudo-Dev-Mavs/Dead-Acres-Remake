# Dead Acres Remake

Remake en Unity del juego Dead Acres (Glowstick, 2015): granja de día y defensa en primera persona de noche.

- Diseño: `design/gdd-vertical-slice.md` en los archivos del proyecto.
- Unity **6.3 LTS** con URP.

## Puesta en marcha (primera vez)

El repo solo contiene `Assets/_Project`. El resto del proyecto (ProjectSettings, Packages) lo genera Unity:

1. En Unity Hub: **New project → Universal 3D (URP)**, Unity 6.3 LTS, y créalo **en la carpeta de este repo**. Si Hub no te deja usar una carpeta que ya existe, crea el proyecto aparte y copia aquí `Assets/_Project`.
2. En **Project Settings → Editor**, comprueba que estén puestos *Asset Serialization: Force Text* y *Version Control: Visible Meta Files*. Son los valores por defecto en Unity 6.
3. Instala por Package Manager: **Input System** y **Test Framework** (este último ya viene en la plantilla).
4. Menú **Dead Acres → Generar datos por defecto**. Crea en `Assets/_Project/Data/` la configuración de día/noche, las 4 dificultades, los modos Campaña/Endless, la sesión y los canales de eventos.
5. **Window → General → Test Runner → EditMode → Run All**.
6. Haz commit de `Assets/`, `Packages/` y `ProjectSettings/`, incluidos los `.meta`.

## Estructura

```
Assets/_Project/
  Scripts/Runtime/   DeadAcres.Runtime.asmdef
    Core/Events      canales de eventos ScriptableObject (Observer)
    Core/Session     GameSession: dificultad y modo elegidos
    DayNight         DayNightClock (lógica pura) + DayNightCycle (adaptador de escena)
    Difficulty       DifficultyProfile (Easy/Medium/Hard/Hell)
    GameMode         GameModeRules (límites, noches) + PlacementLimiter
    Combat           Health (con multiplicadores de dificultad)
  Scripts/Editor/    generador de datos por defecto
  Tests/EditMode/    tests NUnit de la lógica pura
  Data/              assets generados (ScriptableObjects)
```

## Convenciones

- La lógica de juego vive en clases C# puras (testeables y, en co-op, ejecutables solo en el host). Los MonoBehaviour solo hacen de adaptadores.
- Los datos y el balance van en ScriptableObjects; no se hardcodean valores de diseño.
- No se hace polling en `Update` si existe un evento que lo resuelva.
- `[SerializeField] private` en lugar de campos públicos.
