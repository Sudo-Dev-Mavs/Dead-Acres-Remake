using System.Collections.Generic;
using DeadAcres.Economy;
using DeadAcres.Items;
using DeadAcres.Mods;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class ModsTests
    {
        [Test]
        public void ItemId_CarriesTheModId_SoTwoModsNeverClash()
        {
            Assert.AreEqual("mod.mi_mod.escopeta_recortada", ModRules.BuildItemId("Mi Mod", "Escopeta-Recortada"));
            Assert.AreNotEqual(ModRules.BuildItemId("mod_a", "rifle"), ModRules.BuildItemId("mod_b", "rifle"));
            Assert.AreEqual("mod.sin_id.sin_id", ModRules.BuildItemId(null, "  "));
        }

        [TestCase("common", ItemRarity.Common)]
        [TestCase("Rare", ItemRarity.Rare)]
        [TestCase("ultrarare", ItemRarity.UltraRare)]
        [TestCase("Ultra Rare", ItemRarity.UltraRare)]
        [TestCase("EPIC", ItemRarity.Epic)]
        [TestCase("legendario", ItemRarity.Legendary)]
        [TestCase("cualquier cosa", ItemRarity.Common)]
        [TestCase(null, ItemRarity.Common)]
        public void Rarity_IsReadLeniently(string text, ItemRarity expected)
        {
            Assert.AreEqual(expected, ModRules.ParseRarity(text));
        }

        [Test]
        public void Validate_AcceptsACompleteWeapon_AndExplainsWhatIsWrongOtherwise()
        {
            var weapon = new ModWeapon { id = "x", name = "X", damage = 10f, price = 100 };
            Assert.IsNull(ModRules.Validate(weapon));

            Assert.IsNotNull(ModRules.Validate(null));
            Assert.IsTrue(ModRules.Validate(new ModWeapon { name = "X" }).Contains("id"));
            Assert.IsTrue(ModRules.Validate(new ModWeapon { id = "x" }).Contains("name"));
            Assert.IsTrue(ModRules.Validate(new ModWeapon { id = "x", name = "X", damage = 0f }).Contains("damage"));
            Assert.IsTrue(ModRules.Validate(new ModWeapon { id = "x", name = "X", price = 0 }).Contains("price"));
        }

        [Test]
        public void RuntimeItems_AreFoundAndSold_WithoutBeingPartOfTheAssets()
        {
            var baseItem = NewItem("weapon_pistol", 0);
            var modItem = NewItem("mod.mi_mod.recortada", 260);

            var items = ScriptableObject.CreateInstance<ItemCatalog>();
            items.Configure(new[] { baseItem });
            var shop = ScriptableObject.CreateInstance<ShopCatalog>();
            shop.Configure(new List<KeyValuePair<ItemDefinition, int>>());

            Assert.IsFalse(items.TryGet("mod.mi_mod.recortada", out _));
            items.RegisterRuntime(modItem);
            shop.RegisterRuntime(modItem);
            shop.RegisterRuntime(modItem);

            Assert.IsTrue(items.TryGet("mod.mi_mod.recortada", out ItemDefinition found));
            Assert.AreSame(modItem, found);
            Assert.IsTrue(items.TryGet("weapon_pistol", out _), "Los del juego siguen ahí.");
            Assert.AreEqual(1, items.Items.Count, "El asset no cambia.");

            var available = new List<ItemDefinition>();
            shop.GetAvailable(1, available);
            Assert.AreEqual(1, available.Count, "Registrarlo dos veces no lo duplica.");
            Assert.IsTrue(shop.TryGetAvailableFromDay(modItem, out int day));
            Assert.AreEqual(1, day);

            // Al empezar otra partida se vuelve a leer todo: lo anterior se olvida.
            items.ClearRuntime();
            shop.ClearRuntime();
            Assert.IsFalse(items.TryGet("mod.mi_mod.recortada", out _));
            available.Clear();
            shop.GetAvailable(1, available);
            Assert.AreEqual(0, available.Count);

            Object.DestroyImmediate(baseItem);
            Object.DestroyImmediate(modItem);
            Object.DestroyImmediate(items);
            Object.DestroyImmediate(shop);
        }

        private static ItemDefinition NewItem(string id, int price)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure(id, ItemKind.Weapon, 1);
            item.ConfigureExtras(id, Color.white);
            item.ConfigurePrices(price, 0);
            return item;
        }
    }
}
