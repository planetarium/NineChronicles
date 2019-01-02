using UnityEngine;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;


namespace Nekoyume.Data
{
    public class Tables : MonoBehaviour
    {
        public Table<Stats> Stats { get; private set; }
        public Table<Skill> Skill { get; private set; }
        public Table<Stage> Stage { get; private set; }
        public Table<MonsterAppear> MonsterAppear { get; private set; }
        public Table<Monster> Monster { get; private set; }
        public Table<ItemDrop> ItemDrop { get; private set; }
        public Table<BoxDrop> BoxDrop { get; private set; }
        public Table<Item> Item { get; private set; }

        private void Start()
        {
            Stats = new Table<Stats>();
            Load(Stats, "DataTable/stats");

            Skill = new Table<Skill>();
            Load(Skill, "DataTable/skills");
            Load(Skill, "DataTable/monster_skills");

            Stage = new Table<Stage>();
            Load(Stage, "DataTable/stage");

            MonsterAppear = new Table<MonsterAppear>();
            Load(MonsterAppear, "DataTable/monster_appear");

            Monster = new Table<Monster>();
            Load(Monster, "DataTable/monsters");

            ItemDrop = new Table<ItemDrop>();
            Load(ItemDrop, "DataTable/item_drop");

            BoxDrop = new Table<BoxDrop>();
            Load(BoxDrop, "DataTable/box_drop");

            Item = new Table<Item>();
            Load(Item, "DataTable/item");
            Load(Item, "DataTable/item_equip");
            Load(Item, "DataTable/item_box");
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
