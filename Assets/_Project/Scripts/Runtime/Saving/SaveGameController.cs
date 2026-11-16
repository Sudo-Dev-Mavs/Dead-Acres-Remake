using System;
using System.Collections.Generic;
using DeadAcres.Building;
using DeadAcres.Combat;
using DeadAcres.Core.Session;
using DeadAcres.DayNight;
using DeadAcres.Farming;
using DeadAcres.Items;
using DeadAcres.Loot;
using DeadAcres.Player;
using UnityEngine;

namespace DeadAcres.Saving
{
    /// <summary>
    /// Guarda la partida sola cada amanecer (cuando ya no quedan zombis) y, si el menú pidió
    /// "Continuar", la carga al arrancar la escena. En co-op vivirá solo en el host.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class SaveGameController : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private DayNightCycle dayNight;
        [SerializeField] private PhaseEventChannel phaseChanged;
        [SerializeField] private ItemCatalog items;
        [SerializeField] private CropCatalog crops;
        [SerializeField] private FarmWorld world;
        [SerializeField] private FarmArea farm;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerWallet playerWallet;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Health rvHealth;

        private SaveData pendingLoad;

        /// <summary>Se acaba de guardar la partida (al amanecer de este día).</summary>
        public event Action<int> Saved;

        // En Awake: el reloj arranca en su Start y tiene que saber ya en qué día.
        private void Awake()
        {
            if (session == null || !session.ContinueFromSave)
            {
                return;
            }

            session.ContinueFromSave = false;
            if (SaveFile.TryRead(out pendingLoad) && dayNight != null)
            {
                dayNight.SetStartDay(pendingLoad.dayNumber);
            }
        }

        private void OnEnable()
        {
            if (phaseChanged != null) phaseChanged.Subscribe(OnPhaseChanged);
        }

        private void OnDisable()
        {
            if (phaseChanged != null) phaseChanged.Unsubscribe(OnPhaseChanged);
        }

        // En Start y con orden tardío: el HUD y las vistas ya escuchan, así que se enteran de lo
        // restaurado por los mismos eventos que durante el juego.
        private void Start()
        {
            if (pendingLoad == null)
            {
                return;
            }

            SaveData data = pendingLoad;
            Restore(data);
            pendingLoad = null;
        }

        private void OnPhaseChanged(PhaseChange change)
        {
            // El primer amanecer no se guarda (no hay nada que perder) y, mientras se carga una
            // partida, tampoco: se pisaría el archivo con la escena todavía sin restaurar.
            if (change.Phase != GamePhase.Dawn || change.DayNumber < 2 || pendingLoad != null)
            {
                return;
            }

            if (SaveFile.Write(Capture(change.DayNumber)))
            {
                Saved?.Invoke(change.DayNumber);
                if (playerInventory != null) playerInventory.Notify("Partida guardada");
            }
        }

        private SaveData Capture(int dayNumber)
        {
            var data = new SaveData { dayNumber = dayNumber };
            if (session != null)
            {
                data.difficulty = session.Difficulty != null ? session.Difficulty.DisplayName : string.Empty;
                data.mode = session.Mode != null ? session.Mode.DisplayName : string.Empty;
            }

            data.gold = playerWallet != null ? playerWallet.Wallet.Gold : 0;
            data.playerHealth = playerHealth != null ? playerHealth.Current : 0f;
            data.rvHealth = rvHealth != null ? rvHealth.Current : 0f;

            if (playerInventory != null) SaveMapper.CaptureInventory(playerInventory.Inventory, data);
            if (farm != null) SaveMapper.CapturePlots(farm.Field, data);

            foreach (PlacedObject placed in FindObjectsByType<PlacedObject>(FindObjectsSortMode.None))
            {
                if (placed.Item == null)
                {
                    continue;
                }

                var entry = new PlacedSave
                {
                    item = placed.Item.Id,
                    yaw = placed.transform.eulerAngles.y,
                    posX = placed.transform.position.x,
                    posZ = placed.transform.position.z,
                    health = placed.TryGetComponent(out Health health) ? health.Current : 0f,
                };

                if (placed.World != null && placed.World.Grid.TryGetFootprint(placed.OccupantId, out RectInt rect))
                {
                    entry.onGrid = true;
                    entry.x = rect.xMin;
                    entry.z = rect.yMin;
                    entry.width = rect.width;
                    entry.depth = rect.height;
                }

                data.placed.Add(entry);
            }

            foreach (LootContainer container in FindObjectsByType<LootContainer>(FindObjectsSortMode.None))
            {
                if (container.IsEmpty && !string.IsNullOrEmpty(container.Id))
                {
                    data.emptiedContainers.Add(container.Id);
                }
            }

            return data;
        }

        private void Restore(SaveData data)
        {
            int problems = 0;

            // Primero las parcelas: una planta ocupa su celda, la tierra arada no.
            if (farm != null && crops != null) problems += SaveMapper.RestorePlots(data, farm.Field, crops);

            problems += RestorePlaced(data);

            if (playerInventory != null && items != null)
            {
                problems += SaveMapper.RestoreInventory(data, playerInventory.Inventory, items);
            }

            if (playerWallet != null) playerWallet.Wallet.Restore(data.gold);
            if (playerHealth != null && data.playerHealth > 0f) playerHealth.Restore(data.playerHealth);
            if (rvHealth != null && data.rvHealth > 0f) rvHealth.Restore(data.rvHealth);

            var emptied = new HashSet<string>(data.emptiedContainers);
            foreach (LootContainer container in FindObjectsByType<LootContainer>(FindObjectsSortMode.None))
            {
                if (emptied.Contains(container.Id)) container.MarkEmpty();
            }

            if (problems > 0)
            {
                Debug.LogWarning($"Partida cargada con {problems} elemento(s) que no se pudieron restaurar.");
            }
        }

        private int RestorePlaced(SaveData data)
        {
            // Lo que la escena trae suelto de fábrica (los animales de regalo) se sustituye por
            // lo guardado: puede que el jugador los moviera o que ya no estén.
            foreach (PlacedObject existing in FindObjectsByType<PlacedObject>(FindObjectsSortMode.None))
            {
                if (existing.Item != null && existing.World == null)
                {
                    existing.gameObject.SetActive(false);
                    Destroy(existing.gameObject);
                }
            }

            int problems = 0;
            for (int i = 0; i < data.placed.Count; i++)
            {
                PlacedSave entry = data.placed[i];
                if (items == null || !items.TryGet(entry.item, out ItemDefinition item) || item.PlacedPrefab == null)
                {
                    problems++;
                    continue;
                }

                Quaternion rotation = Quaternion.Euler(0f, entry.yaw, 0f);
                GameObject placed;
                if (entry.onGrid && world != null)
                {
                    placed = world.TryPlace(item, new RectInt(entry.x, entry.z, entry.width, entry.depth), rotation);
                }
                else
                {
                    placed = Instantiate(item.PlacedPrefab, new Vector3(entry.posX, 0f, entry.posZ), rotation);
                    placed.AddComponent<PlacedObject>().ConfigureItem(item);
                }

                if (placed == null)
                {
                    problems++;
                    continue;
                }

                if (entry.health > 0f && placed.TryGetComponent(out Health health))
                {
                    health.Restore(entry.health);
                }
            }

            return problems;
        }

        internal void Configure(GameSession gameSession, DayNightCycle cycle, PhaseEventChannel phaseChannel,
            ItemCatalog itemCatalog, CropCatalog cropCatalog, FarmWorld farmWorld, FarmArea farmArea,
            PlayerInventory inventory, PlayerWallet wallet, Health player, Health rv)
        {
            session = gameSession;
            dayNight = cycle;
            phaseChanged = phaseChannel;
            items = itemCatalog;
            crops = cropCatalog;
            world = farmWorld;
            farm = farmArea;
            playerInventory = inventory;
            playerWallet = wallet;
            playerHealth = player;
            rvHealth = rv;
        }
    }
}
