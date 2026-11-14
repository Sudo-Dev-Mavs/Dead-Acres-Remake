using DeadAcres.Core.Events;
using UnityEngine;

namespace DeadAcres.DayNight
{
    [CreateAssetMenu(menuName = "Dead Acres/Events/Phase Event Channel", fileName = "PhaseChangedChannel")]
    public sealed class PhaseEventChannel : EventChannel<PhaseChange>
    {
    }
}
