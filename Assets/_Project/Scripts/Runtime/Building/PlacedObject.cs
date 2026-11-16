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
        [Tooltip("Solo para objetos que ya vienen en la escena: el ítem que devuelve el martillo.")]
        [SerializeField] private ItemDefinition item;

        private FarmWorld world;
        private int occupantId;
        private PlaceableCategory category;
        private bool released;

        public int OccupantId => occupantId;
        public FarmWorld World => world;

        /// <summary>Ítem con el que se colocó (lo que devuelve el martillo). Puede ser null.</summary>
        public ItemDefinition Item => item;
        public PlaceableCategory Category => category;

        public void Bind(FarmWorld farmWorld, int id, PlaceableCategory placeableCategory,
            ItemDefinition sourceItem = null)
        {
            item = sourceItem;
            world = farmWorld;
            occupantId = id;
            category = placeableCategory;
            released = false;
        }

        /// <summary>
        /// Para objetos puestos en la escena sin pasar por la rejilla: no ocupan celda ni cuentan
        /// para el límite, pero el martillo los recoge como el ítem indicado.
        /// </summary>
        internal void ConfigureItem(ItemDefinition sourceItem) => item = sourceItem;

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
