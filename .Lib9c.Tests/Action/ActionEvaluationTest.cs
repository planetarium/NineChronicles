namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Formatters;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using MessagePack;
    using MessagePack.Resolvers;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Xunit;

    public class ActionEvaluationTest
    {
        private readonly Currency _currency;
        private readonly Address _signer;
        private readonly Address _sender;
        private readonly IAccountStateDelta _states;

        public ActionEvaluationTest()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _signer = new PrivateKey().ToAddress();
            _sender = new PrivateKey().ToAddress();
            _states = new State()
                .SetState(_signer, (Text)"ANYTHING")
                .SetState(default, Dictionary.Empty.Add("key", "value"))
                .MintAsset(_signer, _currency * 10000);
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                NineChroniclesResolver.Instance,
                StandardResolver.Instance
            );
            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;
        }

        [Theory]
        [InlineData(typeof(TransferAsset))]
        [InlineData(typeof(CreateAvatar))]
        [InlineData(typeof(HackAndSlash))]
        [InlineData(typeof(ActivateAccount))]
        [InlineData(typeof(AddActivatedAccount))]
        [InlineData(typeof(AddRedeemCode))]
        [InlineData(typeof(Buy))]
        [InlineData(typeof(ChargeActionPoint))]
        [InlineData(typeof(ClaimMonsterCollectionReward))]
        [InlineData(typeof(CombinationConsumable))]
        [InlineData(typeof(CombinationEquipment))]
        [InlineData(typeof(CreatePendingActivation))]
        [InlineData(typeof(DailyReward))]
        [InlineData(typeof(InitializeStates))]
        [InlineData(typeof(ItemEnhancement))]
        [InlineData(typeof(MigrationActivatedAccountsState))]
        [InlineData(typeof(MigrationAvatarState))]
        [InlineData(typeof(MigrationLegacyShop))]
        [InlineData(typeof(MimisbrunnrBattle))]
        [InlineData(typeof(MonsterCollect))]
        [InlineData(typeof(PatchTableSheet))]
        [InlineData(typeof(RankingBattle))]
        [InlineData(typeof(RapidCombination))]
        [InlineData(typeof(RedeemCode))]
        [InlineData(typeof(RewardGold))]
        [InlineData(typeof(Sell))]
        [InlineData(typeof(SellCancellation))]
        [InlineData(typeof(UpdateSell))]
        [InlineData(typeof(CreatePendingActivations))]
        [InlineData(typeof(Grinding))]
        [InlineData(typeof(UnlockEquipmentRecipe))]
        [InlineData(typeof(UnlockWorld))]
        [InlineData(typeof(EventDungeonBattle))]
        [InlineData(typeof(EventConsumableItemCrafts))]
        [InlineData(typeof(Raid))]
        [InlineData(typeof(ClaimRaidReward))]
        [InlineData(typeof(ClaimWordBossKillReward))]
        [InlineData(typeof(PrepareRewardAssets))]
        public void Serialize_With_MessagePack(Type actionType)
        {
            var action = GetAction(actionType);
            var ncAction = action is null ? null : new PolymorphicAction<ActionBase>(action);
            var ncEval = new NCActionEvaluation(
                ncAction,
                _signer,
                1234,
                _states,
                null,
                _states,
                0,
                new Dictionary<string, IValue>()
            );
            var evaluation = ncEval.ToActionEvaluation();
            var b = MessagePackSerializer.Serialize(ncEval);
            var deserialized = MessagePackSerializer.Deserialize<NCActionEvaluation>(b);
            Assert.Equal(evaluation.Signer, deserialized.Signer);
            Assert.Equal(evaluation.BlockIndex, deserialized.BlockIndex);
            var dict = (Dictionary)deserialized.OutputStates.GetState(default)!;
            Assert.Equal("value", (Text)dict["key"]);
            Assert.Equal(_currency * 10000, deserialized.OutputStates.GetBalance(_signer, _currency));
            if (actionType == typeof(RewardGold))
            {
                Assert.Null(deserialized.Action);
            }
            else
            {
                Assert.NotNull(deserialized.Action);
                Assert.IsType(actionType, deserialized.Action.InnerAction);
            }

            if (action is GameAction gameAction)
            {
                Assert.Equal(gameAction.Id, ((GameAction)deserialized.Action.InnerAction).Id);
            }
        }

        private ActionBase GetAction(Type type)
        {
            var action = Activator.CreateInstance(type);
            return action switch
            {
                TransferAsset _ => new TransferAsset(_sender, _signer, _currency * 100),
                CreateAvatar _ => new CreateAvatar
                {
                    ear = 0,
                    hair = 0,
                    index = 0,
                    lens = 0,
                    name = "name",
                    tail = 0,
                },
                HackAndSlash _ => new HackAndSlash
                {
                    Costumes = new List<Guid>(),
                    Equipments = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                    WorldId = 0,
                    StageId = 0,
                    AvatarAddress = new PrivateKey().ToAddress(),
                },
                ActivateAccount _ => new ActivateAccount(new PrivateKey().ToAddress(), new byte[] { 0x0 }),
                AddActivatedAccount _ => new AddActivatedAccount(),
                AddRedeemCode _ => new AddRedeemCode
                {
                    redeemCsv = "csv",
                },
                Buy _ => new Buy
                {
                    buyerAvatarAddress = new PrivateKey().ToAddress(),
                    purchaseInfos = new[]
                    {
                        new PurchaseInfo(
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            _signer,
                            new PrivateKey().ToAddress(),
                            ItemSubType.Armor,
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                            Currency.Legacy("NCG", 2, null) * 10
#pragma warning restore CS0618
                        ),
                    },
                },
                ChargeActionPoint _ => new ChargeActionPoint(),
                ClaimMonsterCollectionReward _ => new ClaimMonsterCollectionReward(),
                CombinationConsumable _ => new CombinationConsumable(),
                CombinationEquipment _ => new CombinationEquipment(),
                CreatePendingActivation _ => new CreatePendingActivation(
                    new PendingActivationState(new byte[] { 0x0 }, new PrivateKey().PublicKey)
                ),
                DailyReward _ => new DailyReward(),
                InitializeStates _ => new InitializeStates
                {
                    TableSheets = new Dictionary<string, string>(),
                    AuthorizedMiners = Dictionary.Empty,
                    Credits = Dictionary.Empty,
                },
                ItemEnhancement _ => new ItemEnhancement(),
                MigrationActivatedAccountsState _ => new MigrationActivatedAccountsState(),
                MigrationAvatarState _ => new MigrationAvatarState
                {
                    avatarStates = new List<Dictionary>(),
                },
                MigrationLegacyShop _ => new MigrationLegacyShop(),
                MimisbrunnrBattle _ => new MimisbrunnrBattle
                {
                    Costumes = new List<Guid>(),
                    Equipments = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                    WorldId = 0,
                    StageId = 0,
                    PlayCount = 0,
                    AvatarAddress = default,
                },
                MonsterCollect _ => new MonsterCollect(),
                PatchTableSheet _ => new PatchTableSheet
                {
                    TableCsv = "table",
                    TableName = "name",
                },
                RankingBattle _ => new RankingBattle
                {
                    avatarAddress = new PrivateKey().ToAddress(),
                    enemyAddress = new PrivateKey().ToAddress(),
                    weeklyArenaAddress = new PrivateKey().ToAddress(),
                    costumeIds = new List<Guid>(),
                    equipmentIds = new List<Guid>(),
                },
                RapidCombination _ => new RapidCombination(),
                RedeemCode _ => new RedeemCode
                {
                    Code = "code",
                    AvatarAddress = new PrivateKey().ToAddress(),
                },
                RewardGold _ => null,
                Sell _ => new Sell
                {
                    price = _currency * 100,
                },
                SellCancellation _ => new SellCancellation(),
                UpdateSell _ => new UpdateSell
                {
                    sellerAvatarAddress = new PrivateKey().ToAddress(),
                    updateSellInfos = new[]
                    {
                        new UpdateSellInfo(
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            ItemSubType.Armor,
                            _currency * 100,
                            1
                        ),
                    },
                },
                CreatePendingActivations _ => new CreatePendingActivations
                {
                    PendingActivations = new[]
                    {
                        (new byte[40], new byte[4], new byte[33]),
                    },
                },
                Grinding _ => new Grinding
                {
                    AvatarAddress = new PrivateKey().ToAddress(),
                    EquipmentIds = new List<Guid>(),
                },
                UnlockEquipmentRecipe _ => new UnlockEquipmentRecipe
                {
                    AvatarAddress = new PrivateKey().ToAddress(),
                    RecipeIds = new List<int>
                    {
                        2,
                        3,
                    },
                },
                UnlockWorld _ => new UnlockWorld
                {
                    AvatarAddress = new PrivateKey().ToAddress(),
                    WorldIds = new List<int>()
                    {
                        2,
                        3,
                    },
                },
                EventDungeonBattle _ => new EventDungeonBattle
                {
                    AvatarAddress = default,
                    EventScheduleId = 0,
                    EventDungeonId = 0,
                    EventDungeonStageId = 0,
                    Equipments = new List<Guid>(),
                    Costumes = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                },
                EventConsumableItemCrafts _ => new EventConsumableItemCrafts
                {
                    AvatarAddress = default,
                    EventScheduleId = 0,
                    EventConsumableItemRecipeId = 0,
                    SlotIndex = 0,
                },
                Raid _ => new Raid
                {
                    AvatarAddress = new PrivateKey().ToAddress(),
                    CostumeIds = new List<Guid>(),
                    EquipmentIds = new List<Guid>(),
                    FoodIds = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                    PayNcg = true,
                },
                ClaimRaidReward _ => new ClaimRaidReward(_sender),
                ClaimWordBossKillReward _ => new ClaimWordBossKillReward
                {
                    AvatarAddress = _sender,
                },
                PrepareRewardAssets _ => new PrepareRewardAssets
                {
                    RewardPoolAddress = _sender,
                    Assets = new List<FungibleAssetValue>
                    {
                        _currency * 100,
                    },
                },
                _ => throw new InvalidCastException(),
            };
        }
    }
}
