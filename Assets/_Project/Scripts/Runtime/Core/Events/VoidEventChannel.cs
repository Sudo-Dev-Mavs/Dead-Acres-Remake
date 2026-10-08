using System;
using UnityEngine;

namespace DeadAcres.Core.Events
{
    /// <summary>Canal de eventos sin payload (p. ej. "RV destruido", "victoria").</summary>
    [CreateAssetMenu(menuName = "Dead Acres/Events/Void Event Channel", fileName = "VoidEventChannel")]
    public sealed class VoidEventChannel : ScriptableObject
    {
        private event Action Raised;

        public void Raise() => Raised?.Invoke();

        public void Subscribe(Action listener) => Raised += listener;

        public void Unsubscribe(Action listener) => Raised -= listener;

        private void OnDisable() => Raised = null;
    }
}
