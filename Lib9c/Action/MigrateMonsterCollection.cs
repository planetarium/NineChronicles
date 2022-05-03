using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    /// <summary>
    /// An action to migrate <see cref="MonsterCollectionState"/> into <see cref="StakeState"/>
    /// without cancellation, to keep its staked period.
    /// </summary>
    public class MigrateMonsterCollection : ActionBase
    {
        public override IValue PlainValue => Null.Value;
        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var agentState = states.GetAgentState(context.Signer);
            var currency = states.GetGoldCurrency();
            Address collectionAddress = MonsterCollectionState.DeriveAddress(context.Signer, agentState.MonsterCollectionRound);
            if (!states.TryGetState(collectionAddress, out Dictionary stateDict))
            {
                throw new FailedLoadStateException($"Aborted as the monster collection state failed to load.");
            }

            var monsterCollectionState = new MonsterCollectionState(stateDict);
            var migratedStakeStateAddress = StakeState.DeriveAddress(context.Signer);
            var migratedStakeState = new StakeState(migratedStakeStateAddress, monsterCollectionState.ReceivedBlockIndex);

            return states.SetState(monsterCollectionState.address, Null.Value)
                .SetState(migratedStakeStateAddress, migratedStakeState.SerializeV2())
                .TransferAsset(
                    monsterCollectionState.address,
                    migratedStakeStateAddress,
                    states.GetBalance(monsterCollectionState.address, currency));
        }
    }
}
