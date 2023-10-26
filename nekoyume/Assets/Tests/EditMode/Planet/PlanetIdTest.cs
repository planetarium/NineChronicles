using System;
using Nekoyume.Planet;
using NUnit.Framework;

namespace Tests.EditMode.Planet
{
    public class PlanetIdTest
    {
        [Test]
        public void Constructor_ToString_ToHexString()
        {
            const string str = "0x000000000000";
            var planetId = new PlanetId(str);
            Assert.AreEqual(str, planetId.ToString());
            Assert.AreEqual(str[2..], planetId.ToHexString());
        }

        [Test]
        public void Constructor_InvalidLength()
        {
            const string str = "0x00000000000";
            Assert.Throws<ArgumentException>(() => new PlanetId(str));
        }

        [Test]
        public void Equals()
        {
            const string str0 = "0x000000000000";
            const string str1 = "0x000000000001";
            var planetId0 = new PlanetId(str0);
            var planetId1 = new PlanetId(str1);
            Assert.True(planetId0.Equals(planetId0));
            Assert.False(planetId0.Equals(planetId1));
            Assert.False(planetId0.Equals(null));
            Assert.False(planetId0.Equals(new object()));
            Assert.False(planetId1.Equals(planetId0));
            Assert.True(planetId1.Equals(planetId1));
        }
    }
}
