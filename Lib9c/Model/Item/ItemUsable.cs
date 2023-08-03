using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    // todo: 소모품과 장비가 함께 쓰기에는 장비 위주의 모델이 된 느낌. 아이템 정리하면서 정리를 흐음..
    [Serializable]
    public abstract class ItemUsable : ItemBase, INonFungibleItem
    {
        public Guid ItemId
        {
            get
            {
                if (_serializedItemId is { })
                {
                    _itemId = _serializedItemId.ToGuid();
                    _serializedItemId = null;
                }

                return _itemId;
            }
        }

        public Guid TradableId => ItemId;
        public Guid NonFungibleId => ItemId;

        public StatsMap StatsMap
        {
            get
            {
                _statsMap ??= new StatsMap();
                if (_serializedStatsMap is { })
                {
                    _statsMap.Deserialize(_serializedStatsMap);
                    _serializedStatsMap = null;
                }

                return _statsMap;
            }
        }

        public List<Skill.Skill> Skills
        {
            get
            {
                _skills ??= new List<Skill.Skill>();
                if (_serializedSkills is { })
                {
                    foreach (var value in _serializedSkills)
                    {
                        var serializedSkill = (Dictionary) value;
                        _skills.Add(SkillFactory.Deserialize(serializedSkill));
                    }

                    _serializedSkills = null;
                }

                return _skills;
            }
        }

        public List<BuffSkill> BuffSkills
        {
            get
            {
                _buffSkills ??= new List<BuffSkill>();
                if (_serializedBuffSkills is { })
                {
                    foreach (var value in _serializedBuffSkills)
                    {
                        var serializedSkill = (Dictionary) value;
                        _buffSkills.Add((BuffSkill) SkillFactory.Deserialize(serializedSkill));
                    }

                    _serializedBuffSkills = null;
                }

                return _buffSkills;
            }
        }

        public long RequiredBlockIndex
        {
            get => _requiredBlockIndex;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(RequiredBlockIndex)} must be greater than 0, but {value}");
                }
                _requiredBlockIndex = value;
            }
        }

        private long _requiredBlockIndex;
        private Guid _itemId;
        private StatsMap _statsMap;
        private List<Skill.Skill> _skills;
        private List<BuffSkill> _buffSkills;
        private Binary? _serializedItemId;
        private Dictionary _serializedStatsMap;
        private List _serializedSkills;
        private List _serializedBuffSkills;

        protected ItemUsable(ItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data)
        {
            _itemId = id;
            _statsMap = new StatsMap();

            switch (data)
            {
                case ConsumableItemSheet.Row consumableItemRow:
                {
                    foreach (var statData in consumableItemRow.Stats)
                    {
                        StatsMap.AddStatValue(statData.StatType, statData.BaseValue);
                    }

                    break;
                }
                case EquipmentItemSheet.Row equipmentItemRow:
                    StatsMap.AddStatValue(equipmentItemRow.Stat.StatType, equipmentItemRow.Stat.BaseValue);
                    break;
            }

            _skills = new List<Model.Skill.Skill>();
            _buffSkills = new List<BuffSkill>();
            RequiredBlockIndex = requiredBlockIndex;
        }

        protected ItemUsable(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "itemId", out var itemId))
            {
                _serializedItemId = (Binary) itemId;
            }
            if (serialized.TryGetValue((Text) "statsMap", out var statsMap))
            {
                _serializedStatsMap = (Dictionary) statsMap;
            }
            if (serialized.TryGetValue((Text) "skills", out var skills))
            {
                _serializedSkills = (List) skills;
            }
            if (serialized.TryGetValue((Text) "buffSkills", out var buffSkills))
            {
                _serializedBuffSkills = (List) buffSkills;
            }
            if (serialized.TryGetValue((Text) "requiredBlockIndex", out var requiredBlockIndex))
            {
                RequiredBlockIndex = requiredBlockIndex.ToLong();
            }
        }

        protected ItemUsable(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        protected bool Equals(ItemUsable other)
        {
            return base.Equals(other) && Equals(ItemId, other.ItemId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemUsable) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ ItemId.GetHashCode();
            }
        }

        public int GetOptionCount()
        {
            return StatsMap.GetAdditionalStats(true).Count()
                   + Skills.Count
                   + BuffSkills.Count;
        }

        public void Update(long blockIndex)
        {
            RequiredBlockIndex = blockIndex;
        }

        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add("itemId", _serializedItemId ?? ItemId.Serialize())
            .Add("statsMap", _serializedStatsMap ?? StatsMap.Serialize())
            .Add("skills", _serializedSkills ?? new List(Skills
                .OrderByDescending(i => i.Chance)
                .ThenByDescending(i => i.Power)
                .Select(s => s.Serialize())))
            .Add("buffSkills", _serializedBuffSkills ?? new List(BuffSkills
                .OrderByDescending(i => i.Chance)
                .ThenByDescending(i => i.Power)
                .Select(s => s.Serialize())))
            .Add("requiredBlockIndex", RequiredBlockIndex.Serialize());
    }
}
