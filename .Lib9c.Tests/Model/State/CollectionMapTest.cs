namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Nekoyume.Model;
    using Xunit;

    public class CollectionMapTest
    {
        [Fact]
        public void Serialize()
        {
            var map = new CollectionMap();
            map.Add(1, 1);
            map.Add(2, 1);

            var map2 = new CollectionMap();
            map2.Add(2, 1);
            map2.Add(1, 1);

            var deserialized = new CollectionMap((Dictionary)map2.Serialize());
            Assert.Equal(deserialized, map);
        }

        [Fact]
        public void GetEnumerator()
        {
            const int size = 10;
            var map = new CollectionMap();
            for (var i = 0; i < size; i++)
            {
                map.Add(size - i, i);
            }

            var idx = 1;
            foreach (var (key, _) in map)
            {
                Assert.Equal(idx, key);
                idx++;
            }
        }
    }
}
