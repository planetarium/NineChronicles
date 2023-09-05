using System;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stake
{
    public readonly struct StakeStateV2 : IState
    {
        public const string StateTypeName = "stake_state";
        public const int StateTypeVersion = 2;

        public static Address DeriveAddress(Address address) =>
            StakeState.DeriveAddress(address);

        public readonly Contract Contract;
        public readonly long StartedBlockIndex;
        public readonly long ReceivedBlockIndex;

        public long CancellableBlockIndex =>
            StartedBlockIndex + Contract.LockupInterval;

        public long ClaimedBlockIndex => ReceivedBlockIndex == 0
            ? StartedBlockIndex
            : StartedBlockIndex + Math.DivRem(
                ReceivedBlockIndex - StartedBlockIndex,
                Contract.RewardInterval,
                out _
            ) * Contract.RewardInterval;

        public long ClaimableBlockIndex =>
            ClaimedBlockIndex + Contract.RewardInterval;

        public StakeStateV2(
            Contract contract,
            long startedBlockIndex,
            long receivedBlockIndex = 0)
        {
            if (startedBlockIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startedBlockIndex),
                    startedBlockIndex,
                    "startedBlockIndex should be greater than or equal to 0.");
            }

            if (receivedBlockIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(receivedBlockIndex),
                    receivedBlockIndex,
                    "receivedBlockIndex should be greater than or equal to 0.");
            }

            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            StartedBlockIndex = startedBlockIndex;
            ReceivedBlockIndex = receivedBlockIndex;
        }

        public StakeStateV2(
            StakeState stakeState,
            Contract contract
        ) : this(
            contract,
            stakeState?.StartedBlockIndex ?? throw new ArgumentNullException(nameof(stakeState)),
            stakeState.ReceivedBlockIndex
        )
        {
        }

        public StakeStateV2(IValue serialized)
        {
            if (serialized is not List list)
            {
                throw new ArgumentException(
                    nameof(serialized),
                    $"{nameof(serialized)} should be List type.");
            }

            if (list[0] is not Text stateTypeNameValue ||
                stateTypeNameValue != StateTypeName ||
                list[1] is not Integer stateTypeVersionValue ||
                stateTypeVersionValue.Value != StateTypeVersion)
            {
                throw new ArgumentException(
                    nameof(serialized),
                    $"{nameof(serialized)} doesn't have valid header.");
            }

            const int reservedCount = 2;

            Contract = new Contract(list[reservedCount]);
            StartedBlockIndex = (Integer)list[reservedCount + 1];
            ReceivedBlockIndex = (Integer)list[reservedCount + 2];
        }

        public IValue Serialize() => new List(
            (Text)StateTypeName,
            (Integer)StateTypeVersion,
            Contract.Serialize(),
            (Integer)StartedBlockIndex,
            (Integer)ReceivedBlockIndex
        );

        public bool Equals(StakeStateV2 other)
        {
            return Equals(Contract, other.Contract) &&
                   StartedBlockIndex == other.StartedBlockIndex &&
                   ReceivedBlockIndex == other.ReceivedBlockIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is StakeStateV2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Contract != null ? Contract.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ StartedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ ReceivedBlockIndex.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(StakeStateV2 left, StakeStateV2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StakeStateV2 left, StakeStateV2 right)
        {
            return !(left == right);
        }
    }
}
