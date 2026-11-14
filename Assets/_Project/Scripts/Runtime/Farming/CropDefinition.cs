using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Farming
{
    /// <summary>
    /// Datos de un cultivo. Como en el original, tras cosechar la planta se queda y vuelve a dar
    /// fruto (<see cref="RegrowSeconds"/>), salvo que se configure como cosecha única.
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Farming/Crop Definition", fileName = "Crop_")]
    public sealed class CropDefinition : ScriptableObject
    {
        [Tooltip("Identificador estable (p. ej. strawberry). No cambiar una vez usado.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ItemDefinition seedItem;
        [SerializeField] private ItemDefinition produceItem;

        [Tooltip("Segundos desde que se planta hasta la primera cosecha.")]
        [SerializeField, Min(0.1f)] private float growSeconds = 60f;

        [Tooltip("Segundos para volver a dar fruto tras cosechar. 0 = cosecha única (la planta desaparece).")]
        [SerializeField, Min(0f)] private float regrowSeconds = 45f;

        [SerializeField, Min(1)] private int yieldMin = 1;
        [SerializeField, Min(1)] private int yieldMax = 2;

        [Header("Visual provisional")]
        [SerializeField] private Color ripeColor = Color.red;
        [SerializeField, Min(0.1f)] private float ripeHeight = 0.5f;

        public string Id => id;
        public string DisplayName => displayName;
        public ItemDefinition SeedItem => seedItem;
        public ItemDefinition ProduceItem => produceItem;
        public float GrowSeconds => growSeconds;
        public float RegrowSeconds => regrowSeconds;
        public bool Regrows => regrowSeconds > 0f;
        public int YieldMin => yieldMin;
        public int YieldMax => yieldMax;
        public Color RipeColor => ripeColor;
        public float RipeHeight => ripeHeight;

        internal void Configure(string newId, string name, ItemDefinition seed, ItemDefinition produce,
            float grow, float regrow, int minYield, int maxYield, Color color, float height)
        {
            id = newId;
            displayName = name;
            seedItem = seed;
            produceItem = produce;
            growSeconds = Mathf.Max(0.1f, grow);
            regrowSeconds = Mathf.Max(0f, regrow);
            yieldMin = Mathf.Max(1, minYield);
            yieldMax = Mathf.Max(yieldMin, maxYield);
            ripeColor = color;
            ripeHeight = Mathf.Max(0.1f, height);
        }

        private void OnValidate()
        {
            if (yieldMax < yieldMin)
            {
                yieldMax = yieldMin;
            }
        }
    }
}
