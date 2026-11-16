using System.Collections.Generic;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Farming
{
    /// <summary>Lista de cultivos del juego. Resuelve qué cultivo planta cada semilla.</summary>
    [CreateAssetMenu(menuName = "Dead Acres/Farming/Crop Catalog", fileName = "CropCatalog")]
    public sealed class CropCatalog : ScriptableObject
    {
        [SerializeField] private List<CropDefinition> crops = new List<CropDefinition>();

        public IReadOnlyList<CropDefinition> Crops => crops;

        /// <summary>Búsqueda lineal: son pocos cultivos y solo se llama al usar una semilla.</summary>
        public bool TryGetBySeed(ItemDefinition seed, out CropDefinition crop)
        {
            if (seed != null)
            {
                for (int i = 0; i < crops.Count; i++)
                {
                    if (crops[i] != null && crops[i].SeedItem == seed)
                    {
                        crop = crops[i];
                        return true;
                    }
                }
            }

            crop = null;
            return false;
        }

        /// <summary>Por el Id estable del cultivo: es lo que se guarda en la partida.</summary>
        public bool TryGetById(string id, out CropDefinition crop)
        {
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < crops.Count; i++)
                {
                    if (crops[i] != null && crops[i].Id == id)
                    {
                        crop = crops[i];
                        return true;
                    }
                }
            }

            crop = null;
            return false;
        }

        internal void Configure(IEnumerable<CropDefinition> newCrops)
        {
            crops = new List<CropDefinition>(newCrops);
        }
    }
}
