using System;
using Nekoyume;
using Nekoyume.Battle;
using NUnit.Framework;

namespace Tests.EditMode.Battle
{
    public class WeightedSelectorTest
    {
        [Test]
        public void Pop()
        {
            var selector = new WeightedSelector<int>(new Cheat.DebugRandom());
            selector.Add(1, 1m);
            selector.Add(2, 1m);
            Assert.AreEqual(2, selector.Count);
            var result = selector.Pop();
            Assert.AreEqual(1, result);
            Assert.AreEqual(1, selector.Count);
            var result2 = selector.Pop();
            Assert.AreEqual(2, result2);
            Assert.AreEqual(0, selector.Count);
        }

        [Test]
        public void ThrowException()
        {
            var selector = new WeightedSelector<int>(new Cheat.DebugRandom());
            Assert.Throws<ArgumentOutOfRangeException>(() => selector.Pop());
            selector.Add(1, 0m);
            Assert.Throws<ArgumentOutOfRangeException>(() => selector.Pop());
        }
    }
}
