using DeadAcres.Economy;
using DeadAcres.Enemies;
using DeadAcres.Items;
using NUnit.Framework;

namespace DeadAcres.Tests
{
    public sealed class ClipShopSizeAndGrowthTests
    {
        [Test]
        public void Clip_FiresUntilEmpty_ThenNeedsAReload()
        {
            var clip = new WeaponClip(3, 2f);
            Assert.IsTrue(clip.IsFull);

            Assert.IsTrue(clip.TryFire());
            Assert.IsTrue(clip.TryFire());
            Assert.IsTrue(clip.TryFire());
            Assert.AreEqual(0, clip.Rounds);
            Assert.IsFalse(clip.TryFire(), "Vacío no dispara.");

            Assert.IsTrue(clip.StartReload());
            Assert.IsFalse(clip.StartReload(), "Ya está recargando.");
            Assert.IsFalse(clip.TryFire(), "Recargando no dispara.");

            Assert.IsFalse(clip.Tick(1.9f));
            Assert.AreEqual(0.95f, clip.ReloadProgress, 0.001f);
            Assert.IsTrue(clip.Tick(0.2f), "Avisa justo al terminar.");
            Assert.AreEqual(3, clip.Rounds);
            Assert.IsFalse(clip.IsReloading);
            Assert.IsFalse(clip.Tick(5f));
        }

        [Test]
        public void Clip_FullMagazine_DoesNotReload()
        {
            var clip = new WeaponClip(6, 1f);
            Assert.IsFalse(clip.StartReload());

            clip.TryFire();
            Assert.IsTrue(clip.StartReload(), "Con una bala gastada ya se puede recargar.");
        }

        [Test]
        public void Clip_CancelledReload_KeepsTheRoundsItHad()
        {
            var clip = new WeaponClip(10, 2f);
            for (int i = 0; i < 7; i++) clip.TryFire();

            clip.StartReload();
            clip.Tick(1.5f);
            clip.CancelReload();

            Assert.IsFalse(clip.IsReloading);
            Assert.AreEqual(3, clip.Rounds, "Cambiar de arma a media recarga no regala ni quita balas.");
            Assert.IsTrue(clip.TryFire());

            clip.StartReload();
            Assert.IsFalse(clip.Tick(1.5f), "La recarga empieza de cero.");
            Assert.IsTrue(clip.Tick(0.6f));
            Assert.AreEqual(10, clip.Rounds);
        }

        [Test]
        public void Clip_WithoutMagazine_AlwaysFires()
        {
            var clip = new WeaponClip(0, 3f);
            Assert.IsFalse(clip.HasMagazine);
            for (int i = 0; i < 100; i++) Assert.IsTrue(clip.TryFire());
            Assert.IsFalse(clip.StartReload());
        }

        [TestCase(1, 4)]
        [TestCase(2, 6)]
        [TestCase(3, 9)]
        public void ShopSize_FirstDays_AreFixed(int day, int expected)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                Assert.AreEqual(expected, ShopStock.GetSize(day, seed));
            }
        }

        [Test]
        public void ShopSize_FromDayFour_IsUsuallyFull_SometimesLess_NeverUnderSix()
        {
            int full = 0;
            int smallest = int.MaxValue;
            const int samples = 4000;
            for (int seed = 0; seed < samples; seed++)
            {
                int size = ShopStock.GetSize(4 + seed % 6, seed);
                Assert.IsTrue(size >= ShopStock.MinLateSize && size <= ShopStock.DefaultSize, $"Tamaño {size}");
                if (size == ShopStock.DefaultSize) full++;
                if (size < smallest) smallest = size;
            }

            float fraction = full / (float)samples;
            Assert.IsTrue(fraction > 0.55f && fraction < 0.65f, $"Completo el {fraction:P0} de los días.");
            Assert.AreEqual(ShopStock.MinLateSize, smallest, "El mínimo de 6 llega a salir.");
        }

        [Test]
        public void ShopSize_IsStableWithinADay_AndRespectsASmallerMaximum()
        {
            Assert.AreEqual(ShopStock.GetSize(7, 1234), ShopStock.GetSize(7, 1234));
            Assert.AreEqual(3, ShopStock.GetSize(1, 5, 3));
            Assert.AreEqual(5, ShopStock.GetSize(9, 5, 5));
        }

        [TestCase(1, 1f)]
        [TestCase(2, 1.2f)]
        [TestCase(5, 1.8f)]
        [TestCase(11, 3f)]
        public void NightGrowth_IsLinearFromTheFirstNight(int night, float expected)
        {
            Assert.AreEqual(expected, WaveMath.GetNightGrowth(0.2f, night), 0.0001f);
        }

        [Test]
        public void NightGrowth_NeverShrinks()
        {
            Assert.AreEqual(1f, WaveMath.GetNightGrowth(-0.5f, 9), 0.0001f);
            Assert.AreEqual(1f, WaveMath.GetNightGrowth(0.2f, -3), 0.0001f);
        }
    }
}
