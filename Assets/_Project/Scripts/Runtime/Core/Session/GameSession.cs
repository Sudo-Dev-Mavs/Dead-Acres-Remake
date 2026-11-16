using DeadAcres.Difficulty;
using DeadAcres.GameMode;
using UnityEngine;

namespace DeadAcres.Core.Session
{
    /// <summary>
    /// Selección de la partida actual (dificultad + modo), escrita por el menú y leída por los sistemas.
    /// Es estado de runtime: no se guarda en el asset entre sesiones de Play.
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Session/Game Session", fileName = "GameSession")]
    public sealed class GameSession : ScriptableObject
    {
        [Tooltip("Se usan si el menú aún no eligió nada (p. ej. al dar Play directamente en la escena de juego).")]
        [SerializeField] private DifficultyProfile defaultDifficulty;
        [SerializeField] private GameModeRules defaultMode;

        [System.NonSerialized] private DifficultyProfile difficulty;
        [System.NonSerialized] private GameModeRules mode;

        public DifficultyProfile Difficulty => difficulty ? difficulty : defaultDifficulty;
        public GameModeRules Mode => mode ? mode : defaultMode;

        /// <summary>
        /// El menú lo pone a true al elegir "Continuar": la escena de juego carga la partida
        /// guardada al arrancar y lo vuelve a apagar.
        /// </summary>
        public bool ContinueFromSave { get; set; }

        /// <summary>
        /// Ranura (0 a 4) en la que guarda esta partida. -1: sin ranura (al dar Play directamente
        /// en la escena de juego), y entonces no se guarda.
        /// </summary>
        public int SaveSlot { get; set; } = -1;

        /// <summary>Nombre que el jugador le puso a la partida.</summary>
        public string SaveName { get; set; }

        public void Select(DifficultyProfile selectedDifficulty, GameModeRules selectedMode)
        {
            // La elección vive solo en memoria: en el juego compilado no debe descargarse este
            // asset al cambiar de escena.
            if (!Application.isEditor) hideFlags |= HideFlags.DontUnloadUnusedAsset;

            difficulty = selectedDifficulty;
            mode = selectedMode;
        }

        public void Clear()
        {
            difficulty = null;
            mode = null;
        }

        internal void ConfigureDefaults(DifficultyProfile difficultyProfile, GameModeRules rules)
        {
            defaultDifficulty = difficultyProfile;
            defaultMode = rules;
        }
    }
}
