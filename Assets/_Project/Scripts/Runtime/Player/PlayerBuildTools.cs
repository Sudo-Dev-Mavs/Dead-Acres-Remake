using System;
using DeadAcres.Building;
using DeadAcres.Combat;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>
    /// Herramientas y consumibles del jugador local, con la acción primaria:
    /// el martillo recoge un objeto colocado (y lo devuelve al inventario), la cinta americana
    /// repara lo que se mire del bando del jugador y la comida cura al propio jugador.
    /// </summary>
    public sealed class PlayerBuildTools : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerInventory playerInventory;

        [Tooltip("Origen y dirección del rayo (la cámara).")]
        [SerializeField] private Transform eye;

        [SerializeField, Min(0.5f)] private float reach = 6f;
        [SerializeField] private LayerMask targetMask = Physics.DefaultRaycastLayers;

        /// <summary>Texto corto para el HUD ("Reparado", "Inventario lleno"...).</summary>
        public event Action<string> Message;

        private Inventory Inventory => playerInventory.Inventory;

        private void OnEnable() => input.PrimaryPressed += Use;

        private void OnDisable() => input.PrimaryPressed -= Use;

        private void Use()
        {
            ItemStack selected = Inventory.Selected;
            if (selected.IsEmpty || eye == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            if (selected.Item.HealAmount > 0f)
            {
                Eat(selected.Item);
                return;
            }

            bool isHammer = selected.Item.Tool == ToolKind.Hammer;
            bool isRepair = selected.Item.RepairAmount > 0f;
            if (!isHammer && !isRepair)
            {
                return;
            }

            if (!Physics.Raycast(new Ray(eye.position, eye.forward), out RaycastHit hit, reach, targetMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            if (isHammer)
            {
                PickUp(hit.collider.GetComponentInParent<PlacedObject>());
            }
            else
            {
                Repair(hit.collider.GetComponentInParent<Health>(), selected.Item);
            }
        }

        private void Eat(ItemDefinition food)
        {
            if (!TryGetComponent(out Health health) || health.IsDead)
            {
                return;
            }

            if (health.Current >= health.Max)
            {
                Message?.Invoke("Ya tienes la vida completa");
                return;
            }

            health.Heal(food.HealAmount);
            Inventory.RemoveAt(Inventory.SelectedIndex, 1);
            Message?.Invoke($"{food.DisplayName}: +{Mathf.CeilToInt(food.HealAmount)} de vida");
        }

        private void PickUp(PlacedObject placed)
        {
            if (placed == null)
            {
                return;
            }

            // Recoger algo dañado y volver a colocarlo lo dejaría como nuevo gratis.
            if (placed.TryGetComponent(out Health placedHealth) && placedHealth.Current < placedHealth.Max - 0.5f)
            {
                Message?.Invoke("Está dañado: repáralo con cinta antes de recogerlo");
                return;
            }

            ItemDefinition item = placed.Item;
            if (item != null)
            {
                if (!Inventory.CanAdd(item, 1))
                {
                    Message?.Invoke("Inventario lleno");
                    return;
                }

                Inventory.Add(item, 1);
                Message?.Invoke($"Recogido: {item.DisplayName}");
            }

            // Al destruirse, PlacedObject libera su celda y su hueco en el límite.
            Destroy(placed.gameObject);
        }

        private void Repair(Health target, ItemDefinition tape)
        {
            if (target == null || target.Faction != Faction.PlayerSide || target.IsDead
                || target.gameObject == gameObject)
            {
                return;
            }

            if (target.Current >= target.Max)
            {
                Message?.Invoke("No necesita reparación");
                return;
            }

            // Las defensas caras cuestan oro además de la cinta.
            int cost = target.TryGetComponent(out PlacedObject placed) && placed.Item != null
                ? placed.Item.RepairGoldCost
                : 0;
            if (cost > 0)
            {
                if (!TryGetComponent(out PlayerWallet wallet) || !wallet.Wallet.TrySpend(cost))
                {
                    Message?.Invoke($"Repararlo cuesta {cost} de oro");
                    return;
                }
            }

            target.Heal(tape.RepairAmount);
            Inventory.RemoveAt(Inventory.SelectedIndex, 1);
            string paid = cost > 0 ? $"  (-{cost} de oro)" : string.Empty;
            Message?.Invoke($"Reparado: {Mathf.CeilToInt(target.Current)}/{Mathf.CeilToInt(target.Max)}{paid}");
        }

        internal void Configure(PlayerInputReader inputReader, PlayerInventory inventory, Transform eyeTransform)
        {
            input = inputReader;
            playerInventory = inventory;
            eye = eyeTransform;
        }
    }
}
