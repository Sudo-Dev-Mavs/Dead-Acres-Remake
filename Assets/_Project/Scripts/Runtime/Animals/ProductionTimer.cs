using System;

namespace DeadAcres.Animals
{
    /// <summary>
    /// Cuenta el tiempo hasta que un animal tiene producto listo (huevo, lana). C# puro.
    /// Con el producto listo deja de contar hasta que se recoge.
    /// </summary>
    public sealed class ProductionTimer
    {
        private readonly float secondsPerProduct;
        private float elapsed;

        public ProductionTimer(float secondsPerProduct)
        {
            this.secondsPerProduct = Math.Max(0.1f, secondsPerProduct);
        }

        public event Action BecameReady;

        public bool IsReady { get; private set; }

        /// <summary>0..1 hacia el siguiente producto.</summary>
        public float Progress => IsReady ? 1f : Math.Min(1f, elapsed / secondsPerProduct);

        public void Tick(float deltaTime)
        {
            if (IsReady || deltaTime <= 0f)
            {
                return;
            }

            elapsed += deltaTime;
            if (elapsed >= secondsPerProduct)
            {
                IsReady = true;
                BecameReady?.Invoke();
            }
        }

        /// <returns>False si no había producto que recoger.</returns>
        public bool Collect()
        {
            if (!IsReady)
            {
                return false;
            }

            IsReady = false;
            elapsed = 0f;
            return true;
        }
    }
}
