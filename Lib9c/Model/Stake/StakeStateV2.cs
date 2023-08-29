using System;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stake
{
    public readonly struct StakeStateV2 : IState
    {
        public const string StateTypeName = "stake_state";
        public const int StateTypeVersion = 2;

        public readonly Contract Contract;
        public readonly long StartedBlockIndex;
        public readonly long ClaimedBlockIndex;

        public long CancellableBlockIndex =>
            StartedBlockIndex + StakeState.LockupInterval;
        public long ClaimableBlockIndex =>
            ClaimedBlockIndex + StakeState.RewardInterval;

        public StakeStateV2(
            Contract contract,
            long startedBlockIndex)
            : this(contract, startedBlockIndex, startedBlockIndex)
        {
        }

        public StakeStateV2(
            Contract contract,
            long startedBlockIndex,
            long claimedBlockIndex)
        {
            Contract = contract;
            StartedBlockIndex = startedBlockIndex;
            ClaimedBlockIndex = claimedBlockIndex;
        }

        public StakeStateV2(
            StakeState stakeState,
            Contract contract
        ) : this(
            contract,
            stakeState.StartedBlockIndex,
            stakeState.GetClaimableBlockIndex(long.MaxValue) - StakeState.RewardInterval
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
            ClaimedBlockIndex = (Integer)list[reservedCount + 2];
        }

        public IValue Serialize() => new List(
            (Text)StateTypeName,
            (Integer)StateTypeVersion,
            Contract.Serialize(),
            (Integer)StartedBlockIndex,
            (Integer)ClaimedBlockIndex
        );
    }
}
