using System.Collections.Generic;
using DeadAcres.Farming;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Saving
{
    /// <summary>
    /// Paso entre la lógica pura (inventario, parcelas) y los datos planos del guardado.
    /// Sin escena ni disco: se puede probar sola.
    /// </summary>
    public static class SaveMapper
    {
        public static void CaptureInventory(Inventory inventory, SaveData data)
        {
            data.slots.Clear();
            data.selectedSlot = inventory.SelectedIndex;
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                ItemStack stack = inventory[i];
                if (!stack.IsEmpty)
                {
                    data.slots.Add(new SlotSave { index = i, item = stack.Item.Id, count = stack.Count });
                }
            }
        }

        /// <returns>Cuántas ranuras no se pudieron restaurar (ítem que ya no existe).</returns>
        public static int RestoreInventory(SaveData data, Inventory inventory, ItemCatalog catalog)
        {
            int missing = 0;
            inventory.Clear();
            for (int i = 0; i < data.slots.Count; i++)
            {
                SlotSave slot = data.slots[i];
                if (catalog.TryGet(slot.item, out ItemDefinition item) && inventory.SetSlot(slot.index, item, slot.count))
                {
                    continue;
                }

                missing++;
            }

            inventory.Select(data.selectedSlot);
            return missing;
        }

        public static void CapturePlots(FarmField field, SaveData data)
        {
            var snapshots = new List<FarmField.PlotSnapshot>();
            field.CapturePlots(snapshots);

            data.plots.Clear();
            for (int i = 0; i < snapshots.Count; i++)
            {
                FarmField.PlotSnapshot plot = snapshots[i];
                data.plots.Add(new PlotSave
                {
                    x = plot.Cell.x,
                    z = plot.Cell.y,
                    crop = plot.Crop != null ? plot.Crop.Id : string.Empty,
                    elapsed = plot.Elapsed,
                    regrowing = plot.Regrowing,
                    ripe = plot.Ripe,
                });
            }
        }

        /// <returns>Cuántas parcelas no se pudieron restaurar.</returns>
        public static int RestorePlots(SaveData data, FarmField field, CropCatalog crops)
        {
            int failed = 0;
            for (int i = 0; i < data.plots.Count; i++)
            {
                PlotSave plot = data.plots[i];
                CropDefinition crop = null;
                if (!string.IsNullOrEmpty(plot.crop) && !crops.TryGetById(plot.crop, out crop))
                {
                    // El cultivo ya no existe: se conserva al menos la tierra arada.
                    failed++;
                }

                var snapshot = new FarmField.PlotSnapshot(new Vector2Int(plot.x, plot.z), crop, plot.elapsed,
                    plot.regrowing, plot.ripe && crop != null);
                if (!field.RestorePlot(snapshot))
                {
                    failed++;
                }
            }

            return failed;
        }
    }
}
