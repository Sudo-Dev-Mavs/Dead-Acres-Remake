using System;
using UnityEngine;

namespace DeadAcres.Core.Events
{
    /// <summary>
    /// Canal de eventos basado en ScriptableObject (Observer desacoplado).
    /// Emisor y oyentes solo comparten el asset, no se referencian entre sí.
    /// </summary>
    /// <remarks>
    /// Los oyentes deben suscribirse en OnEnable y desuscribirse en OnDisable.
    /// Con "Enter Play Mode Options" sin domain reload, el asset no se recarga al salir de Play,
    /// así que un oyente que no se desuscribe queda colgado y recibe eventos en la siguiente sesión.
    /// El OnDisable del asset solo limpia en recargas de dominio.
    /// </remarks>
    public abstract class EventChannel<T> : ScriptableObject
    {
        private event Action<T> Raised;

        public void Raise(T value) => Raised?.Invoke(value);

        public void Subscribe(Action<T> listener) => Raised += listener;

        public void Unsubscribe(Action<T> listener) => Raised -= listener;

        private void OnDisable() => Raised = null;
    }
}
