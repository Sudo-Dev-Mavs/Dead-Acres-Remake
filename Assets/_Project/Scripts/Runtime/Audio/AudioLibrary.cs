using UnityEngine;

namespace DeadAcres.Audio
{
    /// <summary>
    /// Los clips del juego, por función. Cambiar un sonido es cambiar el clip aquí (o sustituir
    /// el .wav): el código nunca nombra archivos.
    /// </summary>
    [CreateAssetMenu(menuName = "Dead Acres/Audio Library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Header("Combate")]
        [SerializeField] private AudioClip shot;
        [SerializeField] private AudioClip hit;
        [SerializeField] private AudioClip zombieDeath;
        [SerializeField] private AudioClip hurt;
        [SerializeField] private AudioClip rvHit;
        [SerializeField] private AudioClip reload;
        [SerializeField] private AudioClip turretShot;
        [SerializeField] private AudioClip explosion;
        [SerializeField] private AudioClip step;

        [Header("Granja y construcción")]
        [SerializeField] private AudioClip till;
        [SerializeField] private AudioClip plant;
        [SerializeField] private AudioClip harvest;
        [SerializeField] private AudioClip place;

        [Header("Avisos")]
        [SerializeField] private AudioClip coin;
        [SerializeField] private AudioClip blip;
        [SerializeField] private AudioClip nightStart;
        [SerializeField] private AudioClip dawn;
        [SerializeField] private AudioClip strong;

        [Header("Música (bucles)")]
        [SerializeField] private AudioClip musicDay;
        [SerializeField] private AudioClip musicNight;

        public AudioClip Shot => shot;
        public AudioClip Hit => hit;
        public AudioClip ZombieDeath => zombieDeath;
        public AudioClip Hurt => hurt;
        public AudioClip RvHit => rvHit;
        public AudioClip Reload => reload;
        public AudioClip TurretShot => turretShot;
        public AudioClip Explosion => explosion;
        public AudioClip Step => step;
        public AudioClip Till => till;
        public AudioClip Plant => plant;
        public AudioClip Harvest => harvest;
        public AudioClip Place => place;
        public AudioClip Coin => coin;
        public AudioClip Blip => blip;
        public AudioClip NightStart => nightStart;
        public AudioClip Dawn => dawn;
        public AudioClip Strong => strong;
        public AudioClip MusicDay => musicDay;
        public AudioClip MusicNight => musicNight;

        /// <param name="load">Devuelve el clip con ese nombre de archivo (sin extensión), o null.</param>
        internal void Configure(System.Func<string, AudioClip> load)
        {
            shot = load("shot");
            hit = load("hit");
            zombieDeath = load("zombie_death");
            hurt = load("hurt");
            rvHit = load("rv_hit");
            reload = load("reload");
            turretShot = load("turret_shot");
            explosion = load("explosion");
            step = load("step");
            till = load("till");
            plant = load("plant");
            harvest = load("harvest");
            place = load("place");
            coin = load("coin");
            blip = load("blip");
            nightStart = load("night_start");
            dawn = load("dawn");
            strong = load("strong");
            musicDay = load("music_day");
            musicNight = load("music_night");
        }
    }
}
