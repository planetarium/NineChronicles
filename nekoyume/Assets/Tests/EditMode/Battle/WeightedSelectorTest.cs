using System.Linq;
using Nekoyume;
using Nekoyume.Battle;
using NUnit.Framework;

namespace Tests.EditMode.Battle
{
    public class WeightedSelectorTest
    {
        [Test]
        public void Select([Values(1, 2)] int expected)
        {
            var selector = new WeightedSelector<int>(new Cheat.DebugRandom());
            selector.Add(1, 0.5m);
            selector.Add(2, 0.5m);
            Assert.AreEqual(2, selector.Count);
            var result = selector.Select(expected);
            Assert.AreEqual(expected, result.Count());
            Assert.AreEqual(2 - expected, selector.Count);
        }

        [Test]
        public void ValidateThrowException()
        {
            var selector = new WeightedSelector<int>(new Cheat.DebugRandom());
            Assert.Throws<InvalidCountException>(() => selector.Select(0));
            Assert.Throws<ListEmptyException>(() => selector.Select(1));
        }
    }
}
