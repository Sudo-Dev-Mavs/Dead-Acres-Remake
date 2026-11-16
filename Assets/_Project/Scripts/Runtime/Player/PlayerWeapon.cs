using DeadAcres.Combat;
using DeadAcres.Items;
using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>
    /// Disparo provisional del arma seleccionada en la hotbar: un rayo desde la cámara que daña
    /// al primer enemigo que toca. Munición infinita, como parecía en el original; el ritmo lo
    /// limita la cadencia del arma.
    /// </summary>
    public sealed class PlayerWeapon : MonoBehaviour
    {
        private const float ImpactSeconds = 0.08f;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerInventory playerInventory;

        [Tooltip("Origen y dirección del disparo (la cámara).")]
        [SerializeField] private Transform eye;

        [Tooltip("Opcional. Se muestra solo con un arma en la mano.")]
        [SerializeField] private GameObject viewModel;

        [Tooltip("Opcional. Marca breve en el punto de impacto.")]
        [SerializeField] private GameObject impactMarker;

        [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;

        private float nextFireTime;
        private float impactHideTime;

        private Inventory Inventory => playerInventory.Inventory;
        /// <summary>Se disparó. True si la bala dio a un enemigo.</summary>
        public event System.Action<bool> Fired;


        private void OnEnable()
        {
            input.PrimaryPressed += Fire;
            Inventory.SelectionChanged += OnSelectionChanged;
            Inventory.SlotChanged += OnSlotChanged;
            RefreshViewModel();
            if (impactMarker != null) impactMarker.SetActive(false);
        }

        private void OnDisable()
        {
            input.PrimaryPressed -= Fire;
            Inventory.SelectionChanged -= OnSelectionChanged;
            Inventory.SlotChanged -= OnSlotChanged;
        }

        private void Update()
        {
            if (impactMarker != null && impactMarker.activeSelf && Time.time >= impactHideTime)
            {
                impactMarker.SetActive(false);
            }

            // Armas automáticas: mientras se mantenga pulsado, Fire respeta la cadencia.
            ItemStack selected = Inventory.Selected;
            if (!selected.IsEmpty && selected.Item.IsAutomatic && input.isActiveAndEnabled && input.IsPrimaryHeld)
            {
                Fire();
            }
        }

        private void Fire()
        {
            ItemStack selected = Inventory.Selected;
            if (selected.IsEmpty || selected.Item.Kind != ItemKind.Weapon || eye == null
                || Cursor.lockState != CursorLockMode.Locked || Time.time < nextFireTime)
            {
                return;
            }

            ItemDefinition weapon = selected.Item;
            nextFireTime = Time.time + weapon.FireInterval;

            // Un rayo por proyectil: las escopetas reparten varios dentro de su cono.
            bool hitEnemy = false;
            bool hitAnything = false;
            Vector3 lastPoint = Vector3.zero;
            for (int i = 0; i < weapon.Pellets; i++)
            {
                var ray = new Ray(eye.position, GetShotDirection(weapon.SpreadDegrees));
                if (!Physics.Raycast(ray, out RaycastHit hit, weapon.WeaponRange, hitMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                hitAnything = true;
                lastPoint = hit.point;
                var target = hit.collider.GetComponentInParent<Health>();
                if (target != null && target.Faction == Faction.Enemy)
                {
                    target.ApplyDamage(weapon.WeaponDamage);
                    hitEnemy = true;
                }
            }

            Fired?.Invoke(hitEnemy);

            if (hitAnything && impactMarker != null)
            {
                impactMarker.transform.position = lastPoint;
                impactMarker.SetActive(true);
                impactHideTime = Time.time + ImpactSeconds;
            }
        }

        // Dirección al azar dentro de un cono de esa apertura alrededor de la mira.
        private Vector3 GetShotDirection(float spreadDegrees)
        {
            if (spreadDegrees <= 0f)
            {
                return eye.forward;
            }

            Vector2 offset = Random.insideUnitCircle * spreadDegrees;
            return eye.rotation * (Quaternion.Euler(offset.y, offset.x, 0f) * Vector3.forward);
        }

        private void OnSelectionChanged(int _) => RefreshViewModel();

        private void OnSlotChanged(int index)
        {
            if (index == Inventory.SelectedIndex)
            {
                RefreshViewModel();
            }
        }

        private void RefreshViewModel()
        {
            if (viewModel == null)
            {
                return;
            }

            ItemStack selected = Inventory.Selected;
            viewModel.SetActive(!selected.IsEmpty && selected.Item.Kind == ItemKind.Weapon);
        }

        internal void Configure(PlayerInputReader inputReader, PlayerInventory inventory, Transform eyeTransform,
            GameObject weaponViewModel, GameObject impact)
        {
            input = inputReader;
            playerInventory = inventory;
            eye = eyeTransform;
            viewModel = weaponViewModel;
            impactMarker = impact;
        }
    }
}
