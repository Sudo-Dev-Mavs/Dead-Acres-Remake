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
    }

    /// <summary>Sin sistema de oleadas: la noche dura exactamente el mínimo.</summary>
    public sealed class NullWaveStatus : IWaveStatus
    {
        public static readonly NullWaveStatus Instance = new NullWaveStatus();

        private NullWaveStatus()
        {
        }

        public bool IsWaveCleared => true;
    }
}
