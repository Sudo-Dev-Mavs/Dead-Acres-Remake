using DeadAcres.GameMode;
using DeadAcres.Items;
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
        public FarmWorld World => world;

        /// <summary>Ítem con el que se colocó (lo que devuelve el martillo). Puede ser null.</summary>
        public ItemDefinition Item { get; private set; }
        public PlaceableCategory Category => category;

        public void Bind(FarmWorld farmWorld, int id, PlaceableCategory placeableCategory,
            ItemDefinition sourceItem = null)
        {
            Item = sourceItem;
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
