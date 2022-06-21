using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.DevExtensions.Model;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model.Item;

namespace Lib9c.DevExtensions.Action
{
    [Serializable]
    [ActionType("create_testbed")]
    public class CreateTestbed : GameAction
    {
        private int _slotIndex = 0;
        private PrivateKey _privateKey = new PrivateKey();
        public Result result = new Result();
        public List<Order> Orders = new List<Order>();
        public Address weeklyArenaAddress;

        [Serializable]
        public class Result
        {
            public Address SellerAgentAddress;
            public Address SellerAvatarAddress;
            public List<ItemInfos> ItemInfos;
        }

        public class ItemInfos
        {
            public Guid OrderId;
            public Guid TradableId;
            public ItemSubType ItemSubType;
            public BigInteger Price;
            public int Count;

            public ItemInfos(Guid orderId, Guid tradableId, ItemSubType itemSubType, BigInteger price, int count)
            {
                OrderId = orderId;
                TradableId = tradableId;
                ItemSubType = itemSubType;
                Price = price;
                Count = count;
            }
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                {"w", weeklyArenaAddress.Serialize()},
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            weeklyArenaAddress = plainValue["w"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var sellData = TestbedHelper.LoadData<TestbedSell>("TestbedSell");
            var addedItemInfos = sellData.Items
                .Select(item => new TestbedHelper.AddedItemInfo(
                    context.Random.GenerateRandomGuid(),
                    context.Random.GenerateRandomGuid()))
                .ToList();

            var agentAddress = _privateKey.PublicKey.ToAddress();
            var states = context.PreviousStates;

            var avatarAddress = agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    _slotIndex
                )
            );
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var orderReceiptAddress = OrderDigestListState.DeriveAddress(avatarAddress);

            if (context.Rehearsal)
            {
                states = states.SetState(agentAddress, MarkChanged);
                for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
                {
                    var slotAddress = avatarAddress.Derive(
                        string.Format(CultureInfo.InvariantCulture,
                            CombinationSlotState.DeriveFormat, i));
                    states = states.SetState(slotAddress, MarkChanged);
                }

                states = states.SetState(avatarAddress, MarkChanged)
                    .SetState(Addresses.Ranking, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged);

                for (var i = 0; i < sellData.Items.Length; i++)
                {
                    var itemAddress = Addresses.GetItemAddress(addedItemInfos[i].TradableId);
                    var orderAddress = Order.DeriveAddress(addedItemInfos[i].OrderId);
                    var shopAddress = ShardedShopStateV2.DeriveAddress(
                        sellData.Items[i].ItemSubType,
                        addedItemInfos[i].OrderId);

                    states = states.SetState(avatarAddress, MarkChanged)
                        .SetState(inventoryAddress, MarkChanged)
                        .MarkBalanceChanged(GoldCurrencyMock, agentAddress,
                            GoldCurrencyState.Address)
                        .SetState(orderReceiptAddress, MarkChanged)
                        .SetState(itemAddress, MarkChanged)
                        .SetState(orderAddress, MarkChanged)
                        .SetState(shopAddress, MarkChanged);
                }
                return states;
            }

            // Create Agent and avatar
            var existingAgentState = states.GetAgentState(agentAddress);
            var agentState = existingAgentState ?? new AgentState(agentAddress);
            var avatarState = states.GetAvatarState(avatarAddress);
            if (!(avatarState is null))
            {
                throw new InvalidAddressException(
                    $"Aborted as there is already an avatar at {avatarAddress}.");
            }

            if (agentState.avatarAddresses.ContainsKey(_slotIndex))
            {
                throw new AvatarIndexAlreadyUsedException(
                    $"Aborted as the signer already has an avatar at index #{_slotIndex}.");
            }

            agentState.avatarAddresses.Add(_slotIndex, avatarAddress);

            var rankingState = context.PreviousStates.GetRankingState();
            var rankingMapAddress = rankingState.UpdateRankingMap(avatarAddress);
            avatarState = TestbedHelper.CreateAvatarState(sellData.Avatar.Name,
                agentAddress,
                avatarAddress,
                context.BlockIndex,
                context.PreviousStates.GetAvatarSheets(),
                context.PreviousStates.GetSheet<WorldSheet>(),
                context.PreviousStates.GetGameConfigState(),
                rankingMapAddress);

            // Add item
            var costumeItemSheet =  context.PreviousStates.GetSheet<CostumeItemSheet>();
            var equipmentItemSheet = context.PreviousStates.GetSheet<EquipmentItemSheet>();
            var optionSheet = context.PreviousStates.GetSheet<EquipmentItemOptionSheet>();
            var skillSheet = context.PreviousStates.GetSheet<SkillSheet>();
            var materialItemSheet = context.PreviousStates.GetSheet<MaterialItemSheet>();
            var consumableItemSheet = context.PreviousStates.GetSheet<ConsumableItemSheet>();
            for (var i = 0; i < sellData.Items.Length; i++)
            {
                TestbedHelper.AddItem(costumeItemSheet,
                    equipmentItemSheet,
                    optionSheet,
                    skillSheet,
                    materialItemSheet,
                    consumableItemSheet,
                    context.Random,
                    sellData.Items[i], addedItemInfos[i], avatarState);
            }

            avatarState.Customize(0, 0, 0, 0);

            foreach (var address in avatarState.combinationSlotAddresses)
            {
                var slotState =
                    new CombinationSlotState(address,
                        GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                states = states.SetState(address, slotState.Serialize());
            }

            avatarState.UpdateQuestRewards(materialItemSheet);
            states = states.SetState(agentAddress, agentState.Serialize())
                .SetState(Addresses.Ranking, rankingState.Serialize())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());
            // ~Create Agent and avatar && ~Add item

            // for sell
            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();
            for (var i = 0; i < sellData.Items.Length; i++)
            {
                var itemAddress = Addresses.GetItemAddress(addedItemInfos[i].TradableId);
                var orderAddress = Order.DeriveAddress(addedItemInfos[i].OrderId);
                var shopAddress = ShardedShopStateV2.DeriveAddress(
                    sellData.Items[i].ItemSubType,
                    addedItemInfos[i].OrderId);

                var balance =
                    context.PreviousStates.GetBalance(agentAddress, states.GetGoldCurrency());
                var price = new FungibleAssetValue(balance.Currency, sellData.Items[i].Price, 0);
                var order = OrderFactory.Create(agentAddress, avatarAddress,
                    addedItemInfos[i].OrderId,
                    price,
                    addedItemInfos[i].TradableId,
                    context.BlockIndex,
                    sellData.Items[i].ItemSubType,
                    sellData.Items[i].Count);

                Orders.Add(order);
                order.Validate(avatarState, sellData.Items[i].Count);
                var tradableItem = order.Sell(avatarState);

                var shardedShopState =
                    states.TryGetState(shopAddress, out Dictionary serializedState)
                        ? new ShardedShopStateV2(serializedState)
                        : new ShardedShopStateV2(shopAddress);
                var orderDigest = order.Digest(avatarState, costumeStatSheet);
                shardedShopState.Add(orderDigest, context.BlockIndex);
                var orderReceiptList =
                    states.TryGetState(orderReceiptAddress, out Dictionary receiptDict)
                        ? new OrderDigestListState(receiptDict)
                        : new OrderDigestListState(orderReceiptAddress);
                orderReceiptList.Add(orderDigest);

                states = states.SetState(orderReceiptAddress, orderReceiptList.Serialize())
                    .SetState(inventoryAddress, avatarState.inventory.Serialize())
                    .SetState(avatarAddress, avatarState.SerializeV2())
                    .SetState(itemAddress, tradableItem.Serialize())
                    .SetState(orderAddress, order.Serialize())
                    .SetState(shopAddress, shardedShopState.Serialize());
            }

            result.SellerAgentAddress = agentAddress;
            result.SellerAvatarAddress = avatarAddress;
            result.ItemInfos = new List<ItemInfos>();
            for (var i = 0; i < sellData.Items.Length; i++)
            {
                result.ItemInfos.Add(new ItemInfos(
                    addedItemInfos[i].OrderId,
                    addedItemInfos[i].TradableId,
                    sellData.Items[i].ItemSubType,
                    sellData.Items[i].Price,
                    sellData.Items[i].Count));
            }

            return states;
        }
    }
}
