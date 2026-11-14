using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Farming
{
    /// <summary>
    /// Acciones de granja que tocan a la vez la parcela y un inventario. Son atómicas: o se
    /// aplica todo o no cambia nada. Es el punto por el que pasan las intenciones del jugador;
    /// en co-op el cliente pedirá estas mismas operaciones al host en lugar de ejecutarlas.
    /// </summary>
    public static class FarmCommands
    {
        /// <summary>Planta gastando una semilla de la casilla indicada (la seleccionada en la hotbar).</summary>
        public static FarmActionResult PlantFromSlot(FarmField field, Vector2Int cell, CropDefinition crop,
            Inventory inventory, int slotIndex)
        {
            if (crop == null)
            {
                return FarmActionResult.InvalidCrop;
            }

            if (slotIndex < 0 || slotIndex >= inventory.SlotCount)
            {
                return FarmActionResult.MissingSeed;
            }

            ItemStack stack = inventory.GetSlot(slotIndex);
            if (stack.IsEmpty || stack.Item != crop.SeedItem)
            {
                return FarmActionResult.MissingSeed;
            }

            FarmActionResult result = field.CanPlant(cell, crop);
            if (result != FarmActionResult.Ok)
            {
                return result;
            }

            inventory.TryRemoveAt(slotIndex, 1);
            return field.Plant(cell, crop);
        }

        /// <summary>
        /// Cosecha y guarda el producto. Exige hueco para el rendimiento máximo del cultivo
        /// porque la cantidad se decide al cosechar: así nunca se pierde producto.
        /// </summary>
        public static FarmActionResult HarvestInto(FarmField field, Vector2Int cell, Inventory inventory,
            out ItemDefinition produce, out int amount)
        {
            produce = null;
            amount = 0;

            FarmActionResult result = field.CanHarvest(cell);
            if (result != FarmActionResult.Ok)
            {
                return result;
            }

            field.TryGetPlot(cell, out PlotState plot);
            if (!inventory.CanAdd(plot.Crop.ProduceItem, plot.Crop.YieldMax))
            {
                return FarmActionResult.InventoryFull;
            }

            result = field.Harvest(cell, out produce, out amount);
            if (result == FarmActionResult.Ok)
            {
                inventory.Add(produce, amount);
            }

            return result;
        }
    }
}
