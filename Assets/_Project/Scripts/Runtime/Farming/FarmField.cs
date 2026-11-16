using System;
using System.Collections.Generic;
using DeadAcres.Building;
using DeadAcres.GameMode;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Farming
{
    /// <summary>
    /// Estado y reglas de la granja en C# puro: qué celdas están aradas, qué crece en ellas
    /// y cuándo madura. No sabe nada de input, física ni objetos de escena; en co-op vivirá
    /// solo en el host y los clientes recibirán <see cref="PlotChanged"/>.
    /// </summary>
    public sealed class FarmField
    {
        /// <summary>Tamaños visuales por los que pasa una planta mientras crece.</summary>
        public const int GrowthSteps = 4;

        private sealed class Plot
        {
            public Vector2Int Cell;
            public CropDefinition Crop;
            public PlotStage Stage;
            public float Elapsed;
            public float Duration;
            public int Step;

            /// <summary>True tras la primera cosecha: la planta ya tiene su tamaño final.</summary>
            public bool Regrowing;

            /// <summary>Id con el que la planta ocupa su celda en la rejilla de colocación.</summary>
            public int OccupantId;
        }

        private readonly RectInt bounds;
        private readonly PlacementLimiter limiter;
        private readonly PlacementGrid grid;
        private readonly System.Random random;
        private readonly Dictionary<Vector2Int, Plot> plots = new Dictionary<Vector2Int, Plot>();
        private readonly List<Plot> growing = new List<Plot>();
        private readonly List<Vector2Int> changedThisTick = new List<Vector2Int>();

        /// <param name="bounds">Celdas en las que se puede arar.</param>
        /// <param name="limiter">Límite de plantas del modo de juego. Null = sin límite.</param>
        /// <param name="random">Para el rendimiento de cada cosecha. Inyectable en tests.</param>
        /// <param name="grid">
        /// Rejilla de colocación compartida con los objetos. Con ella, una planta ocupa su celda
        /// (no se puede poner un muro encima) y no se ara ni se planta donde ya hay un objeto.
        /// </param>
        public FarmField(RectInt bounds, PlacementLimiter limiter = null, System.Random random = null,
            PlacementGrid grid = null)
        {
            this.bounds = bounds;
            this.limiter = limiter;
            this.grid = grid;
            this.random = random ?? new System.Random();
        }

        /// <summary>
        /// Una parcela cambió de forma visible: arada, plantada, creció un paso, maduró,
        /// se cosechó o perdió la planta. No se emite en cada frame de crecimiento.
        /// </summary>
        public event Action<Vector2Int> PlotChanged;

        public RectInt Bounds => bounds;
        public int PlotCount => plots.Count;

        public bool TryGetPlot(Vector2Int cell, out PlotState state)
        {
            if (plots.TryGetValue(cell, out Plot plot))
            {
                state = new PlotState(plot.Cell, plot.Stage, plot.Crop, plot.Step, GetProgress(plot));
                return true;
            }

            state = default;
            return false;
        }

        /// <summary>Añade a <paramref name="results"/> todas las celdas aradas (para vistas y guardado).</summary>
        public void GetCells(List<Vector2Int> results)
        {
            foreach (KeyValuePair<Vector2Int, Plot> pair in plots)
            {
                results.Add(pair.Key);
            }
        }

        public FarmActionResult CanTill(Vector2Int cell)
        {
            if (!bounds.Contains(cell)) return FarmActionResult.OutOfBounds;
            if (plots.ContainsKey(cell)) return FarmActionResult.AlreadyTilled;
            if (IsBlocked(cell)) return FarmActionResult.Blocked;
            return FarmActionResult.Ok;
        }

        public FarmActionResult Till(Vector2Int cell)
        {
            FarmActionResult result = CanTill(cell);
            if (result != FarmActionResult.Ok)
            {
                return result;
            }

            plots.Add(cell, new Plot { Cell = cell, Stage = PlotStage.Tilled });
            PlotChanged?.Invoke(cell);
            return FarmActionResult.Ok;
        }

        public FarmActionResult CanPlant(Vector2Int cell, CropDefinition crop)
        {
            if (crop == null) return FarmActionResult.InvalidCrop;
            if (!bounds.Contains(cell)) return FarmActionResult.OutOfBounds;
            if (!plots.TryGetValue(cell, out Plot plot)) return FarmActionResult.NotTilled;
            if (plot.Crop != null) return FarmActionResult.Occupied;
            if (IsBlocked(cell)) return FarmActionResult.Blocked;
            if (limiter != null && !limiter.CanPlace(PlaceableCategory.Plant)) return FarmActionResult.PlantLimitReached;
            return FarmActionResult.Ok;
        }

        public FarmActionResult Plant(Vector2Int cell, CropDefinition crop)
        {
            FarmActionResult result = CanPlant(cell, crop);
            if (result != FarmActionResult.Ok)
            {
                return result;
            }

            if (limiter != null && !limiter.TryRegister(PlaceableCategory.Plant))
            {
                return FarmActionResult.PlantLimitReached;
            }

            Plot plot = plots[cell];
            plot.Crop = crop;
            plot.Regrowing = false;
            plot.OccupantId = grid != null ? grid.Occupy(new RectInt(cell, Vector2Int.one)) : PlacementGrid.NoOccupant;
            StartGrowing(plot, crop.GrowSeconds);
            PlotChanged?.Invoke(cell);
            return FarmActionResult.Ok;
        }

        public FarmActionResult CanHarvest(Vector2Int cell)
        {
            if (!bounds.Contains(cell)) return FarmActionResult.OutOfBounds;
            if (!plots.TryGetValue(cell, out Plot plot) || plot.Crop == null) return FarmActionResult.NothingPlanted;
            if (plot.Stage != PlotStage.Ripe) return FarmActionResult.NotRipe;
            return FarmActionResult.Ok;
        }

        /// <summary>
        /// Cosecha una parcela madura. Si el cultivo rebrota, la planta se queda y empieza a
        /// preparar la siguiente cosecha; si no, la parcela vuelve a tierra arada.
        /// </summary>
        public FarmActionResult Harvest(Vector2Int cell, out ItemDefinition produce, out int amount)
        {
            produce = null;
            amount = 0;

            FarmActionResult result = CanHarvest(cell);
            if (result != FarmActionResult.Ok)
            {
                return result;
            }

            Plot plot = plots[cell];
            CropDefinition crop = plot.Crop;
            produce = crop.ProduceItem;
            amount = random.Next(crop.YieldMin, crop.YieldMax + 1);

            if (crop.Regrows)
            {
                plot.Regrowing = true;
                StartGrowing(plot, crop.RegrowSeconds);
            }
            else
            {
                RemoveCrop(plot);
            }

            PlotChanged?.Invoke(cell);
            return FarmActionResult.Ok;
        }

        /// <summary>Estado completo de una parcela, para guardar la partida.</summary>
        public readonly struct PlotSnapshot
        {
            public readonly Vector2Int Cell;
            public readonly CropDefinition Crop;
            public readonly float Elapsed;
            public readonly bool Regrowing;
            public readonly bool Ripe;

            public PlotSnapshot(Vector2Int cell, CropDefinition crop, float elapsed, bool regrowing, bool ripe)
            {
                Cell = cell;
                Crop = crop;
                Elapsed = elapsed;
                Regrowing = regrowing;
                Ripe = ripe;
            }
        }

        public void CapturePlots(List<PlotSnapshot> results)
        {
            foreach (KeyValuePair<Vector2Int, Plot> pair in plots)
            {
                Plot plot = pair.Value;
                results.Add(new PlotSnapshot(plot.Cell, plot.Crop, plot.Elapsed, plot.Regrowing,
                    plot.Stage == PlotStage.Ripe));
            }
        }

        /// <summary>
        /// Al cargar una partida: deja la parcela arada y, si llevaba planta, la planta en el
        /// punto de crecimiento en el que estaba. Pasa por las mismas reglas que arar y plantar.
        /// </summary>
        /// <returns>False si la celda ya no admite la parcela o la planta.</returns>
        public bool RestorePlot(PlotSnapshot snapshot)
        {
            FarmActionResult tilled = Till(snapshot.Cell);
            if (tilled != FarmActionResult.Ok && tilled != FarmActionResult.AlreadyTilled)
            {
                return false;
            }

            if (snapshot.Crop == null)
            {
                return true;
            }

            if (Plant(snapshot.Cell, snapshot.Crop) != FarmActionResult.Ok)
            {
                return false;
            }

            Plot plot = plots[snapshot.Cell];
            plot.Regrowing = snapshot.Regrowing && snapshot.Crop.Regrows;
            plot.Duration = plot.Regrowing ? snapshot.Crop.RegrowSeconds : snapshot.Crop.GrowSeconds;

            if (snapshot.Ripe)
            {
                plot.Elapsed = plot.Duration;
                plot.Stage = PlotStage.Ripe;
                plot.Step = GrowthSteps - 1;
                growing.Remove(plot);
            }
            else
            {
                // Un pelo por debajo del final: madura en el siguiente Tick, no aquí.
                plot.Elapsed = Mathf.Clamp(snapshot.Elapsed, 0f, Mathf.Max(0f, plot.Duration - 0.01f));
                plot.Step = ComputeStep(plot);
            }

            PlotChanged?.Invoke(snapshot.Cell);
            return true;
        }

        /// <summary>Arranca la planta (martillo, zombis...). La parcela vuelve a tierra arada.</summary>
        /// <returns>False si no había planta.</returns>
        public bool ClearCrop(Vector2Int cell)
        {
            if (!plots.TryGetValue(cell, out Plot plot) || plot.Crop == null)
            {
                return false;
            }

            RemoveCrop(plot);
            PlotChanged?.Invoke(cell);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || growing.Count == 0)
            {
                return;
            }

            // Los eventos se emiten al final: un oyente puede cosechar o arrancar plantas
            // (y por tanto modificar la lista) sin romper el recorrido.
            changedThisTick.Clear();

            for (int i = growing.Count - 1; i >= 0; i--)
            {
                Plot plot = growing[i];
                plot.Elapsed += deltaTime;

                if (plot.Elapsed >= plot.Duration)
                {
                    plot.Stage = PlotStage.Ripe;
                    plot.Step = GrowthSteps - 1;
                    growing.RemoveAt(i);
                    changedThisTick.Add(plot.Cell);
                    continue;
                }

                int step = ComputeStep(plot);
                if (step != plot.Step)
                {
                    plot.Step = step;
                    changedThisTick.Add(plot.Cell);
                }
            }

            if (PlotChanged == null)
            {
                return;
            }

            for (int i = 0; i < changedThisTick.Count; i++)
            {
                PlotChanged.Invoke(changedThisTick[i]);
            }
        }

        private void StartGrowing(Plot plot, float duration)
        {
            plot.Stage = PlotStage.Growing;
            plot.Elapsed = 0f;
            plot.Duration = duration;
            plot.Step = ComputeStep(plot);

            if (!growing.Contains(plot))
            {
                growing.Add(plot);
            }
        }

        private void RemoveCrop(Plot plot)
        {
            growing.Remove(plot);
            plot.Crop = null;
            plot.Stage = PlotStage.Tilled;
            plot.Elapsed = 0f;
            plot.Duration = 0f;
            plot.Step = 0;
            plot.Regrowing = false;
            limiter?.Unregister(PlaceableCategory.Plant);

            if (grid != null && plot.OccupantId != PlacementGrid.NoOccupant)
            {
                grid.Release(plot.OccupantId);
            }

            plot.OccupantId = PlacementGrid.NoOccupant;
        }

        private bool IsBlocked(Vector2Int cell) =>
            grid != null && grid.GetOccupant(cell) != PlacementGrid.NoOccupant;

        private static float GetProgress(Plot plot)
        {
            if (plot.Stage == PlotStage.Ripe) return 1f;
            if (plot.Stage != PlotStage.Growing || plot.Duration <= 0f) return 0f;
            return Mathf.Clamp01(plot.Elapsed / plot.Duration);
        }

        private static int ComputeStep(Plot plot)
        {
            if (plot.Regrowing)
            {
                return GrowthSteps - 1;
            }

            return Mathf.Min(GrowthSteps - 1, Mathf.FloorToInt(GetProgress(plot) * GrowthSteps));
        }
    }
}
