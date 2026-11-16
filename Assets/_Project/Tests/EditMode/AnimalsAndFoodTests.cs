using DeadAcres.Animals;
using DeadAcres.Items;
using DeadAcres.UI;
using NUnit.Framework;
using UnityEngine;

namespace DeadAcres.Tests
{
    public sealed class AnimalsAndFoodTests
    {
        [Test]
        public void ProductionTimer_BecomesReadyOnceAfterItsTime()
        {
            var timer = new ProductionTimer(10f);
            int readyEvents = 0;
            timer.BecameReady += () => readyEvents++;

            timer.Tick(6f);
            Assert.IsFalse(timer.IsReady);
            Assert.AreEqual(0.6f, timer.Progress, 0.0001f);

            timer.Tick(6f);
            timer.Tick(50f);
            Assert.IsTrue(timer.IsReady);
            Assert.AreEqual(1, readyEvents, "Con el producto listo no vuelve a avisar ni acumula más.");
            Assert.AreEqual(1f, timer.Progress, 0.0001f);
        }

        [Test]
        public void ProductionTimer_CollectRestartsTheCount()
        {
            var timer = new ProductionTimer(10f);
            Assert.IsFalse(timer.Collect(), "Sin producto no hay nada que recoger.");

            timer.Tick(30f);
            Assert.IsTrue(timer.Collect());
            Assert.IsFalse(timer.IsReady);
            Assert.AreEqual(0f, timer.Progress, 0.0001f, "El tiempo de sobra no se guarda para el siguiente.");

            timer.Tick(10f);
            Assert.IsTrue(timer.IsReady);
        }

        [Test]
        public void ProductionTimer_IgnoresNegativeTime()
        {
            var timer = new ProductionTimer(10f);
            timer.Tick(-100f);
            Assert.AreEqual(0f, timer.Progress, 0.0001f);
        }

        [Test]
        public void HealAmount_IsZeroUnlessConfiguredAndNeverNegative()
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure("Fresa", ItemKind.Produce, 20);
            Assert.AreEqual(0f, item.HealAmount, 0.0001f);

            item.ConfigureHeal(4f);
            Assert.AreEqual(4f, item.HealAmount, 0.0001f);

            item.ConfigureHeal(-3f);
            Assert.AreEqual(0f, item.HealAmount, 0.0001f);
            Object.DestroyImmediate(item);
        }

        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(12, 1)]
        [TestCase(13, 2)]
        [TestCase(24, 2)]
        [TestCase(25, 3)]
        public void ListPaging_CountsPages(int itemCount, int expectedPages)
        {
            Assert.AreEqual(expectedPages, ListPaging.GetPageCount(itemCount, 12));
        }

        [Test]
        public void ListPaging_WrapsToTheFirstPagePastTheEnd()
        {
            Assert.AreEqual(1, ListPaging.WrapPage(1, 25, 12));
            Assert.AreEqual(2, ListPaging.WrapPage(2, 25, 12));
            Assert.AreEqual(0, ListPaging.WrapPage(3, 25, 12), "Tras la última página vuelve a la primera.");
            Assert.AreEqual(0, ListPaging.WrapPage(1, 5, 12), "Si la lista se acorta, la página deja de existir.");
            Assert.AreEqual(0, ListPaging.WrapPage(-1, 25, 12));
        }
    }
}
