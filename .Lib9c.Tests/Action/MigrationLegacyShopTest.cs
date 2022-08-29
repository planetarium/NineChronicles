namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Xunit;

    public class MigrationLegacyShopTest
    {
        private readonly TableSheets _tableSheets;

        public MigrationLegacyShopTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Theory]
        [InlineData(false, false, typeof(PermissionDeniedException))]
        [InlineData(false, true, typeof(PolicyExpiredException))]
        [InlineData(true, false, null)]
        public void Execute(bool isAdmin, bool expire, Type exc)
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var states = new State().SetState(Addresses.Admin, adminState.Serialize());
            var signer = isAdmin ? adminAddress : default;
            var blockIndex = expire ? 200 : 100;

            var action = new MigrationLegacyShop();

            var avatarAddress = new Address(action.AvatarAddressesHex.First());

            if (exc is null)
            {
                var agentState = new AgentState(adminAddress);
                var avatarState = new AvatarState(
                    avatarAddress,
                    adminAddress,
                    0,
                    _tableSheets.GetAvatarSheets(),
                    new GameConfigState(),
                    default);
                agentState.avatarAddresses[0] = avatarAddress;

                var shopState = new ShopState();
                var itemSubTypes = new[] { ItemSubType.Weapon, ItemSubType.FullCostume };
                var random = new TestRandom();
                var itemIds = new List<Guid>();
                foreach (var itemSubType in itemSubTypes)
                {
                    var item = (ITradableItem)ItemFactory.CreateItem(_tableSheets.ItemSheet.Values.First(r => r.ItemSubType == itemSubType), random);
                    var shopItem = new ShopItem(
                        adminAddress,
                        avatarAddress,
                        Guid.NewGuid(),
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                        Currency.Legacy("NCG", 2, null) * 100,
#pragma warning restore CS0618
                        item);
                    shopState.Register(shopItem);
                    itemIds.Add(item.TradableId);
                }

                states = states
                    .SetState(Addresses.Shop, shopState.Serialize())
                    .SetState(adminAddress, agentState.Serialize())
                    .SetState(avatarAddress, avatarState.Serialize());

                var nextState = action.Execute(new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousStates = states,
                    Signer = signer,
                });

                var nextShopState = nextState.GetShopState();
                Assert.Empty(nextShopState.Products);
                var nextAvatarState = nextState.GetAvatarState(avatarAddress);
                Assert.All(itemIds, id => nextAvatarState.inventory.HasNonFungibleItem(id));
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousStates = states,
                    Signer = signer,
                }));
            }
        }
    }
}
