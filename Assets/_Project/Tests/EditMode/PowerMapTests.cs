using DeadAcres.Building;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class PowerMapTests
    {
        [Test]
        public void IsPowered_InsideAnySourceRadius_MeasuredHorizontally()
        {
            var map = new PowerMap();
            Assert.IsFalse(map.IsPowered(Vector3.zero), "Sin fuentes no hay energía.");

            map.Add(new Vector3(0f, 0f, 0f), 10f);
            map.Add(new Vector3(30f, 0f, 0f), 5f);

            Assert.IsTrue(map.IsPowered(new Vector3(6f, 0f, 8f)), "Justo en el borde (distancia 10).");
            Assert.IsFalse(map.IsPowered(new Vector3(6.1f, 0f, 8f)));
            Assert.IsTrue(map.IsPowered(new Vector3(3f, 50f, 0f)), "La altura no cuenta.");
            Assert.IsTrue(map.IsPowered(new Vector3(33f, 0f, 0f)));
            Assert.IsFalse(map.IsPowered(new Vector3(20f, 0f, 0f)), "Entre las dos, fuera de ambas.");
        }

        [Test]
        public void Remove_TakesPowerAway_AndNotifies()
        {
            var map = new PowerMap();
            int changes = 0;
            map.Changed += () => changes++;

            int generator = map.Add(new Vector3(5f, 0f, 5f), 9f);
            Assert.IsTrue(map.IsPowered(new Vector3(5f, 0f, 10f)));

            Assert.IsTrue(map.Remove(generator));
            Assert.IsFalse(map.Remove(generator), "Ya no existe.");

            Assert.IsFalse(map.IsPowered(new Vector3(5f, 0f, 10f)));
            Assert.AreEqual(0, map.SourceCount);
            Assert.AreEqual(2, changes, "Un aviso al añadir y otro al quitar.");
        }
    }
}
