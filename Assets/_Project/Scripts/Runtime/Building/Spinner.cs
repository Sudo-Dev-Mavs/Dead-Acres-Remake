using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>Gira sin parar sobre su eje Z local (las aspas del molino).</summary>
    public sealed class Spinner : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 40f;

        private void Update() => transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
