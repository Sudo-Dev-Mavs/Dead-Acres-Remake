using DeadAcres.GameMode;
using UnityEngine;

namespace DeadAcres.Building
{
    /// <summary>
    /// Marca un objeto colocado en la rejilla. Al destruirse (martillo, enemigos, cosecha)
    /// libera sus celdas y su hueco en el límite, pase lo que pase con el GameObject.
    /// </summary>
    public sealed class PlacedObject : MonoBehaviour
    {
        private FarmWorld world;
        private int occupantId;
        private PlaceableCategory category;
        private bool released;

        public int OccupantId => occupantId;
        public PlaceableCategory Category => category;

        public void Bind(FarmWorld farmWorld, int id, PlaceableCategory placeableCategory)
        {
            world = farmWorld;
            occupantId = id;
            category = placeableCategory;
            released = false;
        }

        private void OnDestroy() => Release();

        public void Release()
        {
            if (released || world == null) return;

            released = true;
            world.Grid.Release(occupantId);
            world.Limiter.Unregister(category);
        }
    }
}
