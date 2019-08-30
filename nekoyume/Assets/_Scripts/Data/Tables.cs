using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.Pattern;
using UnityEngine;

namespace Nekoyume.Data
{
    public class Tables : MonoSingleton<Tables>
    {
        public Table<Item> Item { get; private set; }
        public Table<Recipe> Recipe { get; private set; }
        public Table<Elemental> Elemental { get; private set; }
        public Table<ItemEquipment> ItemEquipment { get; private set; }
        public Table<SetEffect> SetEffect { get; private set; }
        public Table<SkillEffect> SkillEffect { get; private set; }
        public Table<StageDialog> StageDialogs { get; private set; }
        public Table<Quest> Quest { get; private set; }
        public Table<CollectQuest> CollectQuest { get; private set; }
        public Table<CombinationQuest> CombinationQuest { get; private set; }
        public Table<TradeQuest> TradeQuest { get; private set; }

        public void Initialize()
        {
            Item = new Table<Item>();
            Load(Item, "DataTable/item");

            ItemEquipment = new Table<ItemEquipment>();
            Load(ItemEquipment, "DataTable/item_equip");

            Recipe = new Table<Recipe>();
            Load(Recipe, "DataTable/recipe");

            Elemental = new Table<Elemental>();
            Load(Elemental, "DataTable/elemental");

            SetEffect = new Table<SetEffect>();
            Load(SetEffect, "DataTable/set_effect");

            SkillEffect = new Table<SkillEffect>();
            Load(SkillEffect, "DataTable/skill_effect");

            StageDialogs = new Table<StageDialog>();
            Load(StageDialogs, "DataTable/stage_dialog");

            Quest = new Table<Quest>();
            Load(Quest, "DataTable/battle_quest");

            CollectQuest = new Table<CollectQuest>();
            Load(CollectQuest, "DataTable/collect_quest");

            CombinationQuest = new Table<CombinationQuest>();
            Load(CombinationQuest, "DataTable/combination_quest");

            TradeQuest = new Table<TradeQuest>();
            Load(TradeQuest, "DataTable/trade_quest");
        }

        private void Load(ITable table, string filename)
        {
            var file = Resources.Load<TextAsset>(filename);
            if (file != null)
            {
                table.Load(file.text);
            }
        }

        public ItemBase CreateItemBase(int itemId)
        {
            return !Item.TryGetValue(itemId, out var item) ? null : ItemFactory.Create(item, default);
        }

        public bool TryGetItem(int itemId, out Item item)
        {
            return Item.TryGetValue(itemId, out item);
        }

        public bool TryGetItemEquipment(int itemEquipmentId, out ItemEquipment itemEquipment)
        {
            return ItemEquipment.TryGetValue(itemEquipmentId, out itemEquipment);
        }

        public IEnumerable<IStatMap> GetSetEffect(int id, int count)
        {
            var statMaps = new List<IStatMap>();
            foreach (var row in SetEffect)
            {
                if (row.Value.setId == id)
                {
                    statMaps.Add(row.Value.ToSetEffectMap());
                }
            }

            return statMaps.Take(count).ToArray();
        }
    }
}
