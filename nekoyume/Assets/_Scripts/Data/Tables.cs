using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Data
{
    public class Tables : MonoSingleton<Tables>
    {
        public Table<Level> Level { get; private set; }
        public Table<Stage> Stage { get; private set; }
        public Table<World> World { get; private set; }
        public Table<Character> Character { get; private set; }
        public Table<Item> Item { get; private set; }
        public Table<Recipe> Recipe { get; private set; }
        public Table<Elemental> Elemental { get; private set; }
        public Table<ItemEquipment> ItemEquipment { get; private set; }
        public Table<Background> Background { get; private set; }
        public Table<StageReward> StageReward { get; private set; }
        public Table<SetEffect> SetEffect { get; private set; }
        public Table<SkillEffect> SkillEffect { get; private set; }
        public Table<Setting> Settings { get; private set; }
        public Table<StageDialog> StageDialogs { get; private set; }
        public Table<Quest> Quest { get; private set; }
        public Table<CollectQuest> CollectQuest { get; private set; }
        public Table<CombinationQuest> CombinationQuest { get; private set; }
        public Table<TradeQuest> TradeQuest { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            
            Level = new Table<Level>();
            Load(Level, "DataTable/level");

            Stage = new Table<Stage>();
            Load(Stage, "DataTable/stage");

            World = new Table<World>();
            Load(World, "DataTable/world");

            Character = new Table<Character>();
            Load(Character, "DataTable/character");

            Item = new Table<Item>();
            Load(Item, "DataTable/item");
            ItemEquipment = new Table<ItemEquipment>();
            Load(ItemEquipment, "DataTable/item_equip");

            Recipe = new Table<Recipe>();
            Load(Recipe, "DataTable/recipe");
            Elemental = new Table<Elemental>();
            Load(Elemental, "DataTable/elemental");

            Background = new Table<Background>();
            Load(Background, "DataTable/background");

            StageReward = new Table<StageReward>();
            Load(StageReward, "DataTable/stage_reward");
            SetEffect = new Table<SetEffect>();
            Load(SetEffect, "DataTable/set_effect");
            SkillEffect = new Table<SkillEffect>();
            Load(SkillEffect, "DataTable/skill_effect");

            Settings = new Table<Setting>();
            Load(Settings, "DataTable/settings");
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
            return !Item.TryGetValue(itemId, out var item) ? null : ItemBase.ItemFactory(item);
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
            var effects = new List<IStatMap>();
            foreach (var row in SetEffect)
            {
                if (row.Value.setId == id)
                {
                    effects.Add(row.Value.ToSetEffectMap());
                }
            }

            return effects.Take(count).ToArray();
        }
    }
}
