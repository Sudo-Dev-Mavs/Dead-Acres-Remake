using System;
using DeadAcres.Farming;
using UnityEngine;

namespace DeadAcres.Player
{
    public enum PrimaryActionKind : byte
    {
        None = 0,
        Till = 1,
        Plant = 2,
        Harvest = 3,
    }

    /// <summary>
    /// Qué está mirando el jugador y qué podría hacer ahí con el ítem seleccionado.
    /// Lo consumen el HUD (texto de ayuda) y el resaltado de la celda.
    /// </summary>
    public readonly struct InteractionContext : IEquatable<InteractionContext>
    {
        public readonly bool HasTarget;
        public readonly Vector2Int Cell;

        /// <summary>Lo que haría ahora mismo la acción primaria (clic).</summary>
        public readonly PrimaryActionKind Primary;

        /// <summary>Cultivo que se plantaría. Null si <see cref="Primary"/> no es Plant.</summary>
        public readonly CropDefinition CropToPlant;

        public readonly bool HasPlot;
        public readonly PlotStage Stage;

        /// <summary>Cultivo que ya hay en la parcela. Null si no hay.</summary>
        public readonly CropDefinition PlotCrop;

        /// <summary>Lo que se mira y admite Interactuar (Jack...).</summary>
        public readonly IInteractable Interactable;

        public InteractionContext(IInteractable interactable) : this()
        {
            Interactable = interactable;
        }

        public InteractionContext(bool hasTarget, Vector2Int cell, PrimaryActionKind primary,
            CropDefinition cropToPlant, bool hasPlot, PlotStage stage, CropDefinition plotCrop)
        {
            Interactable = null;
            HasTarget = hasTarget;
            Cell = cell;
            Primary = primary;
            CropToPlant = cropToPlant;
            HasPlot = hasPlot;
            Stage = stage;
            PlotCrop = plotCrop;
        }

        /// <summary>Hay un cultivo maduro en la celda. Cosecharlo requiere la hoz (Primary == Harvest).</summary>
        public bool IsRipe => HasPlot && PlotCrop != null && Stage == PlotStage.Ripe;

        public bool Equals(InteractionContext other)
        {
            return HasTarget == other.HasTarget
                && Cell == other.Cell
                && Primary == other.Primary
                && CropToPlant == other.CropToPlant
                && HasPlot == other.HasPlot
                && Stage == other.Stage
                && PlotCrop == other.PlotCrop
                && ReferenceEquals(Interactable, other.Interactable);
        }

        public override bool Equals(object obj) => obj is InteractionContext other && Equals(other);

        public override int GetHashCode() => Cell.GetHashCode() ^ ((int)Primary << 8) ^ ((int)Stage << 16);
    }
}
