using System.Collections.Generic;
using System.Text;
using DeadAcres.Items;
using DeadAcres.Player;
using UnityEngine;

namespace DeadAcres.Loot
{
    /// <summary>
    /// Algo que se registra una sola vez con Interactuar (F): un auto abandonado. El botín se
    /// tira la primera vez; lo que no quepa en el inventario se queda dentro para otra visita.
    /// </summary>
    public sealed class LootContainer : MonoBehaviour, IInteractable
    {
        [Tooltip("Identificador estable: es lo que se guarda en la partida.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName = "auto";
        [SerializeField] private LootTable table;

        [Tooltip("Opcional. Se muestra al quedar vacío (el maletero abierto).")]
        [SerializeField] private GameObject emptiedVisual;

        [Tooltip("Opcional. Se oculta al quedar vacío (el maletero cerrado).")]
        [SerializeField] private GameObject closedVisual;

        private readonly List<ItemStack> pending = new List<ItemStack>();
        private bool rolled;
        private int weaponAllowance = 1;

        /// <summary>Cuántas armas puede llegar a tener este contenedor (lo reparte LootDirector).</summary>
        public void SetWeaponAllowance(int weapons) => weaponAllowance = Mathf.Max(0, weapons);

        public string Id => id;

        /// <summary>Ya no queda nada dentro.</summary>
        public bool IsEmpty => rolled && pending.Count == 0;

        public string Prompt => rolled ? $"Recoger lo que queda en el {displayName}" : $"Registrar {displayName}";
        public bool CanInteract => table != null && !IsEmpty;

        private void Awake() => RefreshVisuals();

        public void Interact(GameObject player)
        {
            if (!CanInteract || !player.TryGetComponent(out PlayerInventory playerInventory))
            {
                return;
            }

            var summary = new StringBuilder();
            if (!rolled)
            {
                rolled = true;
                int gold = table.Roll(new System.Random(), pending, weaponAllowance);
                if (gold > 0 && player.TryGetComponent(out PlayerWallet wallet))
                {
                    wallet.Wallet.Add(gold);
                    summary.Append($"+{gold} de oro");
                }
            }

            // Se entrega lo que quepa; el resto se queda para cuando haya sitio.
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                ItemStack stack = pending[i];
                int leftover = playerInventory.Inventory.Add(stack.Item, stack.Count);
                int taken = stack.Count - leftover;
                if (taken > 0)
                {
                    if (summary.Length > 0) summary.Append(", ");
                    summary.Append($"+{taken} {stack.Item.DisplayName}");
                }

                if (leftover > 0)
                {
                    pending[i] = stack.WithCount(leftover);
                }
                else
                {
                    pending.RemoveAt(i);
                }
            }

            if (pending.Count > 0)
            {
                if (summary.Length > 0) summary.Append(". ");
                summary.Append("Inventario lleno: queda botín dentro");
            }
            else if (summary.Length == 0)
            {
                summary.Append("Vacío");
            }

            playerInventory.Notify(summary.ToString());
            RefreshVisuals();
        }

        /// <summary>Al cargar una partida: este ya se había vaciado.</summary>
        public void MarkEmpty()
        {
            rolled = true;
            pending.Clear();
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (emptiedVisual != null) emptiedVisual.SetActive(IsEmpty);
            if (closedVisual != null) closedVisual.SetActive(!IsEmpty);
        }

        internal void Configure(string containerId, string name, LootTable lootTable, GameObject closed,
            GameObject emptied)
        {
            id = containerId;
            displayName = name;
            table = lootTable;
            closedVisual = closed;
            emptiedVisual = emptied;
        }
    }
}
