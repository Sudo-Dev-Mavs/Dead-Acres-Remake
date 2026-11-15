using DeadAcres.Items;

namespace DeadAcres.Economy
{
    public enum ShopResult : byte
    {
        Ok = 0,

        /// <summary>Jack no vende ese ítem o no tiene precio de compra.</summary>
        NotForSale = 1,

        /// <summary>Lo venderá, pero a partir de un día posterior.</summary>
        NotAvailableYet = 2,
        NotEnoughGold = 3,
        InventoryFull = 4,

        /// <summary>El jugador no tiene esa cantidad.</summary>
        NothingToSell = 5,

        /// <summary>El ítem no tiene precio de venta.</summary>
        NotSellable = 6,
    }

    /// <summary>
    /// Comprar y vender. Atómicas: o se mueve el oro y el ítem, o no cambia nada.
    /// En co-op el cliente pedirá estas mismas operaciones al host.
    /// </summary>
    public static class ShopCommands
    {
        public static ShopResult CanBuy(ShopCatalog catalog, ItemDefinition item, int count, int dayNumber,
            Wallet wallet, Inventory inventory)
        {
            if (item == null || count <= 0 || item.BuyPrice <= 0
                || !catalog.TryGetAvailableFromDay(item, out int fromDay))
            {
                return ShopResult.NotForSale;
            }

            if (dayNumber < fromDay) return ShopResult.NotAvailableYet;
            if (!wallet.CanAfford(item.BuyPrice * count)) return ShopResult.NotEnoughGold;
            if (!inventory.CanAdd(item, count)) return ShopResult.InventoryFull;
            return ShopResult.Ok;
        }

        public static ShopResult Buy(ShopCatalog catalog, ItemDefinition item, int count, int dayNumber,
            Wallet wallet, Inventory inventory)
        {
            ShopResult result = CanBuy(catalog, item, count, dayNumber, wallet, inventory);
            if (result != ShopResult.Ok)
            {
                return result;
            }

            wallet.TrySpend(item.BuyPrice * count);
            inventory.Add(item, count);
            return ShopResult.Ok;
        }

        /// <summary>Jack compra cualquier ítem con precio de venta, esté o no en su catálogo.</summary>
        public static ShopResult Sell(ItemDefinition item, int count, Wallet wallet, Inventory inventory)
        {
            if (item == null || count <= 0 || inventory.CountOf(item) < count)
            {
                return ShopResult.NothingToSell;
            }

            if (item.SellPrice <= 0)
            {
                return ShopResult.NotSellable;
            }

            inventory.Remove(item, count);
            wallet.Add(item.SellPrice * count);
            return ShopResult.Ok;
        }
    }
}
