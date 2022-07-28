using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BTAI;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Model.Arena;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    public class ArenaCharacter : ICloneable
    {
        public const decimal CriticalMultiplier = 1.5m;

        private readonly SkillSheet _skillSheet;
        private readonly SkillBuffSheet _skillBuffSheet;
        private readonly BuffSheet _buffSheet;
        private readonly ArenaSimulator _simulator;
        private readonly CharacterStats _stats;
        private readonly ArenaSkills _skills;

        private readonly int _attackCountMax;

        private ArenaCharacter _target;
        private int _attackCount;

        public Guid Id { get; } = Guid.NewGuid();
        public BattleStatus.Arena.ArenaSkill SkillLog { get; private set; }
        public ElementalType OffensiveElementalType { get; }
        public ElementalType DefenseElementalType { get; }
        public SizeType SizeType { get; }
        public float RunSpeed { get; }
        public float AttackRange { get; }
        public int CharacterId { get; }
        public bool IsEnemy { get; }

        public int Level
        {
            get => _stats.Level;
            set => _stats.SetLevel(value);
        }
        public int CurrentHP
        {
            get => _stats.CurrentHP;
            set => _stats.CurrentHP = value;
        }
        public int HP => _stats.HP;
        public int AdditionalHP => _stats.BuffStats.HP;
        public int ATK => _stats.ATK;
        public int DEF => _stats.DEF;
        public int CRI => _stats.CRI;
        public int HIT => _stats.HIT;
        public int SPD => _stats.SPD;
        public bool IsDead => CurrentHP <= 0;

        public Dictionary<int, Buff.Buff> Buffs { get; } = new Dictionary<int, Buff.Buff>();

        public object Clone() => new ArenaCharacter(this);

        public ArenaCharacter(
            ArenaSimulator simulator,
            ArenaPlayerDigest digest,
            ArenaSimulatorSheets sheets,
            bool isEnemy = false)
        {
            OffensiveElementalType = GetElementalType(digest.Equipments, ItemSubType.Weapon);
            DefenseElementalType = GetElementalType(digest.Equipments, ItemSubType.Armor);
            var row = CharacterRow(digest.CharacterId, sheets);
            SizeType = row?.SizeType ?? SizeType.S;
            RunSpeed = row?.RunSpeed ?? 1f;
            AttackRange = row?.AttackRange ?? 1f;
            CharacterId = digest.CharacterId;
            IsEnemy = isEnemy;

            _skillSheet = sheets.SkillSheet;
            _skillBuffSheet = sheets.SkillBuffSheet;
            _buffSheet = sheets.BuffSheet;

            _simulator = simulator;
            _stats = GetStat(digest, sheets);
            _skills = GetSkills(digest.Equipments, sheets.SkillSheet);
            _attackCountMax = AttackCountHelper.GetCountMax(digest.Level);
        }

        private ArenaCharacter(ArenaCharacter value)
        {
            Id = value.Id;
            SkillLog = value.SkillLog;
            OffensiveElementalType = value.OffensiveElementalType;
            DefenseElementalType = value.OffensiveElementalType;
            SizeType = value.SizeType;
            RunSpeed = value.RunSpeed;
            AttackRange = value.AttackRange;
            CharacterId = value.CharacterId;
            IsEnemy = value.IsEnemy;

            _skillSheet = value._skillSheet;
            _skillBuffSheet = value._skillBuffSheet;
            _buffSheet = value._buffSheet;

            _simulator = value._simulator;
            _stats = new CharacterStats(value._stats);
            _skills = value._skills;
            Buffs = new Dictionary<int, Buff.Buff>();
#pragma warning disable LAA1002
            foreach (var pair in value.Buffs)
#pragma warning restore LAA1002
            {
                Buffs.Add(pair.Key, (Buff.Buff) pair.Value.Clone());
            }

            _attackCountMax = value._attackCount;
            _attackCount = value._attackCount;
            _target = value._target;
        }

        private ElementalType GetElementalType(IEnumerable<Equipment> equipments, ItemSubType itemSubType)
        {
            var equipment = equipments.FirstOrDefault(x => x.ItemSubType.Equals(itemSubType));
            return equipment?.ElementalType ?? ElementalType.Normal;
        }

        private static CharacterSheet.Row CharacterRow(int characterId, ArenaSimulatorSheets sheets)
        {
            if (!sheets.CharacterSheet.TryGetValue(characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", characterId);
            }

            return row;
        }


        private static CharacterStats GetStat(ArenaPlayerDigest digest, ArenaSimulatorSheets sheets)
        {
            var row = CharacterRow(digest.CharacterId, sheets);
            var stats = new CharacterStats(row, digest.Level);
            stats.SetEquipments(digest.Equipments, sheets.EquipmentItemSetEffectSheet);

            var options = new List<StatModifier>();
            foreach (var itemId in digest.Costumes.Select(costume => costume.Id))
            {
                if (TryGetStats(sheets.CostumeStatSheet, itemId, out var option))
                {
                    options.AddRange(option);
                }
            }

            stats.SetOption(options);
            stats.IncreaseHpForArena();
            stats.EqualizeCurrentHPWithHP();
            return stats;
        }

        private static bool TryGetStats(
            CostumeStatSheet statSheet,
            int itemId,
            out IEnumerable<StatModifier> statModifiers)
        {
            statModifiers = statSheet.OrderedList
                .Where(r => r.CostumeId == itemId)
                .Select(row =>
                    new StatModifier(row.StatType, StatModifier.OperationType.Add, (int)row.Stat));

            return statModifiers.Any();
        }

        private static ArenaSkills GetSkills(IEnumerable<Equipment> equipments, SkillSheet skillSheet)
        {
            var skills = new ArenaSkills();

            // normal attack
            if (!skillSheet.TryGetValue(GameConfig.DefaultAttackId, out var skillRow))
            {
                throw new KeyNotFoundException(GameConfig.DefaultAttackId.ToString(CultureInfo.InvariantCulture));
            }

            var attack = SkillFactory.GetForArena(skillRow, 0, 100);
            skills.Add(attack);

            foreach (var skill in equipments.SelectMany(equipment => equipment.Skills))
            {
                var arenaSkill = SkillFactory.GetForArena(skill.SkillRow, skill.Power, skill.Chance);
                skills.Add(arenaSkill);
            }

            foreach (var buff in equipments.SelectMany(equipment => equipment.BuffSkills))
            {
                var buffSkill = SkillFactory.GetForArena(buff.SkillRow, buff.Power, buff.Chance);
                skills.Add(buffSkill);
            }

            return skills;
        }

        #region Behaviour Tree

        [NonSerialized]
        private Root _root;

        private void InitAI()
        {
            _root = new Root();
            _root.OpenBranch(
                BT.Call(Act)
            );
        }

        [Obsolete("Use InitAI")]
        private void InitAIV1()
        {
            _root = new Root();
            _root.OpenBranch(
                BT.Call(ActV1)
            );
        }

        private void Act()
        {
            if (IsDead)
            {
                return;
            }

            ReduceDurationOfBuffs();
            ReduceSkillCooldown();
            UseSkill();
            RemoveBuffs();
        }

        [Obsolete("Use Act")]
        private void ActV1()
        {
            if (IsDead)
            {
                return;
            }

            ReduceDurationOfBuffs();
            ReduceSkillCooldown();
            UseSkillV1();
            RemoveBuffsV1();
        }

        private void ReduceDurationOfBuffs()
        {
#pragma warning disable LAA1002
            foreach (var pair in Buffs)
#pragma warning restore LAA1002
            {
                pair.Value.remainedDuration--;
            }
        }

        private void ReduceSkillCooldown()
        {
            _skills.ReduceCooldown();
        }

        private void UseSkill()
        {
            var selectedSkill = _skills.Select(_simulator.Random);
            SkillLog = selectedSkill.Use(
                this,
                _target,
                _simulator.Turn,
                BuffFactory.GetBuffs(selectedSkill, _skillBuffSheet, _buffSheet)
            );

            if (!_skillSheet.TryGetValue(selectedSkill.SkillRow.Id, out var row))
            {
                throw new KeyNotFoundException(
                    selectedSkill.SkillRow.Id.ToString(CultureInfo.InvariantCulture));
            }

            _skills.SetCooldown(selectedSkill.SkillRow.Id, row.Cooldown);
        }

        [Obsolete("Use UseSkill")]
        private void UseSkillV1()
        {
            var selectedSkill = _skills.Select(_simulator.Random);
            SkillLog = selectedSkill.UseV1(
                this,
                _target,
                _simulator.Turn,
                BuffFactory.GetBuffs(selectedSkill, _skillBuffSheet, _buffSheet)
            );

            if (!_skillSheet.TryGetValue(selectedSkill.SkillRow.Id, out var row))
            {
                throw new KeyNotFoundException(
                    selectedSkill.SkillRow.Id.ToString(CultureInfo.InvariantCulture));
            }

            _skills.SetCooldown(selectedSkill.SkillRow.Id, row.Cooldown);
        }

        private void RemoveBuffs()
        {
            var isApply = false;

            foreach (var key in Buffs.Keys.ToList())
            {
                var buff = Buffs[key];
                if (buff.remainedDuration > 0)
                {
                    continue;
                }

                Buffs.Remove(key);
                isApply = true;
            }

            if (isApply)
            {
                _stats.SetBuffs(Buffs.Values);
                _stats.IncreaseHpForArena();
            }
        }

        [Obsolete("Use RemoveBuffs")]
        private void RemoveBuffsV1()
        {
            var isApply = false;

            foreach (var key in Buffs.Keys.ToList())
            {
                var buff = Buffs[key];
                if (buff.remainedDuration > 0)
                {
                    continue;
                }

                Buffs.Remove(key);
                isApply = true;
            }

            if (isApply)
            {
                _stats.SetBuffs(Buffs.Values);
            }
        }

        public void Tick()
        {
            _root.Tick();
        }
        #endregion

        public void Spawn(ArenaCharacter target)
        {
            _target = target;
            InitAI();
        }

        [Obsolete("Use Spawn")]
        public void SpawnV1(ArenaCharacter target)
        {
            _target = target;
            InitAIV1();
        }

        public void AddBuff(Buff.Buff buff, bool updateImmediate = true)
        {
            if (Buffs.TryGetValue(buff.RowData.GroupId, out var outBuff) &&
                outBuff.RowData.Id > buff.RowData.Id)
                return;

            var clone = (Buff.Buff) buff.Clone();
            Buffs[buff.RowData.GroupId] = clone;
            _stats.AddBuff(clone, updateImmediate);
            _stats.IncreaseHpForArena();
        }

        [Obsolete("Use AddBuff")]
        public void AddBuffV1(Buff.Buff buff, bool updateImmediate = true)
        {
            if (Buffs.TryGetValue(buff.RowData.GroupId, out var outBuff) &&
                outBuff.RowData.Id > buff.RowData.Id)
                return;

            var clone = (Buff.Buff) buff.Clone();
            Buffs[buff.RowData.GroupId] = clone;
            _stats.AddBuff(clone, updateImmediate);
        }

        public void Heal(int heal)
        {
            CurrentHP += heal;
        }

        public bool IsCritical(bool considerAttackCount = true)
        {
            var chance = _simulator.Random.Next(0, 100);
            if (!considerAttackCount)
                return CRI >= chance;

            var additionalCriticalChance =
                (int) AttackCountHelper.GetAdditionalCriticalChance(_attackCount, _attackCountMax);
            return CRI + additionalCriticalChance >= chance;
        }

        public virtual bool IsHit(ArenaCharacter caster)
        {
            var isHit = HitHelper.IsHitForArena(
                caster.Level,
                caster.HIT,
                Level,
                HIT,
                _simulator.Random.Next(0, 100));
            if (!isHit)
            {
                caster._attackCount = 0;
            }

            return isHit;
        }

        public int GetDamage(int damage, bool considerAttackCount = true)
        {
            if (!considerAttackCount)
                return damage;

            _attackCount++;
            if (_attackCount > _attackCountMax)
            {
                _attackCount = 1;
            }

            var damageMultiplier = (int) AttackCountHelper.GetDamageMultiplier(_attackCount, _attackCountMax);
            damage *= damageMultiplier;
            return damage;
        }
    }
}
