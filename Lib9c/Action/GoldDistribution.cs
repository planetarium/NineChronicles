using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Libplanet;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    public class GoldDistribution : IEquatable<GoldDistribution>
    {
        [Ignore]
        public Address Address;

        [Index(0)]
        public string AddressString
        {
            get => Address.ToHex();
            set => Address = new Address(value);
        }

        [Index(1)]
        public BigInteger AmountPerBlock { get; set; }

        [Index(2)]
        public long StartBlock { get; set; }

        [Index(3)]
        public long EndBlock { get; set; }

        public static GoldDistribution[] LoadInDescendingEndBlockOrder(string csvPath)
        {
            GoldDistribution[] records;
#if UNITY_ANDROID
            UnityEngine.WWW www = new UnityEngine.WWW(csvPath);
            while (!www.isDone)
            {
                // wait for data load
            }
            using var stream = new MemoryStream(www.bytes);
            using var streamReader = new StreamReader(stream, System.Text.Encoding.Default);
#else
            using var streamReader = new StreamReader(csvPath);
#endif
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            records = csvReader.GetRecords<GoldDistribution>().ToArray();
            Array.Sort<GoldDistribution>(records, new DescendingEndBlockComparer());
            return records;
        }

        public GoldDistribution()
        {
        }

        public GoldDistribution(IValue serialized)
            : this((Bencodex.Types.Dictionary)serialized)
        {
        }

        public GoldDistribution(Bencodex.Types.Dictionary serialized)
        {
            Address = serialized["addr"].ToAddress();
            AmountPerBlock = serialized["amnt"].ToBigInteger();
            StartBlock = serialized["strt"].ToLong();
            EndBlock = serialized["end"].ToLong();
        }

        public Bencodex.Types.Dictionary Serialize() => Bencodex.Types.Dictionary.Empty
            .Add("addr", Address.Serialize())
            .Add("amnt", AmountPerBlock.Serialize())
            .Add("strt", StartBlock.Serialize())
            .Add("end", EndBlock.Serialize());

        public BigInteger GetAmount(long blockIndex)
        {
            if (StartBlock <= blockIndex && blockIndex <= EndBlock)
            {
                return AmountPerBlock;
            }

            return 0;
        }

        public bool Equals(GoldDistribution other) =>
            Address.Equals(other.Address) &&
            AmountPerBlock.Equals(other.AmountPerBlock) &&
            StartBlock.Equals(other.StartBlock) &&
            EndBlock.Equals(other.EndBlock);

        public override bool Equals(object obj) =>
            obj is GoldDistribution o && Equals(o);

        public override int GetHashCode() =>
            (Address, AmountPerBlock, StartBlock, EndBlock).GetHashCode();

        private class DescendingEndBlockComparer : IComparer<GoldDistribution>
        {
            public int Compare(GoldDistribution x, GoldDistribution y) =>
                y.EndBlock.CompareTo(x.EndBlock);
        }
    }
}
