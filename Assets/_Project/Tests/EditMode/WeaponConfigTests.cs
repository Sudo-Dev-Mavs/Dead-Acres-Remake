using DeadAcres.Items;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class WeaponConfigTests
    {
        [Test]
        public void ConfigureWeapon_KeepsValuesAndClampsNonsense()
        {
            var weapon = ScriptableObject.CreateInstance<ItemDefinition>();
            weapon.Configure("Escopeta", ItemKind.Weapon, 1);

            weapon.ConfigureWeapon(13f, 24f, 0.45f, true, 8, 5.5f);
            Assert.AreEqual(13f, weapon.WeaponDamage, 0.0001f);
            Assert.AreEqual(8, weapon.Pellets);
            Assert.AreEqual(5.5f, weapon.SpreadDegrees, 0.0001f);
            Assert.IsTrue(weapon.IsAutomatic);

            // Volver a configurarla como arma sencilla borra lo anterior: los assets se reutilizan.
            weapon.ConfigureWeapon(20f, 60f, 0.25f);
            Assert.IsFalse(weapon.IsAutomatic);
            Assert.AreEqual(1, weapon.Pellets);
            Assert.AreEqual(0f, weapon.SpreadDegrees, 0.0001f);

            weapon.ConfigureWeapon(-5f, 0f, 0f, false, 0, -3f);
            Assert.AreEqual(0f, weapon.WeaponDamage, 0.0001f);
            Assert.AreEqual(1f, weapon.WeaponRange, 0.0001f);
            Assert.AreEqual(0.02f, weapon.FireInterval, 0.0001f);
            Assert.AreEqual(1, weapon.Pellets);
            Assert.AreEqual(0f, weapon.SpreadDegrees, 0.0001f);
            Object.DestroyImmediate(weapon);
        }
    }
}
