using System;
using DeadAcres.Core.Session;
using UnityEngine;

namespace DeadAcres.Combat
{
    public enum Faction : byte
    {
        /// <summary>Jugador, RV, muros, torretas, generadores, cultivos y animales.</summary>
        PlayerSide = 0,
        Enemy = 1,
    }

    /// <summary>
    /// Vida con los multiplicadores de dificultad aplicados.
    /// Enemigos: se escala la vida máxima. Bando del jugador: se escala el daño recibido.
    /// Compatible con pooling: llamar a <see cref="ResetHealth"/> al reutilizar.
    /// </summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private Faction faction = Faction.PlayerSide;
        [SerializeField, Min(1f)] private float baseMaxHealth = 100f;
        [SerializeField] private GameSession session;

        private float maxHealth;
        private float currentHealth;

        /// <summary>(vida actual, vida máxima). Para barras de vida; sin alocaciones.</summary>
        public event Action<float, float> HealthChanged;

        public event Action<Health> Died;

        public Faction Faction => faction;
        public float Current => currentHealth;
        public float Max => maxHealth;
        public bool IsDead => currentHealth <= 0f;
        public float Normalized => maxHealth > 0f ? currentHealth / maxHealth : 0f;

        private void Awake() => ResetHealth();

        /// <summary>Recalcula la vida máxima con la dificultad actual y llena la barra.</summary>
        /// <param name="extraHealthMultiplier">Escalado adicional (p. ej. crecimiento por noche en Endless).</param>
        public void ResetHealth(float extraHealthMultiplier = 1f)
        {
            float scaled = baseMaxHealth * extraHealthMultiplier;
            if (faction == Faction.Enemy && session != null && session.Difficulty != null)
            {
                scaled = session.Difficulty.ScaleEnemyHealth(scaled);
            }

            maxHealth = scaled;
            currentHealth = scaled;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <returns>Daño realmente aplicado tras la dificultad.</returns>
        public float ApplyDamage(float rawDamage)
        {
            if (IsDead || rawDamage <= 0f)
            {
                return 0f;
            }

            float damage = rawDamage;
            if (faction == Faction.PlayerSide && session != null && session.Difficulty != null)
            {
                damage = session.Difficulty.ScaleDamageTaken(rawDamage);
            }

            damage = Mathf.Min(damage, currentHealth);
            currentHealth -= damage;
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Died?.Invoke(this);
            }

            return damage;
        }

        /// <summary>Curación (cinta americana). No revive objetos destruidos.</summary>
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f || currentHealth >= maxHealth)
            {
                return;
            }

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        internal void Configure(Faction newFaction, float newBaseMaxHealth, GameSession newSession)
        {
            faction = newFaction;
            baseMaxHealth = newBaseMaxHealth;
            session = newSession;
            ResetHealth();
        }
    }
}
