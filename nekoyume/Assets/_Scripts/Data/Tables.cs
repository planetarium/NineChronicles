using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using UnityEngine;

namespace Nekoyume.Data
{
    public class Tables : MonoBehaviour
    {
        public Table<Level> Level { get; private set; }
        public Table<Skill> Skill { get; private set; }
        public Table<Stage> Stage { get; private set; }
        public Table<MonsterAppear> MonsterAppear { get; private set; }
        public Table<Character> Character { get; private set; }
        public Table<ItemDrop> ItemDrop { get; private set; }
        public Table<BoxDrop> BoxDrop { get; private set; }
        public Table<Item> Item { get; private set; }
        public Table<Recipe> Recipe { get; private set; }
        public Table<MonsterWave> MonsterWave { get; private set; }
        public Table<Elemental> Elemental { get; private set; }
        public Table<ItemEquipment> ItemEquipment { get; private set; }
        public Table<Background> Background { get; private set; }
        public Table<StageReward> StageReward { get; private set; }

        private void Start()
        {
            Level = new Table<Level>();
            Load(Level, "DataTable/level");
//
//            Skill = new Table<Skill>();
//            Load(Skill, "DataTable/skills");
//            Load(Skill, "DataTable/monster_skills");

            Stage = new Table<Stage>();
            Load(Stage, "DataTable/stage");

//            MonsterAppear = new Table<MonsterAppear>();
//            Load(MonsterAppear, "DataTable/monster_appear");

            Character = new Table<Character>();
            Load(Character, "DataTable/character");

//            ItemDrop = new Table<ItemDrop>();
//            Load(ItemDrop, "DataTable/item_drop");
//
//            BoxDrop = new Table<BoxDrop>();
//            Load(BoxDrop, "DataTable/box_drop");
//
            Item = new Table<Item>();
            Load(Item, "DataTable/item");
            ItemEquipment = new Table<ItemEquipment>();
            Load(ItemEquipment, "DataTable/item_equip");

//            Load(Item, "DataTable/item_box");
//
//            Recipe = new Table<Recipe>();
//            Load(Recipe, "DataTable/recipe");
//            MonsterWave = new Table<MonsterWave>();
//            Load(MonsterWave, "DataTable/monster_wave");
            Elemental = new Table<Elemental>();
            Load(Elemental, "DataTable/elemental");

            Background = new Table<Background>();
            Load(Background, "DataTable/background");

            StageReward = new Table<StageReward>();
            Load(StageReward, "DataTable/stage_reward");
        }

        private void Load(ITable table, string filename)
        {
            TextAsset file = Resources.Load<TextAsset>(filename);
            if (file != null)
            {
                table.Load(file.text);
            }
        }

        public ItemBase GetItem(int itemId)
        {
            Item itemData;
            if (!Item.TryGetValue(itemId, out itemData))
                return null;
            var item = ItemBase.ItemFactory(itemData);
            return item;
        }
    }
}
