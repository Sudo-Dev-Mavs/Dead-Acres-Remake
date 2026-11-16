using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadAcres.Loot
{
    /// <summary>
    /// Reparte el cupo de armas entre los autos de la partida: como mucho un arma en total. Un
    /// solo auto, elegido al azar, puede llegar a tenerla; que le salga o no depende de su tirada.
    /// La elección sale de una semilla que se guarda con la partida.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public sealed class LootDirector : MonoBehaviour
    {
        public int Seed { get; private set; }

        private void Awake() => ApplySeed(UnityEngine.Random.Range(1, int.MaxValue));

        /// <summary>Al cargar una partida: el mismo auto afortunado que tenía.</summary>
        public void ApplySeed(int seed)
        {
            Seed = seed;
            var containers = new List<LootContainer>(FindObjectsByType<LootContainer>(FindObjectsSortMode.None));

            // Orden estable: el de búsqueda no lo es entre una carga y otra.
            containers.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            int lucky = PickLucky(seed, containers.Count);
            for (int i = 0; i < containers.Count; i++)
            {
                containers[i].SetWeaponAllowance(i == lucky ? 1 : 0);
            }
        }

        /// <summary>Índice del auto que puede tener el arma. -1 si no hay autos.</summary>
        public static int PickLucky(int seed, int count)
        {
            return count <= 0 ? -1 : new System.Random(seed).Next(count);
        }
    }
}
