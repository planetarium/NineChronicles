using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Bencodex.Types;
using Libplanet;
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
        private const string kRecipePath = "Assets/Resources/DataTable/recipe.csv";
        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            material_1 = int.Parse(plainValue["material_1"].ToString());
            material_2 = int.Parse(plainValue["material_2"].ToString());
            material_3 = int.Parse(plainValue["material_3"].ToString());
            result = int.Parse(plainValue["result"].ToString());
        }

        public override AddressStateMap Execute(Address @from, Address to, AddressStateMap states)
        {
            var ctx = (Context) states.GetValueOrDefault(to);
            var player = new Player(ctx.avatar);
            var materials = new List<int>
            {
                material_1,
                material_2,
                material_3
            };
            var items = player.inventory._items.Select(i => i.Item.Data.Id);
            bool owned = materials.All(material => items.Contains(material));
            if (!owned)
            {
                throw new InvalidActionException();
            }
            var recipe = new Table<Recipe>();
            var recipePath = Path.Combine(Directory.GetCurrentDirectory(), kRecipePath);
            recipe.Load(File.ReadAllText(recipePath));

            var itemTable = Agent.ItemTable();

            Recipe r;
            if (recipe.TryGetValue(result, out r))
            {
                foreach (var material in materials)
                {
                    var inventoryItem = player.inventory._items.Find(i => i.Item.Data.Id == material);
                    inventoryItem.Count -= 1;
                }
                Item itemData;
                if (itemTable.TryGetValue(r.Id, out itemData))
                {
                    var combined = ItemBase.ItemFactory(itemData);
                    player.inventory.Add(combined);
                }
                ctx.avatar.Update(player);
                return (AddressStateMap) states.SetItem(to, ctx);
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
