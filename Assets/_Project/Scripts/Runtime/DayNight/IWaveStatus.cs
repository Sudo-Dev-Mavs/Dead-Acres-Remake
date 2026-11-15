namespace DeadAcres.DayNight
{
    /// <summary>
    /// Lo que el reloj necesita saber del sistema de oleadas para cerrar la noche.
    /// El WaveDirector lo implementará; mientras no exista se usa <see cref="NullWaveStatus"/>.
    /// </summary>
    public interface IWaveStatus
    {
        /// <summary>True cuando la oleada de esta noche terminó de spawnear y no queda ningún enemigo vivo.</summary>
        bool IsWaveCleared { get; }

        /// <summary>
        /// Velocidad a la que pasa la noche hasta su duración mínima. 1 = normal; mayor que 1
        /// cuando el jugador despejó pronto la oleada y lo que queda debe pasar rápido.
        /// </summary>
        float NightTimeScale { get; }
    }

    /// <summary>Sin sistema de oleadas: la noche dura exactamente el mínimo.</summary>
    public sealed class NullWaveStatus : IWaveStatus
    {
        public static readonly NullWaveStatus Instance = new NullWaveStatus();

        private NullWaveStatus()
        {
        }

        public bool IsWaveCleared => true;

        public float NightTimeScale => 1f;
    }
}
