using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class StakingState: State
    {
        public static Address DeriveAddress(Address baseAddress, int stakingRound)
        {
            return baseAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeriveFormat,
                    stakingRound
                )
            );
        }

        public const string DeriveFormat = "staking-{0}";
        public const long ExpirationIndex = 16000;

        public int Level { get; private set; }
        public long ExpiredBlockIndex { get; private set; }
        public long StartedBlockIndex { get; private set; }
        public long ReceivedBlockIndex { get; private set; }

        public StakingState(Address address, int level, long blockIndex) : base(address)
        {
            Level = level;
            StartedBlockIndex = blockIndex;
            ExpiredBlockIndex = blockIndex + ExpirationIndex;
        }

        public StakingState(Dictionary serialized) : base(serialized)
        {
            Level = serialized[LevelKey].ToInteger();
            ExpiredBlockIndex = serialized[ExpiredBlockIndexKey].ToLong();
            StartedBlockIndex = serialized[StartedBlockIndexKey].ToLong();
            ReceivedBlockIndex = serialized[ReceivedBlockIndexKey].ToLong();
        }

        public void Update(int level)
        {
            Level = level;
        }

        public override IValue Serialize()
        {
#pragma warning disable LAA1002
            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) LevelKey] = Level.Serialize(),
                [(Text) ExpiredBlockIndexKey] = ExpiredBlockIndex.Serialize(),
                [(Text) StartedBlockIndexKey] = StartedBlockIndex.Serialize(),
                [(Text) ReceivedBlockIndexKey] = ReceivedBlockIndex.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        protected bool Equals(StakingState other)
        {
            return Level == other.Level && ExpiredBlockIndex == other.ExpiredBlockIndex &&
                   StartedBlockIndex == other.StartedBlockIndex && ReceivedBlockIndex == other.ReceivedBlockIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StakingState) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Level;
                hashCode = (hashCode * 397) ^ ExpiredBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ StartedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ ReceivedBlockIndex.GetHashCode();
                return hashCode;
            }
        }
    }
}
