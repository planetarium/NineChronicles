using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    // todo: 소모품과 장비가 함께 쓰기에는 장비 위주의 모델이 된 느낌. 아이템 정리하면서 정리를 흐음..
    [Serializable]
    public abstract class ItemUsable : ItemBase
    {
        public new ItemSheet.Row Data { get; }
        public Guid ItemId { get; }
        public StatsMap StatsMap { get; }
        public List<Skill.Skill> Skills { get; }
        public List<BuffSkill> BuffSkills { get; }

        protected ItemUsable(ItemSheet.Row data, Guid id) : base(data)
        {
            Data = data;
            ItemId = id;
            StatsMap = new StatsMap();

            switch (data)
            {
                case ConsumableItemSheet.Row consumableItemRow:
                    {
                        foreach (var statData in consumableItemRow.Stats)
                        {
                            StatsMap.AddStatValue(statData.StatType, statData.Value);
                        }

                        break;
                    }
                case EquipmentItemSheet.Row equipmentItemRow:
                    StatsMap.AddStatValue(equipmentItemRow.Stat.Type, equipmentItemRow.Stat.Value);
                    break;
            }

            Skills = new List<Model.Skill.Skill>();
            BuffSkills = new List<BuffSkill>();
        }

        protected bool Equals(ItemUsable other)
        {
            return base.Equals(other) && Equals(ItemId, other.ItemId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ItemUsable)obj);
        }

        public override int GetHashCode()
        {
            return (Data != null ? Data.GetHashCode() : 0) ^ ItemId.GetHashCode();
        }

        public int GetOptionCount()
        {
            return StatsMap.GetAdditionalStats().Count()
                   + Skills.Count
                   + BuffSkills.Count;
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"itemId"] = ItemId.Serialize(),
                [(Text)"statsMap"] = StatsMap.Serialize(),
                [(Text)"skills"] = new List(Skills.Select(s => s.Serialize())),
                [(Text)"buffSkills"] = new List(BuffSkills.Select(s => s.Serialize())),
            }.Union((Dictionary)base.Serialize()));
    }
}
