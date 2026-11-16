using System.Collections.Generic;
using DeadAcres.Building;
using DeadAcres.Defenses;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class PowerCapacityAndTurretTests
    {
        [Test]
        public void Power_ConsumersBeyondCapacity_StayOff_InConnectionOrder()
        {
            var power = new PowerMap();
            power.Add(Vector3.zero, 10f, default, 10f);

            int first = power.AddConsumer(new Vector3(2f, 0f, 0f), 5f);
            int second = power.AddConsumer(new Vector3(3f, 0f, 0f), 4f);
            int third = power.AddConsumer(new Vector3(4f, 0f, 0f), 3f);
            int small = power.AddConsumer(new Vector3(5f, 0f, 0f), 1f);

            Assert.IsTrue(power.IsConsumerPowered(first));
            Assert.IsTrue(power.IsConsumerPowered(second));
            Assert.IsFalse(power.IsConsumerPowered(third), "5 + 4 + 3 no caben en 10.");
            Assert.IsTrue(power.IsConsumerPowered(small), "Pero uno pequeño sí aprovecha lo que sobra.");
            Assert.AreEqual(10f, power.TotalUsed, 0.0001f);
            Assert.AreEqual(10f, power.TotalCapacity, 0.0001f);
        }

        [Test]
        public void Power_RemovingAConsumer_FreesCapacityForTheNext()
        {
            var power = new PowerMap();
            power.Add(Vector3.zero, 10f, default, 6f);
            int first = power.AddConsumer(Vector3.right, 5f);
            int second = power.AddConsumer(Vector3.forward, 5f);

            int changes = 0;
            power.Changed += () => changes++;
            Assert.IsFalse(power.IsConsumerPowered(second));

            power.RemoveConsumer(first);
            Assert.IsTrue(power.IsConsumerPowered(second));
            Assert.AreEqual(1, changes, "Los aparatos se enteran del nuevo reparto.");
            Assert.IsFalse(power.IsConsumerPowered(first), "El que se quitó ya no cuenta.");
        }

        [Test]
        public void Power_AGenerator_TakesTheLoadTheCaravanCannot()
        {
            var power = new PowerMap();
            power.Add(Vector3.zero, 6f, default, 10f);
            int electric = power.AddConsumer(new Vector3(4f, 0f, 0f), 6f);
            int auto = power.AddConsumer(new Vector3(5f, 0f, 0f), 5f);
            Assert.IsFalse(power.IsConsumerPowered(auto));

            int generator = power.Add(new Vector3(8f, 0f, 0f), 9f, default, 20f);
            Assert.IsTrue(power.IsConsumerPowered(electric));
            Assert.IsTrue(power.IsConsumerPowered(auto));
            Assert.AreEqual(30f, power.TotalCapacity, 0.0001f);

            power.Remove(generator);
            Assert.IsFalse(power.IsConsumerPowered(auto), "Sin el generador vuelve a faltar energía.");
        }

        [Test]
        public void Power_OutOfRange_IsOffEvenWithSpareCapacity()
        {
            var power = new PowerMap();
            power.Add(Vector3.zero, 6f, default, 100f);
            Assert.IsFalse(power.IsConsumerPowered(power.AddConsumer(new Vector3(7f, 0f, 0f), 1f)));
            Assert.AreEqual(0f, power.TotalUsed, 0.0001f);
        }

        [Test]
        public void Cone_TakesOnlyWhatIsInFront_NearestFirst_UpToTheLimit()
        {
            var positions = new List<Vector3>
            {
                new Vector3(0f, 0f, 5f),    // 0: justo delante
                new Vector3(1f, 0f, 3f),    // 1: delante, más cerca
                new Vector3(5f, 0f, 1f),    // 2: de lado, fuera del cono
                new Vector3(0f, 0f, -4f),   // 3: detrás
                new Vector3(0f, 0f, 9f),    // 4: delante pero fuera de alcance
                new Vector3(-1f, 0f, 6f),   // 5: delante
            };
            var selected = new List<int>();

            TurretTargeting.SelectCone(Vector3.zero, Vector3.forward, 25f, 8f, positions, 4, selected);
            Assert.AreEqual(3, selected.Count);
            Assert.AreEqual(1, selected[0]);
            Assert.AreEqual(0, selected[1]);
            Assert.AreEqual(5, selected[2]);

            TurretTargeting.SelectCone(Vector3.zero, Vector3.forward, 25f, 8f, positions, 2, selected);
            Assert.AreEqual(2, selected.Count, "El máximo de objetivos manda.");
        }

        [Test]
        public void Chain_JumpsToTheNearestUntouched_WithinRange_AndStops()
        {
            var positions = new List<Vector3>
            {
                new Vector3(0f, 0f, 10f),   // 0: primer objetivo
                new Vector3(3f, 0f, 10f),   // 1: a 3 m del 0
                new Vector3(6f, 0f, 10f),   // 2: a 3 m del 1
                new Vector3(20f, 0f, 10f),  // 3: demasiado lejos de todos
                new Vector3(6f, 0f, 14f),   // 4: a 4 m del 2
                new Vector3(-2f, 0f, 10f),  // 5: a 2 m del 0 (el más cercano al primero)
            };
            var chain = new List<int>();

            TurretTargeting.SelectChain(positions, 0, 4, 5f, chain);
            // 0 → 5 (2 m) → 1 (5 m desde el 5) → 2 → 4. El 3 queda fuera siempre.
            Assert.AreEqual(5, chain.Count);
            Assert.AreEqual(0, chain[0]);
            Assert.AreEqual(5, chain[1]);
            Assert.AreEqual(1, chain[2]);
            Assert.AreEqual(2, chain[3]);
            Assert.AreEqual(4, chain[4]);

            TurretTargeting.SelectChain(positions, 0, 2, 5f, chain);
            Assert.AreEqual(3, chain.Count, "Primer objetivo más dos saltos.");

            TurretTargeting.SelectChain(positions, 3, 4, 5f, chain);
            Assert.AreEqual(1, chain.Count, "Sin nadie cerca no hay saltos.");

            TurretTargeting.SelectChain(positions, -1, 4, 5f, chain);
            Assert.AreEqual(0, chain.Count);
        }

        [Test]
        public void Chain_DamageDropsWithEachJump()
        {
            Assert.AreEqual(45f, TurretTargeting.GetChainDamage(45f, 0.7f, 0), 0.001f);
            Assert.AreEqual(31.5f, TurretTargeting.GetChainDamage(45f, 0.7f, 1), 0.001f);
            Assert.AreEqual(22.05f, TurretTargeting.GetChainDamage(45f, 0.7f, 2), 0.001f);
            Assert.AreEqual(10.8045f, TurretTargeting.GetChainDamage(45f, 0.7f, 4), 0.001f);
        }
    }
}
