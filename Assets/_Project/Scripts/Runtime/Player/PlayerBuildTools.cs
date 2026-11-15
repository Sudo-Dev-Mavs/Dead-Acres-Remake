using System;
using DeadAcres.Building;
using DeadAcres.Combat;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>
    /// Herramientas de construcción del jugador local, con la acción primaria:
    /// el martillo recoge un objeto colocado (y lo devuelve al inventario) y la cinta americana
    /// repara lo que se mire del bando del jugador, gastando una unidad.
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

        private void PickUp(PlacedObject placed)
        {
            if (placed == null)
            {
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

            target.Heal(tape.RepairAmount);
            Inventory.RemoveAt(Inventory.SelectedIndex, 1);
            Message?.Invoke($"Reparado: {Mathf.CeilToInt(target.Current)}/{Mathf.CeilToInt(target.Max)}");
        }

        internal void Configure(PlayerInputReader inputReader, PlayerInventory inventory, Transform eyeTransform)
        {
            input = inputReader;
            playerInventory = inventory;
            eye = eyeTransform;
        }
    }
}
