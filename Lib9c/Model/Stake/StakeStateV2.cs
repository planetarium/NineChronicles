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
            long startedBlockIndex)
            : this(contract, startedBlockIndex, 0)
        {
        }

        public StakeStateV2(
            Contract contract,
            long startedBlockIndex,
            long receivedBlockIndex)
        {
            Contract = contract;
            StartedBlockIndex = startedBlockIndex;
            ReceivedBlockIndex = receivedBlockIndex;
        }

        public StakeStateV2(
            StakeState stakeState,
            Contract contract
        ) : this(
            contract,
            stakeState.StartedBlockIndex,
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
    }
}
