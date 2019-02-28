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

        public override AddressStateMap Execute(IActionContext actionCtx)
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
            foreach (var id_ in new []{301001, 303001, 304001, 305001, 306001, 307001})
            {
                Item itemData;
                table.TryGetValue(id_, out itemData);
                var equipment = ItemBase.ItemFactory(itemData);
                avatar.Items.Add(new Inventory.InventoryItem(equipment));
            }
            var states = actionCtx.PreviousStates;
            var to = actionCtx.To;
            var ctx = new Context(avatar);
            return (AddressStateMap) states.SetItem(to, ctx);
        }
        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>()
        {
            ["name"] = name,
        }.ToImmutableDictionary();
    }
}
