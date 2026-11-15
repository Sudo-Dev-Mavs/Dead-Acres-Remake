using System;
using DeadAcres.DayNight;

namespace DeadAcres.Enemies
{
    /// <summary>
    /// Ritmo de una oleada en C# puro: reparte las apariciones a lo largo de una ventana de tiempo,
    /// sabe cuándo la oleada está despejada y admite grupos de refuerzo si el jugador la limpia
    /// pronto. No crea enemigos: dice cuántos tocan en cada tick. En co-op vive solo en el host.
    /// </summary>
    public sealed class WaveRunner : IWaveStatus
    {
        // Tanda en curso: la oleada principal o un grupo de refuerzo.
        private int batchTotal;
        private int batchSpawned;
        private float batchElapsed;
        private float batchDelay;
        private float batchInterval;

        public int Total { get; private set; }
        public int Spawned { get; private set; }
        public int Alive { get; private set; }

        /// <summary>Grupos de refuerzo enviados esta noche.</summary>
        public int ReinforcementGroups { get; private set; }

        /// <summary>True desde <see cref="Begin"/> hasta que amanece (<see cref="Abort"/>).</summary>
        public bool IsActive { get; private set; }

        /// <summary>Sin oleada en curso también cuenta como despejada: la noche puede terminar.</summary>
        public bool IsWaveCleared => !IsActive || IsFieldEmpty;

        /// <summary>
        /// Mayor que 1 desde que llegan los primeros refuerzos: lo que queda de noche hasta su
        /// duración mínima pasa más rápido.
        /// </summary>
        public float NightTimeScale { get; private set; } = 1f;

        /// <summary>Ya apareció todo lo previsto y no queda ninguno vivo.</summary>
        private bool IsFieldEmpty => Spawned >= Total && Alive == 0;

        public void Begin(int totalEnemies, float delayBeforeFirst, float spawnWindow)
        {
            Total = Math.Max(0, totalEnemies);
            Spawned = 0;
            Alive = 0;
            ReinforcementGroups = 0;
            NightTimeScale = 1f;
            IsActive = Total > 0;
            StartBatch(Total, delayBeforeFirst, spawnWindow);
        }

        /// <summary>
        /// Añade un grupo de refuerzo y acelera el resto de la noche. Solo se acepta con la oleada
        /// en curso ya despejada.
        /// </summary>
        public bool AddReinforcements(int count, float spawnWindow, float nightTimeScale)
        {
            if (!IsActive || count <= 0 || !IsFieldEmpty)
            {
                return false;
            }

            Total += count;
            ReinforcementGroups++;
            NightTimeScale = Math.Max(1f, nightTimeScale);
            StartBatch(count, 0f, spawnWindow);
            return true;
        }

        /// <returns>Cuántos enemigos deben aparecer ahora.</returns>
        public int Tick(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f || batchSpawned >= batchTotal)
            {
                return 0;
            }

            batchElapsed += deltaTime;
            float sinceFirst = batchElapsed - batchDelay;
            if (sinceFirst < 0f)
            {
                return 0;
            }

            int due = batchInterval > 0f
                ? Math.Min(batchTotal, 1 + (int)(sinceFirst / batchInterval))
                : batchTotal;
            int toSpawn = due - batchSpawned;
            if (toSpawn <= 0)
            {
                return 0;
            }

            batchSpawned += toSpawn;
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
        }

        /// <summary>Amanece: se acaba la oleada con lo que quedara.</summary>
        public void Abort()
        {
            IsActive = false;
            Alive = 0;
            NightTimeScale = 1f;
        }

        private void StartBatch(int count, float delayBeforeFirst, float spawnWindow)
        {
            batchTotal = count;
            batchSpawned = 0;
            batchElapsed = 0f;
            batchDelay = Math.Max(0f, delayBeforeFirst);

            // El primero sale al acabar la espera y el último al final de la ventana.
            batchInterval = count > 1 ? Math.Max(0f, spawnWindow) / (count - 1) : 0f;
        }
    }
}
