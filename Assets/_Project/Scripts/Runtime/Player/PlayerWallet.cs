using DeadAcres.Economy;
using UnityEngine;

namespace DeadAcres.Player
{
    /// <summary>Oro del jugador en la escena.</summary>
    public sealed class PlayerWallet : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingGold;

        private Wallet wallet;

        /// <summary>Se crea al primer uso: el HUD puede pedirlo antes de que se ejecute Awake aquí.</summary>
        public Wallet Wallet
        {
            get
            {
                if (wallet == null)
                {
                    wallet = new Wallet(startingGold);
                }

                return wallet;
            }
        }

        internal void Configure(int gold)
        {
            startingGold = Mathf.Max(0, gold);
            wallet = null;
        }
    }
}
