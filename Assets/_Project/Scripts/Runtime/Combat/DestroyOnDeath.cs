using UnityEngine;

namespace DeadAcres.Combat
{
    /// <summary>
    /// Destruye el objeto cuando su <see cref="Health"/> llega a cero (muros y demás colocables).
    /// Al destruirse, PlacedObject libera su celda y su hueco en el límite del modo.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class DestroyOnDeath : MonoBehaviour
    {
        private Health health;

        private void Awake() => health = GetComponent<Health>();

        private void OnEnable() => health.Died += OnDied;

        private void OnDisable() => health.Died -= OnDied;

        private void OnDied(Health _) => Destroy(gameObject);
    }
}
