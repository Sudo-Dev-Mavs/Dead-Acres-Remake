using DeadAcres.Core.Events;
using UnityEngine;

namespace DeadAcres.DayNight
{
    [CreateAssetMenu(menuName = "Dead Acres/Events/Night Result Event Channel", fileName = "NightCompletedChannel")]
    public sealed class NightResultEventChannel : EventChannel<NightResult>
    {
    }
}
