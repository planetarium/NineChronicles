namespace Lib9c.Tests.Types
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
    using Xunit;

    public class BencodexTypesListTest
    {
        private readonly Bencodex.Types.List _integerList;
        private readonly Bencodex.Types.List _descendingIntegerList;

        private readonly Bencodex.Types.List _addressList;
        private readonly Bencodex.Types.List _descendingAddressList;

        public BencodexTypesListTest()
        {
            _integerList = new Bencodex.Types.List(new List<IValue>
            {
                1.Serialize(),
                2.Serialize(),
                3.Serialize(),
            });
            _descendingIntegerList = (Bencodex.Types.List)_integerList
                .OrderByDescending(element => element.ToInteger())
                .Serialize();

            _addressList = new Bencodex.Types.List(new List<IValue>
            {
                new PrivateKey().ToAddress().Serialize(),
                new PrivateKey().ToAddress().Serialize(),
                new PrivateKey().ToAddress().Serialize(),
            });
            _descendingAddressList = (Bencodex.Types.List)_addressList
                .OrderByDescending(element => element.ToAddress())
                .Serialize();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        private void DeterministicWhenSerializationWithInteger(int caseIndex)
        {
            Bencodex.Types.List targetList = Bencodex.Types.List.Empty;
            switch (caseIndex)
            {
                case 0:
                    targetList = _integerList;
                    break;
                case 1:
                    targetList = _descendingIntegerList;
                    break;
            }

            var deserializedList = targetList.ToList(element => element.ToInteger());
            var newList = (Bencodex.Types.List)deserializedList
                .Select(element => element.Serialize())
                .Serialize();

            Assert.Equal(targetList.Count, deserializedList.Count);
            Assert.Equal(targetList.Count, newList.Count);
            for (var i = 0; i < targetList.Count; i++)
            {
                Assert.Equal(targetList[i].ToInteger(), deserializedList[i]);
                Assert.Equal(targetList[i], newList[i]);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        private void DeterministicWhenSerializationWithAddress(int caseIndex)
        {
            Bencodex.Types.List targetList = Bencodex.Types.List.Empty;
            switch (caseIndex)
            {
                case 0:
                    targetList = _addressList;
                    break;
                case 1:
                    targetList = _descendingAddressList;
                    break;
            }

            var deserializedList = targetList.ToList(element => element.ToAddress());
            var newList = (Bencodex.Types.List)deserializedList
                .Select(element => element.Serialize())
                .Serialize();

            Assert.Equal(targetList.Count, deserializedList.Count);
            Assert.Equal(targetList.Count, newList.Count);
            for (var i = 0; i < targetList.Count; i++)
            {
                Assert.Equal(targetList[i].ToAddress(), deserializedList[i]);
                Assert.Equal(targetList[i], newList[i]);
            }
        }

        [Fact]
        private void DeterministicWhenConvertToImmutableDictionaryWithInteger()
        {
            var deserializedDict = _integerList.ToImmutableDictionary(
                element => element.ToInteger(),
                element => element.ToInteger());
            var newList = (Bencodex.Types.List)deserializedDict
                .Select(pair => pair.Value.Serialize())
                .Serialize();

            Assert.Equal(_integerList.Count, deserializedDict.Count);
            Assert.Equal(_integerList.Count, newList.Count);
            for (var i = 0; i < _integerList.Count; i++)
            {
                var deserializedValue = _integerList[i].ToInteger();
                Assert.Equal(deserializedValue, deserializedDict[deserializedValue]);
                Assert.Equal(_integerList[i], newList[i]);
            }
        }
    }
}
