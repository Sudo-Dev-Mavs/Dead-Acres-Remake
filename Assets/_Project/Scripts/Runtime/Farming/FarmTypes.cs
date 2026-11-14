using UnityEngine;

namespace DeadAcres.Farming
{
    public enum PlotStage : byte
    {
        /// <summary>Tierra arada, sin planta.</summary>
        Tilled = 0,
        Growing = 1,
        Ripe = 2,
    }

    public enum FarmActionResult : byte
    {
        Ok = 0,
        OutOfBounds = 1,
        AlreadyTilled = 2,
        NotTilled = 3,
        Occupied = 4,
        NothingPlanted = 5,
        NotRipe = 6,
        PlantLimitReached = 7,
        MissingSeed = 8,
        InventoryFull = 9,
        InvalidCrop = 10,
    }

    /// <summary>Foto de una parcela para vistas y HUD. Struct: no genera basura.</summary>
    public readonly struct PlotState
    {
        public readonly Vector2Int Cell;
        public readonly PlotStage Stage;

        /// <summary>Null si la parcela solo está arada.</summary>
        public readonly CropDefinition Crop;

        /// <summary>Tamaño visual de la planta: 0..<see cref="FarmField.GrowthSteps"/>-1.</summary>
        public readonly int GrowthStep;

        /// <summary>0..1 hacia la próxima cosecha.</summary>
        public readonly float Progress;

        public PlotState(Vector2Int cell, PlotStage stage, CropDefinition crop, int growthStep, float progress)
        {
            Cell = cell;
            Stage = stage;
            Crop = crop;
            GrowthStep = growthStep;
            Progress = progress;
        }
    }
}
