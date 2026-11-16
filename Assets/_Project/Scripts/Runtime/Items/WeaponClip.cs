using System;

namespace DeadAcres.Items
{
    /// <summary>
    /// El cargador de un arma: cuántas balas le quedan y cuánto falta para terminar de recargar.
    /// La munición total es infinita; lo que se acaba es el cargador. C# puro.
    /// </summary>
    public sealed class WeaponClip
    {
        private readonly float reloadSeconds;
        private float reloadElapsed;

        /// <param name="size">Balas por cargador. 0 o menos: sin cargador, dispara siempre.</param>
        public WeaponClip(int size, float reloadSeconds)
        {
            Size = Math.Max(0, size);
            this.reloadSeconds = Math.Max(0f, reloadSeconds);
            Rounds = Size;
        }

        public int Size { get; }
        public int Rounds { get; private set; }
        public bool IsReloading { get; private set; }
        public bool HasMagazine => Size > 0;
        public bool IsFull => !HasMagazine || Rounds >= Size;

        /// <summary>0..1 de la recarga en curso; 0 si no está recargando.</summary>
        public float ReloadProgress =>
            !IsReloading ? 0f : reloadSeconds <= 0f ? 1f : Math.Min(1f, reloadElapsed / reloadSeconds);

        /// <summary>Gasta una bala. False si el cargador está vacío o se está recargando.</summary>
        public bool TryFire()
        {
            if (!HasMagazine)
            {
                return true;
            }

            if (IsReloading || Rounds <= 0)
            {
                return false;
            }

            Rounds--;
            return true;
        }

        /// <returns>False si no hace falta (lleno, sin cargador) o ya se estaba recargando.</returns>
        public bool StartReload()
        {
            if (!HasMagazine || IsReloading || IsFull)
            {
                return false;
            }

            IsReloading = true;
            reloadElapsed = 0f;
            return true;
        }

        /// <summary>Al cambiar de arma a media recarga: se pierde lo recargado, no las balas que quedaban.</summary>
        public void CancelReload()
        {
            IsReloading = false;
            reloadElapsed = 0f;
        }

        /// <returns>True en el instante en que termina la recarga.</returns>
        public bool Tick(float deltaTime)
        {
            if (!IsReloading || deltaTime <= 0f)
            {
                return false;
            }

            reloadElapsed += deltaTime;
            if (reloadElapsed < reloadSeconds)
            {
                return false;
            }

            IsReloading = false;
            reloadElapsed = 0f;
            Rounds = Size;
            return true;
        }
    }
}
