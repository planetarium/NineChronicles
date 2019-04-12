using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [ActionType("create_novice")]
    public class CreateNovice : ActionBase
    {
        public string name;
        public const int DefaultId = 100010;
        private const int DefaultSetId = 1;
        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            name = (string) plainValue["name"];
        }

        public static Context CreateContext(string name)
        {
            Avatar avatar = CreateAvatar(name);
            var ctx = new Context(avatar);
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

            var table = ActionManager.Instance.tables.ItemEquipment;
            foreach (ItemEquipment data in table.Select(i => i.Value).Where(e => e.setId == DefaultSetId))
            {
                var equipment = ItemBase.ItemFactory(data);
                avatar.Items.Add(new Inventory.InventoryItem(equipment));
            }

            return avatar;
        }

        public override IAccountStateDelta Execute(IActionContext actionCtx)
        {
            IAccountStateDelta states = actionCtx.PreviousStates;
            var ctx = (Context)states.GetState(actionCtx.Signer);
            if (ReferenceEquals(ctx, null))
            {
                ctx = CreateContext(name);
            }
            else
            {
                ctx.avatar = CreateAvatar(name);
            }
            return states.SetState(actionCtx.Signer, ctx);
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>()
        {
            ["name"] = name,
        }.ToImmutableDictionary();
    }
}
