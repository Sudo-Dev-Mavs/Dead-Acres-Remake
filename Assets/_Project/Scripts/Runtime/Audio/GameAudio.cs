using DeadAcres.Building;
using DeadAcres.Combat;
using DeadAcres.DayNight;
using DeadAcres.Economy;
using DeadAcres.Enemies;
using DeadAcres.Items;
using DeadAcres.Player;
using DeadAcres.Settings;
using UnityEngine;

namespace DeadAcres.Audio
{
    /// <summary>
    /// Pone sonido a la partida escuchando los eventos que ya existen: no hay llamadas a "reproduce
    /// tal sonido" repartidas por el juego. Todo suena en 2D (sin posición) por ahora.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField] private AudioLibrary library;
        [SerializeField] private PhaseEventChannel phaseChanged;
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerBuildTools buildTools;
        [SerializeField] private PlayerWallet playerWallet;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Health rvHealth;
        [SerializeField] private WaveDirector waves;
        [SerializeField] private FarmWorld world;

        [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
        [SerializeField, Min(0.1f)] private float musicFadeSeconds = 3f;

        [Tooltip("Durante este tiempo desde que carga la escena no suenan monedas ni objetos: al cargar una partida se restauran de golpe.")]
        [SerializeField, Min(0f)] private float quietStartSeconds = 1f;

        private AudioSource effects;
        private AudioSource musicDay;
        private AudioSource musicNight;
        private readonly AudioSource[] worldSources = new AudioSource[8];
        private int nextWorldSource;
        private Wallet wallet;
        private bool nightMusic;
        private float dayFade = 1f;
        private float lastPlayerHealth;
        private float lastRvHealth;
        private float nextRvHitTime;

        private void Awake()
        {
            GameSettings.Apply();
            effects = CreateSource("Effects", null, false);
            musicDay = CreateSource("Music (day)", library != null ? library.MusicDay : null, true);
            musicNight = CreateSource("Music (night)", library != null ? library.MusicNight : null, true);
            for (int i = 0; i < worldSources.Length; i++)
            {
                worldSources[i] = CreateSource($"World {i}", null, false);
                WorldSound.Configure(worldSources[i]);
            }
        }

        private void OnEnable()
        {
            if (phaseChanged != null) phaseChanged.Subscribe(OnPhaseChanged);
            if (weapon != null)
            {
                weapon.Fired += OnFired;
                weapon.ReloadStarted += OnReloadStarted;
                if (weapon.TryGetComponent(out FirstPersonController walker)) walker.Stepped += OnStepped;
            }
            if (interactor != null) interactor.ActionPerformed += OnFarmAction;
            if (playerInventory != null) playerInventory.Notice += OnNotice;
            if (buildTools != null) buildTools.Message += OnNotice;
            if (waves != null)
            {
                waves.ZombieKilled += OnZombieKilled;
                waves.StrongIncoming += OnStrongIncoming;
            }

            if (playerHealth != null) playerHealth.HealthChanged += OnPlayerHealthChanged;
            if (rvHealth != null) rvHealth.HealthChanged += OnRvHealthChanged;
        }

        // En Start: la cartera y la rejilla los crean otros componentes al despertar.
        private void Start()
        {
            wallet = playerWallet != null ? playerWallet.Wallet : null;
            if (wallet != null) wallet.Changed += OnGoldChanged;
            if (world != null) world.Placed += OnPlaced;

            lastPlayerHealth = playerHealth != null ? playerHealth.Current : 0f;
            lastRvHealth = rvHealth != null ? rvHealth.Current : 0f;
        }

        private void OnDisable()
        {
            if (phaseChanged != null) phaseChanged.Unsubscribe(OnPhaseChanged);
            if (weapon != null)
            {
                weapon.Fired -= OnFired;
                weapon.ReloadStarted -= OnReloadStarted;
                if (weapon.TryGetComponent(out FirstPersonController walker)) walker.Stepped -= OnStepped;
            }
            if (interactor != null) interactor.ActionPerformed -= OnFarmAction;
            if (playerInventory != null) playerInventory.Notice -= OnNotice;
            if (buildTools != null) buildTools.Message -= OnNotice;
            if (waves != null)
            {
                waves.ZombieKilled -= OnZombieKilled;
                waves.StrongIncoming -= OnStrongIncoming;
            }

            if (playerHealth != null) playerHealth.HealthChanged -= OnPlayerHealthChanged;
            if (rvHealth != null) rvHealth.HealthChanged -= OnRvHealthChanged;
            if (wallet != null) wallet.Changed -= OnGoldChanged;
            if (world != null) world.Placed -= OnPlaced;
            wallet = null;
        }

        // El fundido de la música es continuo; con tiempo real para que siga en pausa.
        private void Update()
        {
            // El volumen de música de las opciones se nota al momento, no al final del fundido.
            float target = musicVolume * GameSettings.MusicVolume;
            dayFade = Mathf.MoveTowards(dayFade, nightMusic ? 0f : 1f, Time.unscaledDeltaTime / musicFadeSeconds);
            musicDay.volume = target * dayFade;
            musicNight.volume = target * (1f - dayFade);
        }

        private void OnPhaseChanged(PhaseChange change)
        {
            if (library == null)
            {
                return;
            }

            if (change.Phase == GamePhase.Night)
            {
                nightMusic = true;
                Play(library.NightStart);
            }
            else if (change.Phase == GamePhase.Dawn)
            {
                nightMusic = false;

                // El primer amanecer es el arranque de la partida: sin fanfarria.
                if (!IsQuietStart) Play(library.Dawn);
            }
        }

        private bool IsQuietStart => Time.timeSinceLevelLoad < quietStartSeconds;

        private void OnFired(bool hitEnemy)
        {
            Play(library != null ? library.Shot : null);
            if (hitEnemy) Play(library != null ? library.Hit : null, 0.7f);
        }

        private void OnFarmAction(PrimaryActionKind kind)
        {
            if (library == null) return;

            switch (kind)
            {
                case PrimaryActionKind.Till: Play(library.Till); break;
                case PrimaryActionKind.Plant: Play(library.Plant); break;
                case PrimaryActionKind.Harvest: Play(library.Harvest); break;
            }
        }

        private void OnNotice(string _) => Play(library != null ? library.Blip : null, 0.6f);

        private void OnZombieKilled(Vector3 position) => PlayAt(library != null ? library.ZombieDeath : null, position);

        private void OnStepped() => Play(library != null ? library.Step : null, 0.35f);

        private void OnReloadStarted() => Play(library != null ? library.Reload : null, 0.7f);

        private void OnStrongIncoming(int _) => Play(library != null ? library.Strong : null);

        private void OnGoldChanged(int _)
        {
            if (!IsQuietStart) Play(library != null ? library.Coin : null, 0.7f);
        }

        private void OnPlaced(GameObject _)
        {
            if (!IsQuietStart) Play(library != null ? library.Place : null);
        }

        private void OnPlayerHealthChanged(float current, float max)
        {
            if (current < lastPlayerHealth && !IsQuietStart) Play(library != null ? library.Hurt : null);
            lastPlayerHealth = current;
        }

        private void OnRvHealthChanged(float current, float max)
        {
            // Con varios zombis pegando a la vez sonaría como una ametralladora: se espacia.
            if (current < lastRvHealth && !IsQuietStart && Time.time >= nextRvHitTime)
            {
                nextRvHitTime = Time.time + 0.35f;
                PlayAt(library != null ? library.RvHit : null, rvHealth.transform.position);
            }

            lastRvHealth = current;
        }

        private void Play(AudioClip clip, float volumeScale = 1f)
        {
            if (clip != null) effects.PlayOneShot(clip, effectsVolume * GameSettings.EffectsVolume * volumeScale);
        }

        // Sonido con posición: se oye más flojo y hacia su lado cuanto más lejos esté.
        private void PlayAt(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource source = worldSources[nextWorldSource];
            nextWorldSource = (nextWorldSource + 1) % worldSources.Length;
            source.transform.position = position;
            source.PlayOneShot(clip, effectsVolume * GameSettings.EffectsVolume * volumeScale);
        }

        private AudioSource CreateSource(string sourceName, AudioClip loopClip, bool isMusic)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);

            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            if (isMusic)
            {
                source.clip = loopClip;
                source.loop = true;
                source.volume = 0f;

                // La música no se corta ni cambia de tono con la pausa.
                source.ignoreListenerPause = true;
                if (loopClip != null) source.Play();
            }

            return source;
        }

        internal void Configure(AudioLibrary audioLibrary, PhaseEventChannel phaseChannel, PlayerWeapon playerWeapon,
            PlayerInteractor playerInteractor, PlayerInventory inventory, PlayerBuildTools tools, PlayerWallet walletOwner,
            Health player, Health rv, WaveDirector waveDirector, FarmWorld farmWorld)
        {
            library = audioLibrary;
            phaseChanged = phaseChannel;
            weapon = playerWeapon;
            interactor = playerInteractor;
            playerInventory = inventory;
            buildTools = tools;
            playerWallet = walletOwner;
            playerHealth = player;
            rvHealth = rv;
            waves = waveDirector;
            world = farmWorld;
        }
    }
}
