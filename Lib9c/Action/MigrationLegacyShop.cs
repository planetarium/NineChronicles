using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("migration_legacy_shop")]
    public class MigrationLegacyShop : GameAction
    {
        public List<Address> SellerAvatarAddresses;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                states = SellerAvatarAddresses.Aggregate(states,
                    (current, sellerAvatarAddress) => current.SetState(sellerAvatarAddress, MarkChanged));

                return states.SetState(Addresses.Shop, MarkChanged);
            }

            CheckPermission(context);

            Log.Debug("Start Migration Legacy Shop");
            var shopState = states.GetShopState();
            var groupBy = shopState.Products.Values
                .Where(s => SellerAvatarAddresses.Contains(s.SellerAvatarAddress))
                .GroupBy(p => p.SellerAvatarAddress)
                .ToList();

            Log.Debug($"Found {groupBy.Count} Seller Avatar.");
            foreach (var group in groupBy)
            {
                var avatarAddress = group.Key;
                var avatarState = states.GetAvatarState(avatarAddress);
                Log.Debug($"Start Migration Avatar({avatarAddress}). Target Count: {group.Count()}");
                foreach (var shopItem in group)
                {
                    var sellerAgentAddress = shopItem.SellerAgentAddress;
                    var agentAddress = avatarState.agentAddress;
                    if (!sellerAgentAddress.Equals(agentAddress))
                    {
                        Log.Debug($"Skip Invalid ShopItem. Expected: {sellerAgentAddress}, Actual: {agentAddress}");
                        continue;
                    }

                    if (!(shopItem.ItemUsable is null))
                    {
                        avatarState.inventory.AddItem(shopItem.ItemUsable);
                    }
                    if (!(shopItem.Costume is null))
                    {
                        avatarState.inventory.AddItem(shopItem.Costume);
                    }
                    shopState.Unregister(shopItem);
                }
                states = states.SetState(avatarAddress, avatarState.Serialize());
                Log.Debug($"Finish Migration Avatar({avatarAddress})");
            }

            states = states.SetState(Addresses.Shop, shopState.Serialize());
            Log.Debug($"Finish Migration Legacy Shop({shopState.Products.Count})");

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["sa"] = new List(SellerAvatarAddresses.Select(a => a.Serialize()))
        }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            SellerAvatarAddresses = plainValue["sa"].ToList(StateExtensions.ToAddress);
        }
    }
}
