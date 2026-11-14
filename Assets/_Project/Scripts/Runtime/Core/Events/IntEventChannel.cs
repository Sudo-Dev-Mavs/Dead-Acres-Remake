using UnityEngine;

namespace DeadAcres.Core.Events
{
    /// <summary>Canal de eventos con un entero (p. ej. número de noche superada).</summary>
    [CreateAssetMenu(menuName = "Dead Acres/Events/Int Event Channel", fileName = "IntEventChannel")]
    public sealed class IntEventChannel : EventChannel<int>
    {
    }
}
