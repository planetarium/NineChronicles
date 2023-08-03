namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Exceptions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class PetEnhancement0Test
    {
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV1;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV2;
        private readonly int _targetPetId;
        private readonly long _firstRoundStartBlockIndex;

        public PetEnhancement0Test()
        {
            TableSheets tableSheets;
            (
                tableSheets,
                _agentAddr,
                _avatarAddr,
                _initialStatesWithAvatarStateV1,
                _initialStatesWithAvatarStateV2
            ) = InitializeUtil.InitializeStates();
            _targetPetId = tableSheets.PetSheet.First!.Id;
            var firstRound = tableSheets.ArenaSheet.OrderedList!
                .SelectMany(row => row.Round)
                .MinBy(round => round.StartBlockIndex);
            Assert.NotNull(firstRound);
            _firstRoundStartBlockIndex = firstRound.StartBlockIndex;
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(1, 10)]
        public void Execute_Success(
            int currentPetLevel,
            int targetPetLevel)
        {
            Execute(
                _initialStatesWithAvatarStateV1,
                _firstRoundStartBlockIndex,
                _agentAddr,
                _avatarAddr,
                _targetPetId,
                currentPetLevel,
                targetPetLevel);
            Execute(
                _initialStatesWithAvatarStateV2,
                _firstRoundStartBlockIndex,
                _agentAddr,
                _avatarAddr,
                _targetPetId,
                currentPetLevel,
                targetPetLevel);
        }

        [Fact]
        public void Execute_Throw_InvalidActionFieldException_AgentAddress()
        {
            var invalidAgentAddr = new PrivateKey().ToAddress();
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV1,
                    _firstRoundStartBlockIndex,
                    invalidAgentAddr,
                    _avatarAddr,
                    _targetPetId,
                    0,
                    1));
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV2,
                    _firstRoundStartBlockIndex,
                    invalidAgentAddr,
                    _avatarAddr,
                    _targetPetId,
                    0,
                    1));
        }

        [Fact]
        public void Execute_Throw_InvalidActionFieldException_AvatarAddress()
        {
            var invalidAvatarAddr = new PrivateKey().ToAddress();
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV1,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    invalidAvatarAddr,
                    _targetPetId,
                    0,
                    1));
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV2,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    invalidAvatarAddr,
                    _targetPetId,
                    0,
                    1));
        }

        [Theory]
        [InlineData(0, int.MinValue)]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(int.MaxValue, int.MaxValue)]
        public void Execute_Throw_InvalidActionFieldException_PetLevel(
            int currentPetLevel,
            int targetPetLevel)
        {
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV1,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    currentPetLevel,
                    targetPetLevel));
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV2,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    currentPetLevel,
                    targetPetLevel));
        }

        [Fact]
        public void Execute_Throw_SheetRowNotFoundException()
        {
            // PetSheet
            Assert.Throws<SheetRowNotFoundException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV1,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    0,
                    1,
                    removePetRow: true));
            Assert.Throws<SheetRowNotFoundException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV2,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    0,
                    1,
                    removePetRow: true));

            // PetCostSheet
            Assert.Throws<SheetRowNotFoundException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV1,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    0,
                    1,
                    removePetCostRow: true));
            Assert.Throws<SheetRowNotFoundException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV2,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    0,
                    1,
                    removePetCostRow: true));
        }

        [Fact]
        public void Execute_Throw_PetCostNotFoundException()
        {
            const int targetPetLevel = 1;

            Assert.Throws<PetCostNotFoundException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV1,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    0,
                    targetPetLevel,
                    removePetCostRowWithTargetPetLevel: true));
            Assert.Throws<PetCostNotFoundException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV2,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    0,
                    targetPetLevel,
                    removePetCostRowWithTargetPetLevel: true));
        }

        [Fact]
        public void Execute_Throw_RoundNotFoundException()
        {
            Assert.Throws<RoundNotFoundException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV1,
                    _firstRoundStartBlockIndex - 1,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    0,
                    1));
            Assert.Throws<RoundNotFoundException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV2,
                    _firstRoundStartBlockIndex - 1,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    0,
                    1));
        }

        [Theory]
        [InlineData(0, 1)]
        public void Execute_Throw_NotEnoughFungibleAssetValueException(
            int currentPetLevel,
            int targetPetLevel)
        {
            Assert.Throws<NotEnoughFungibleAssetValueException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV1,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    currentPetLevel,
                    targetPetLevel,
                    mintAssets: false));
            Assert.Throws<NotEnoughFungibleAssetValueException>(() =>
                Execute(
                    _initialStatesWithAvatarStateV2,
                    _firstRoundStartBlockIndex,
                    _agentAddr,
                    _avatarAddr,
                    _targetPetId,
                    currentPetLevel,
                    targetPetLevel,
                    mintAssets: false));
        }

        private static IAccountStateDelta Execute(
            IAccountStateDelta prevStates,
            long blockIndex,
            Address agentAddr,
            Address avatarAddr,
            int petId,
            int currentPetLevel,
            int targetPetLevel,
            bool mintAssets = true,
            bool removePetRow = false,
            bool removePetCostRow = false,
            bool removePetCostRowWithTargetPetLevel = false)
        {
            var context = new ActionContext();
            var petAddress = PetState.DeriveAddress(avatarAddr, petId);
            if (currentPetLevel > 0)
            {
                prevStates = prevStates.SetState(
                    petAddress,
                    new List(
                        petId.Serialize(),
                        currentPetLevel.Serialize(),
                        blockIndex.Serialize()));
            }

            var ncgCurrency = prevStates.GetGoldCurrency();
            var petSheet = prevStates.GetSheet<PetSheet>();
            Assert.True(petSheet.TryGetValue(petId, out var petRow));
            var soulStoneCurrency = Currency.Legacy(petRow.SoulStoneTicker, 0, minters: null);
            if (mintAssets &&
                // NOTE: If the currentPetLevel does not less than targetPetLevel,
                //       ArgumentOutOfRangeException will be thrown.
                //       For this reason, the following condition is added.
                currentPetLevel < targetPetLevel)
            {
                var costSheet = prevStates.GetSheet<PetCostSheet>();
                var (ncgCost, soulStoneCost) = PetHelper.CalculateEnhancementCost(
                    costSheet,
                    petId,
                    currentPetLevel,
                    targetPetLevel);

                if (ncgCost > 0)
                {
                    prevStates = prevStates.MintAsset(context, agentAddr, ncgCost * ncgCurrency);
                }

                if (soulStoneCost > 0)
                {
                    prevStates = prevStates.MintAsset(context, avatarAddr, soulStoneCost * soulStoneCurrency);
                }
            }

            if (removePetRow)
            {
                var petSheetCsv = prevStates.GetSheetCsv<PetSheet>();
                var insolventPetSheetCsv = CsvUtil.CsvLinqWhere(
                    petSheetCsv,
                    line => !line.StartsWith($"{petId},"));
                prevStates = prevStates.SetState(
                    Addresses.GetSheetAddress<PetSheet>(),
                    insolventPetSheetCsv.Serialize());
            }

            if (removePetCostRow || removePetCostRowWithTargetPetLevel)
            {
                var targetPetLevelString = targetPetLevel.ToString();
                var petCostSheetCsv = prevStates.GetSheetCsv<PetCostSheet>();
                string insolventPetCostSheetCsv;
                if (removePetCostRow)
                {
                    insolventPetCostSheetCsv = CsvUtil.CsvLinqWhere(
                        petCostSheetCsv,
                        line => !line.StartsWith($"{petId},"));
                }
                else
                {
                    insolventPetCostSheetCsv = CsvUtil.CsvLinqWhere(
                        petCostSheetCsv,
                        line =>
                        {
                            if (!line.StartsWith($"{petId},"))
                            {
                                return true;
                            }

                            var fields = line.Split(',');
                            return !fields[2].Equals(targetPetLevelString);
                        });
                }

                prevStates = prevStates.SetState(
                    Addresses.GetSheetAddress<PetCostSheet>(),
                    insolventPetCostSheetCsv.Serialize());
            }

            var action = new PetEnhancement
            {
                AvatarAddress = avatarAddr,
                PetId = petId,
                TargetLevel = targetPetLevel,
            };
            var nextStates = action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = prevStates,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = agentAddr,
            });
            var nextNcgBal = nextStates.GetBalance(agentAddr, ncgCurrency);
            var nextSoulStoneBal = nextStates.GetBalance(avatarAddr, soulStoneCurrency);
            Assert.Equal(0, nextNcgBal.MajorUnit);
            Assert.Equal(0, nextSoulStoneBal.MajorUnit);

            var rawPetState = (List)nextStates.GetState(petAddress);
            var nextPetState = new PetState(rawPetState);
            Assert.Equal(targetPetLevel, nextPetState.Level);

            return nextStates;
        }
    }
}
