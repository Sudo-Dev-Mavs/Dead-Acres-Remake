using System;

namespace DeadAcres.Economy
{
    /// <summary>Oro de un jugador, en C# puro. En co-op lo modifica solo el host.</summary>
    public sealed class Wallet
    {
        public Wallet(int startingGold = 0)
        {
            Gold = Math.Max(0, startingGold);
        }

        /// <summary>Nuevo total de oro.</summary>
        public event Action<int> Changed;

        public int Gold { get; private set; }

        public bool CanAfford(int amount) => amount >= 0 && Gold >= amount;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Gold += amount;
            Changed?.Invoke(Gold);
        }

        /// <summary>Gasta todo o nada.</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || Gold < amount)
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            Gold -= amount;
            Changed?.Invoke(Gold);
            return true;
        }
    }
}
