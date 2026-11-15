using System;
using DeadAcres.DayNight;

namespace DeadAcres.Enemies
{
    /// <summary>
    /// Ritmo de una oleada en C# puro: reparte las apariciones a lo largo de una ventana de tiempo
    /// y sabe cuándo la oleada está despejada. No crea enemigos: dice cuántos tocan en cada tick.
    /// En co-op vive solo en el host.
    /// </summary>
    public sealed class WaveRunner : IWaveStatus
    {
        private float elapsed;
        private float initialDelay;
        private float interval;

        public int Total { get; private set; }
        public int Spawned { get; private set; }
        public int Alive { get; private set; }

        /// <summary>True desde <see cref="Begin"/> hasta que se despeja o se aborta.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Sin oleada en curso también cuenta como despejada: la noche puede terminar.</summary>
        public bool IsWaveCleared => !IsActive || (Spawned >= Total && Alive == 0);

        public void Begin(int totalEnemies, float delayBeforeFirst, float spawnWindow)
        {
            Total = Math.Max(0, totalEnemies);
            Spawned = 0;
            Alive = 0;
            elapsed = 0f;
            initialDelay = Math.Max(0f, delayBeforeFirst);

            // El primero sale al acabar la espera y el último al final de la ventana.
            interval = Total > 1 ? Math.Max(0f, spawnWindow) / (Total - 1) : 0f;
            IsActive = Total > 0;
        }

        /// <returns>Cuántos enemigos deben aparecer ahora.</returns>
        public int Tick(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f || Spawned >= Total)
            {
                return 0;
            }

            elapsed += deltaTime;
            float sinceFirst = elapsed - initialDelay;
            if (sinceFirst < 0f)
            {
                return 0;
            }

            int due = interval > 0f ? Math.Min(Total, 1 + (int)(sinceFirst / interval)) : Total;
            int toSpawn = due - Spawned;
            if (toSpawn <= 0)
            {
                return 0;
            }

            Spawned += toSpawn;
            Alive += toSpawn;
            return toSpawn;
        }

        /// <summary>Un enemigo de la oleada murió o se retiró.</summary>
        public void NotifyEnemyRemoved()
        {
            if (Alive > 0)
            {
                Alive--;
            }

            if (IsActive && Spawned >= Total && Alive == 0)
            {
                IsActive = false;
            }
        }

        /// <summary>Amanece: se acaba la oleada con lo que quedara.</summary>
        public void Abort()
        {
            IsActive = false;
            Alive = 0;
        }
    }
}
