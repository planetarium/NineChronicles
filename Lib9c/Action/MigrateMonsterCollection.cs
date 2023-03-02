using System;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// An action to claim remained monster collection rewards and to migrate
    /// <see cref="MonsterCollectionState"/> into <see cref="StakeState"/> without cancellation, to
    /// keep its staked period.
    /// </summary>
    [ActionType("migrate_monster_collection")]
    public class MigrateMonsterCollection : ActionBase, IMigrateMonsterCollectionV1
    {
        public Address AvatarAddress { get; private set; }

        Address IMigrateMonsterCollectionV1.AvatarAddress => AvatarAddress;

        public MigrateMonsterCollection(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }

        public MigrateMonsterCollection()
        {
        }

        public override IValue PlainValue =>
            Dictionary.Empty.Add(AvatarAddressKey, AvatarAddress.Serialize());

        public override void LoadPlainValue(IValue plainValue)
        {
            var dictionary = (Dictionary)plainValue;
            AvatarAddress = dictionary[AvatarAddressKey].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}MigrateMonsterCollection exec started", addressesHex);
            if (states.TryGetStakeState(context.Signer, out StakeState _))
            {
                throw new InvalidOperationException("The user has already staked.");
            }

            try
            {
                var claimMonsterCollectionReward = new ClaimMonsterCollectionReward
                {
                    avatarAddress = AvatarAddress,
                };
                states = claimMonsterCollectionReward.Execute(context);
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "An exception({Exception}) occurred while claiming monster collection rewards.",
                    e);
            }

            var agentState = states.GetAgentState(context.Signer);
            var currency = states.GetGoldCurrency();

            Address collectionAddress = MonsterCollectionState.DeriveAddress(context.Signer, agentState.MonsterCollectionRound);
            if (!states.TryGetState(collectionAddress, out Dictionary stateDict))
            {
                throw new FailedLoadStateException($"Aborted as the monster collection state failed to load.");
            }

            var monsterCollectionState = new MonsterCollectionState(stateDict);
            var migratedStakeStateAddress = StakeState.DeriveAddress(context.Signer);
            var migratedStakeState = new StakeState(
                migratedStakeStateAddress,
                monsterCollectionState.StartedBlockIndex,
                monsterCollectionState.ReceivedBlockIndex,
                monsterCollectionState.ExpiredBlockIndex,
                new StakeState.StakeAchievements());

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}MigrateMonsterCollection Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states.SetState(monsterCollectionState.address, Null.Value)
                .SetState(migratedStakeStateAddress, migratedStakeState.SerializeV2())
                .TransferAsset(
                    monsterCollectionState.address,
                    migratedStakeStateAddress,
                    states.GetBalance(monsterCollectionState.address, currency));
        }
    }
}
