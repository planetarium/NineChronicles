using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
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
            };
            var table = ActionManager.Instance.tables.Item;
            // equipments id from item_equip.csv
            foreach (var id in new[] {304001, 304002, 304003, 308001, 308002, 308003, 101001})
            {
                Item itemData;
                table.TryGetValue(id, out itemData);
                var equipment = ItemBase.ItemFactory(itemData);

                avatar.Items.Add(id == 101001
                    ? new Inventory.InventoryItem(equipment, 30)
                    : new Inventory.InventoryItem(equipment));
            }

            return avatar;
        }

        public override IAccountStateDelta Execute(IActionContext actionCtx)
        {
            IAccountStateDelta states = actionCtx.PreviousStates;
            Context ctx = CreateContext(name);
            return states.SetState(actionCtx.Signer, ctx);
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>()
        {
            ["name"] = name,
        }.ToImmutableDictionary();
    }
}
