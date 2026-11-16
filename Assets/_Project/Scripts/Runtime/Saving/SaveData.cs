using System;
using System.Collections.Generic;

namespace DeadAcres.Saving
{
    /// <summary>
    /// Lo que se guarda de una partida. Solo datos planos e Ids de texto: nunca referencias a
    /// assets. Se escribe como JSON.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        /// <summary>Día que empieza al cargar (se guarda al amanecer).</summary>
        public int dayNumber = 1;
        public string difficulty;
        public string mode;

        public int gold;
        public float playerHealth;
        public float rvHealth;

        public int selectedSlot;
        public List<SlotSave> slots = new List<SlotSave>();
        public List<PlotSave> plots = new List<PlotSave>();
        public List<PlacedSave> placed = new List<PlacedSave>();

        /// <summary>Ids de los autos (u otros contenedores) ya vaciados.</summary>
        public List<string> emptiedContainers = new List<string>();
    }

    [Serializable]
    public struct SlotSave
    {
        public int index;
        public string item;
        public int count;
    }

    [Serializable]
    public struct PlotSave
    {
        public int x;
        public int z;

        /// <summary>Vacío: solo tierra arada.</summary>
        public string crop;
        public float elapsed;
        public bool regrowing;
        public bool ripe;
    }

    [Serializable]
    public struct PlacedSave
    {
        public string item;

        /// <summary>True: ocupa las celdas de abajo. False: está suelto en la posición (animales de regalo).</summary>
        public bool onGrid;
        public int x;
        public int z;
        public int width;
        public int depth;
        public float posX;
        public float posZ;
        public float yaw;

        /// <summary>0 o menos: sin vida que restaurar.</summary>
        public float health;
    }
}
