using System.Collections.Generic;
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

        // Un cargador por tipo de arma: sigue como estaba aunque se guarde y se vuelva a sacar.
        private readonly Dictionary<ItemDefinition, WeaponClip> clips = new Dictionary<ItemDefinition, WeaponClip>();
        private readonly RaycastHit[] rayHits = new RaycastHit[32];
        private WeaponClip activeClip;
        private float nextFireTime;
        private float impactHideTime;

        private Inventory Inventory => playerInventory.Inventory;
        /// <summary>Se disparó. True si la bala dio a un enemigo.</summary>
        public event System.Action<bool> Fired;

        /// <summary>Algún proyectil de este disparo dio en la cabeza.</summary>
        public event System.Action Headshot;

        /// <summary>Cambió lo que hay que enseñar del cargador (balas, recarga, arma en la mano).</summary>
        public event System.Action AmmoChanged;

        /// <summary>Empezó una recarga (para el sonido).</summary>
        public event System.Action ReloadStarted;

        /// <summary>Texto del cargador del arma en la mano: "7/12", "recargando…" o vacío.</summary>
        public string AmmoText
        {
            get
            {
                if (activeClip == null || !activeClip.HasMagazine) return string.Empty;
                return activeClip.IsReloading ? "recargando…" : $"{activeClip.Rounds}/{activeClip.Size}";
            }
        }

        /// <summary>
        /// Recarga el arma en la mano si le faltan balas.
        /// </summary>
        /// <returns>True si empezó a recargar o ya estaba recargando (la tecla queda "usada").</returns>
        public bool TryStartReload()
        {
            if (activeClip == null)
            {
                return false;
            }

            if (activeClip.IsReloading)
            {
                return true;
            }

            if (!activeClip.StartReload())
            {
                return false;
            }

            ReloadStarted?.Invoke();
            AmmoChanged?.Invoke();
            return true;
        }

        private void OnReloadPressed() => TryStartReload();


        private void OnEnable()
        {
            input.PrimaryPressed += Fire;
            input.ReloadPressed += OnReloadPressed;
            Inventory.SelectionChanged += OnSelectionChanged;
            Inventory.SlotChanged += OnSlotChanged;
            RefreshViewModel();
            RefreshActiveClip();
            if (impactMarker != null) impactMarker.SetActive(false);
        }

        private void OnDisable()
        {
            input.PrimaryPressed -= Fire;
            input.ReloadPressed -= OnReloadPressed;
            Inventory.SelectionChanged -= OnSelectionChanged;
            Inventory.SlotChanged -= OnSlotChanged;
        }

        private void Update()
        {
            if (impactMarker != null && impactMarker.activeSelf && Time.time >= impactHideTime)
            {
                impactMarker.SetActive(false);
            }

            if (activeClip != null && activeClip.Tick(Time.deltaTime))
            {
                AmmoChanged?.Invoke();
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
            if (activeClip != null)
            {
                if (activeClip.IsReloading)
                {
                    return;
                }

                // Cargador vacío: el clic recarga en vez de disparar.
                if (!activeClip.TryFire())
                {
                    TryStartReload();
                    return;
                }
            }

            nextFireTime = Time.time + weapon.FireInterval;

            // Un rayo por proyectil: las escopetas reparten varios dentro de su cono.
            bool hitEnemy = false;
            bool headshot = false;
            bool hitAnything = false;
            Vector3 lastPoint = Vector3.zero;
            for (int i = 0; i < weapon.Pellets; i++)
            {
                var ray = new Ray(eye.position, GetShotDirection(weapon.SpreadDegrees));
                if (!TryRaycastIgnoringSelf(ray, weapon.WeaponRange, out RaycastHit hit))
                {
                    continue;
                }

                hitAnything = true;
                lastPoint = hit.point;
                var target = hit.collider.GetComponentInParent<Health>();
                if (target != null && target.Faction == Faction.Enemy)
                {
                    // Según dónde dé: cabeza el doble, brazos y piernas menos, torso normal.
                    float multiplier = 1f;
                    if (target.TryGetComponent(out HitZoneBody body))
                    {
                        HitZone zone = body.GetZone(hit.point, ray);
                        multiplier = body.GetMultiplier(zone);
                        if (zone == HitZone.Head) headshot = true;
                    }

                    target.ApplyDamage(weapon.WeaponDamage * multiplier);
                    hitEnemy = true;
                }
            }

            Fired?.Invoke(hitEnemy);
            if (headshot) Headshot?.Invoke();

            // Con la última bala empieza a recargar sola.
            if (activeClip != null && activeClip.HasMagazine)
            {
                if (activeClip.Rounds == 0)
                {
                    TryStartReload();
                }
                else
                {
                    AmmoChanged?.Invoke();
                }
            }

            if (hitAnything && impactMarker != null)
            {
                impactMarker.transform.position = lastPoint;
                impactMarker.SetActive(true);
                impactHideTime = Time.time + ImpactSeconds;
            }
        }

        // Lo más cercano que toca la bala, sin contar al propio jugador.
        private bool TryRaycastIgnoringSelf(Ray ray, float range, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.RaycastNonAlloc(ray, rayHits, range, hitMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (rayHits[i].distance < best && !rayHits[i].collider.transform.IsChildOf(transform))
                {
                    best = rayHits[i].distance;
                    nearest = rayHits[i];
                }
            }

            return best < float.MaxValue;
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

        private void OnSelectionChanged(int _)
        {
            RefreshViewModel();
            RefreshActiveClip();
        }

        private void OnSlotChanged(int index)
        {
            if (index == Inventory.SelectedIndex)
            {
                RefreshViewModel();
                RefreshActiveClip();
            }
        }

        // El cargador del arma que hay ahora en la mano. Cambiar de arma a media recarga la cancela.
        private void RefreshActiveClip()
        {
            ItemStack selected = Inventory.Selected;
            WeaponClip clip = null;
            if (!selected.IsEmpty && selected.Item.Kind == ItemKind.Weapon)
            {
                if (!clips.TryGetValue(selected.Item, out clip))
                {
                    clip = new WeaponClip(selected.Item.MagazineSize, selected.Item.ReloadSeconds);
                    clips.Add(selected.Item, clip);
                }
            }

            if (clip == activeClip)
            {
                return;
            }

            if (activeClip != null) activeClip.CancelReload();
            activeClip = clip;

            // Si se guardó vacía, empieza a recargar al sacarla.
            if (activeClip != null && activeClip.HasMagazine && activeClip.Rounds == 0)
            {
                activeClip.StartReload();
            }

            AmmoChanged?.Invoke();
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
