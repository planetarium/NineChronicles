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
            _currency = new Currency("NCG", 2, minters: null);
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

        [Fact]
        public void Serialize_With_DotnetAPI()
        {
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            var evaluation = new ActionBase.ActionEvaluation<ActionBase>()
            {
                Action = GetAction(typeof(TransferAsset)),
                Signer = _signer,
                BlockIndex = 1234,
                PreviousStates = _states,
                OutputStates = _states,
            };
            formatter.Serialize(ms, evaluation);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (ActionBase.ActionEvaluation<ActionBase>)formatter.Deserialize(ms);

            // FIXME We should equality check more precisely.
            Assert.Equal(evaluation.Signer, deserialized.Signer);
            Assert.Equal(evaluation.BlockIndex, deserialized.BlockIndex);
            var dict = (Dictionary)deserialized.OutputStates.GetState(default)!;
            Assert.Equal("value", (Text)dict["key"]);
            Assert.Equal(_currency * 10000, deserialized.OutputStates.GetBalance(_signer, _currency));
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
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    foods = new List<Guid>(),
                    worldId = 0,
                    stageId = 0,
                    playCount = 0,
                    avatarAddress = new PrivateKey().ToAddress(),
                    rankingMapAddress = new PrivateKey().ToAddress(),
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
                            new Currency("NCG", 2, minters: null) * 10
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
                    Ranking = default,
                    Shop = default,
                    TableSheets = new Dictionary<string, string>(),
                    GameConfig = default,
                    RedeemCode = default,
                    AdminAddress = default,
                    ActivatedAccounts = default,
                    GoldCurrency = default,
                    GoldDistributions = default,
                    PendingActivations = default,
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
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    foods = new List<Guid>(),
                    worldId = 0,
                    stageId = 0,
                    playCount = 0,
                    avatarAddress = default,
                    rankingMapAddress = new PrivateKey().ToAddress(),
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
                    consumableIds = new List<Guid>(),
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
                    price = _currency * 100,
                },
                _ => throw new InvalidCastException()
            };
        }
    }
}
