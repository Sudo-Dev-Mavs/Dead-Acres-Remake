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

        public void Select(DifficultyProfile selectedDifficulty, GameModeRules selectedMode)
        {
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
