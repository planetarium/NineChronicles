using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("create_novice")]
    public class CreateNovice : GameAction
    {
        public string name;
        public Address avatarAddress;
        public const int DefaultId = 100010;
        private const int DefaultSetId = 1;
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            name = (string) plainValue["name"];
            avatarAddress = new Address((byte[])plainValue["avatar_address"]);
        }

        public static AvatarState CreateState(string name, Address avatarAddress)
        {
            Avatar avatar = CreateAvatar(name);
            var ctx = new AvatarState(avatar, avatarAddress);
            return ctx;
        }

        public static Avatar CreateAvatar(string name)
        {
            var avatar = new Avatar
            {
                Name = name,
                Level = 1,
                EXP = 0,
                HPMax = 0,
                WorldStage = 1,
                CurrentHP = 0,
                Items = new List<Inventory.InventoryItem>(),
                id = DefaultId,
            };

            var table = Tables.instance.ItemEquipment;
            foreach (var data in table.Select(i => i.Value).Where(e => e.setId == DefaultSetId))
            {
                var equipment = ItemBase.ItemFactory(data);
                avatar.Items.Add(new Inventory.InventoryItem(equipment));
            }

            return avatar;
        }

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            IAccountStateDelta states = actionCtx.PreviousStates;
            var ctx = (AvatarState)states.GetState(actionCtx.Signer);
            if (ReferenceEquals(ctx, null))
            {
                ctx = CreateState(name, avatarAddress);
            }
            else
            {
                ctx.avatar = CreateAvatar(name);
            }
            return states.SetState(actionCtx.Signer, ctx);
        }

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>()
        {
            ["name"] = name,
            ["avatar_address"] = avatarAddress.ToByteArray(),
        }.ToImmutableDictionary();
    }
}
