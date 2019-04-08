using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [ActionType("combination")]
    public class Combination : ActionBase
    {
        public int material_1;
        public int material_2;
        public int material_3;
        public int result;
        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            material_1 = int.Parse(plainValue["material_1"].ToString());
            material_2 = int.Parse(plainValue["material_2"].ToString());
            material_3 = int.Parse(plainValue["material_3"].ToString());
            result = int.Parse(plainValue["result"].ToString());
        }

        public override IAccountStateDelta Execute(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            var ctx = (Context) states.GetState(actionCtx.Signer) ?? CreateNovice.CreateContext("dummy");
            var player = new Player(ctx.avatar);
            var materials = new List<int>
            {
                material_1,
                material_2,
                material_3
            };
            var items = player.inventory.items.Select(i => i.Item.Data.id);
            bool owned = materials.All(material => items.Contains(material));
            if (!owned)
            {
                throw new InvalidActionException();
            }

            var tables = ActionManager.Instance.tables;
            var recipe = tables.Recipe;
            var itemTable = tables.Item;

            Recipe r;
            if (recipe.TryGetValue(result, out r))
            {
                foreach (var material in materials)
                {
                    var inventoryItem =
                        player.inventory.items.FirstOrDefault(i => i.Item.Data.id == material && i.Count >= 1);
                    if (inventoryItem == null)
                    {
                        throw new InvalidActionException();
                    }
                    inventoryItem.Count -= 1;
                }
                Item itemData;
                if (itemTable.TryGetValue(r.Id, out itemData))
                {
                    var combined = ItemBase.ItemFactory(itemData);
                    player.inventory.Add(combined);
                }
                ctx.avatar.Update(player);
                return states.SetState(actionCtx.Signer, ctx);
            }
            throw new InvalidActionException();
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>
        {
            ["material_1"] = material_1,
            ["material_2"] = material_2,
            ["material_3"] = material_3,
            ["result"] = result,
        }.ToImmutableDictionary();
    }
}
