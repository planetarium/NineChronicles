// #define TEST_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class CharacterBase : ICloneable
    {
        public const decimal CriticalMultiplier = 1.5m;

        public readonly Guid Id = Guid.NewGuid();

        [NonSerialized]
        public readonly Simulator Simulator;

        public ElementalType atkElementType;
        public float attackRange;
        public ElementalType defElementType;

        public readonly Skills Skills = new Skills();
        public readonly Skills BuffSkills = new Skills();
        public readonly Dictionary<int, Buff.Buff> Buffs = new Dictionary<int, Buff.Buff>();
        public readonly List<CharacterBase> Targets = new List<CharacterBase>();

        public CharacterSheet.Row RowData { get; }
        public int CharacterId { get; }
        public SizeType SizeType => RowData?.SizeType ?? SizeType.S;
        public float RunSpeed => RowData?.RunSpeed ?? 1f;
        public CharacterStats Stats { get; }

        public int Level
        {
            get => Stats.Level;
            set => Stats.SetLevel(value);
        }

        public int HP => Stats.HP;
        public int ATK => Stats.ATK;
        public int DEF => Stats.DEF;
        public int CRI => Stats.CRI;
        public int HIT => Stats.HIT;
        public int SPD => Stats.SPD;

        public int CurrentHP
        {
            get => Stats.CurrentHP;
            set => Stats.CurrentHP = value;
        }

        public bool IsDead => CurrentHP <= 0;

        public int AttackCount { get; private set; }
        public int AttackCountMax { get; protected set; }

        protected CharacterBase(Simulator simulator, CharacterSheet characterSheet, int characterId, int level,
            IEnumerable<StatModifier> optionalStatModifiers = null)
        {
            Simulator = simulator;

            if (!characterSheet.TryGetValue(characterId, out var row))
                throw new SheetRowNotFoundException("CharacterSheet", characterId);

            RowData = row;
            CharacterId = characterId;
            Stats = new CharacterStats(RowData, level);
            if (!(optionalStatModifiers is null))
            {
                Stats.AddOption(optionalStatModifiers);
            }

            Skills.Clear();

            atkElementType = RowData.ElementalType;
            attackRange = RowData.AttackRange;
            defElementType = RowData.ElementalType;
            CurrentHP = HP;
            AttackCountMax = 0;
        }

        protected CharacterBase(CharacterBase value)
        {
            _root = value._root;
            Id = value.Id;
            Simulator = value.Simulator;
            atkElementType = value.atkElementType;
            attackRange = value.attackRange;
            defElementType = value.defElementType;
            // 스킬은 변하지 않는다는 가정 하에 얕은 복사.
            Skills = value.Skills;
            // 버프는 컨테이너도 옮기고,
            Buffs = new Dictionary<int, Buff.Buff>();
#pragma warning disable LAA1002
            foreach (var pair in value.Buffs)
#pragma warning restore LAA1002
            {
                // 깊은 복사까지 꼭.
                Buffs.Add(pair.Key, (Buff.Buff) pair.Value.Clone());
            }

            // 타갯은 컨테이너만 옮기기.
            Targets = new List<CharacterBase>(value.Targets);
            // 캐릭터 테이블 데이타는 변하지 않는다는 가정 하에 얕은 복사.
            RowData = value.RowData;
            Stats = new CharacterStats(value.Stats);
            AttackCountMax = value.AttackCountMax;
            CharacterId = value.CharacterId;
        }

        public abstract object Clone();

        #region Behaviour Tree

        [NonSerialized]
        private Root _root;

        protected CharacterBase(CharacterSheet.Row row)
        {
            RowData = row;
        }

        public void InitAI()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Call(Act)
            );
        }

        [Obsolete("Use InitAI")]
        public void InitAI2()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Call(Act2)
            );
        }

        [Obsolete("Use InitAI")]
        public void InitAI3()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Call(Act3)
            );
        }

        public void Tick()
        {
            _root.Tick();
        }

        private bool IsAlive()
        {
            return !IsDead;
        }

        private void ReduceDurationOfBuffs()
        {
            // 자신의 기존 버프 턴 조절.
#pragma warning disable LAA1002
            foreach (var pair in Buffs)
#pragma warning restore LAA1002
            {
                pair.Value.remainedDuration--;
            }
        }

        private void ReduceSkillCooldown()
        {
            Skills.ReduceCooldown();
        }

        private void UseSkill()
        {
            // 스킬 선택.
            var selectedSkill = Skills.Select(Simulator.Random);

            // 스킬 사용.
            var usedSkill = selectedSkill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    selectedSkill,
                    Simulator.SkillBuffSheet,
                    Simulator.BuffSheet
                )
            );

            // 쿨다운 적용.
            Skills.SetCooldown(selectedSkill.SkillRow.Id, selectedSkill.SkillRow.Cooldown);
            Simulator.Log.Add(usedSkill);

            foreach (var info in usedSkill.SkillInfos)
            {
                if (!info.Target.IsDead)
                    continue;

                var target = Targets.FirstOrDefault(i => i.Id == info.Target.Id);
                target?.Die();
            }
        }

        [Obsolete("Use UseSkill")]
        private void UseSkill2()
        {
            // 스킬 선택.
            var selectedSkill = Skills.Select2(Simulator.Random);

            // 스킬 사용.
            var usedSkill = selectedSkill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    selectedSkill,
                    Simulator.SkillBuffSheet,
                    Simulator.BuffSheet
                )
            );

            // 쿨다운 적용.
            Skills.SetCooldown(selectedSkill.SkillRow.Id, selectedSkill.SkillRow.Cooldown);
            Simulator.Log.Add(usedSkill);

            foreach (var info in usedSkill.SkillInfos)
            {
                if (!info.Target.IsDead)
                    continue;

                var target = Targets.FirstOrDefault(i => i.Id == info.Target.Id);
                target?.Die();
            }
        }

        [Obsolete("Use UseSkill")]
        private void UseSkill3()
        {
            // 스킬 선택.
            var selectedSkill = Skills.Select3(Simulator.Random);

            // 스킬 사용.
            var usedSkill = selectedSkill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    selectedSkill,
                    Simulator.SkillBuffSheet,
                    Simulator.BuffSheet
                )
            );

            // 쿨다운 적용.
            Skills.SetCooldown(selectedSkill.SkillRow.Id, selectedSkill.SkillRow.Cooldown);
            Simulator.Log.Add(usedSkill);

            foreach (var info in usedSkill.SkillInfos)
            {
                if (!info.Target.IsDead)
                    continue;

                var target = Targets.FirstOrDefault(i => i.Id == info.Target.Id);
                target?.Die();
            }
        }

        private void RemoveBuffs()
        {
            var isDirtyMySelf = false;

            // 자신의 버프 제거.
            var keyList = Buffs.Keys.ToList();
            foreach (var key in keyList)
            {
                var buff = Buffs[key];
                if (buff.remainedDuration > 0)
                    continue;

                Buffs.Remove(key);
                isDirtyMySelf = true;
            }

            if (!isDirtyMySelf)
                return;

            // 버프를 상태에 반영.
            Stats.SetBuffs(Buffs.Values);
            Simulator.Log.Add(new RemoveBuffs((CharacterBase) Clone()));
        }

        protected virtual void EndTurn()
        {
#if TEST_LOG
            UnityEngine.Debug.LogWarning($"{nameof(RowData.Id)} : {RowData.Id} / Turn Ended.");
#endif
        }

        #endregion

        #region Buff

        public void AddBuff(Buff.Buff buff, bool updateImmediate = true)
        {
            if (Buffs.TryGetValue(buff.RowData.GroupId, out var outBuff) &&
                outBuff.RowData.Id > buff.RowData.Id)
                return;

            var clone = (Buff.Buff) buff.Clone();
            Buffs[buff.RowData.GroupId] = clone;
            Stats.AddBuff(clone, updateImmediate);
        }

        #endregion

        public bool IsCritical(bool considerAttackCount = true)
        {
            var chance = Simulator.Random.Next(0, 100);
            if (!considerAttackCount)
                return CRI >= chance;

            var additionalCriticalChance =
                (int) AttackCountHelper.GetAdditionalCriticalChance(AttackCount, AttackCountMax);
            return CRI + additionalCriticalChance >= chance;
        }

        public bool IsHit(ElementalResult result)
        {
            var correction = result == ElementalResult.Lose ? 50 : 0;
            var chance = Simulator.Random.Next(0, 100);
            return chance >= Stats.HIT + correction;
        }

        public virtual bool IsHit(CharacterBase caster)
        {
            var isHit = HitHelper.IsHit(caster.Level, caster.HIT, Level, HIT, Simulator.Random.Next(0, 100));
            if (!isHit)
            {
                caster.AttackCount = 0;
            }

            return isHit;
        }

        public int GetDamage(int damage, bool considerAttackCount = true)
        {
            if (!considerAttackCount)
                return damage;

            AttackCount++;
            if (AttackCount > AttackCountMax)
            {
                AttackCount = 1;
            }

            var damageMultiplier = (int) AttackCountHelper.GetDamageMultiplier(AttackCount, AttackCountMax);
            damage *= damageMultiplier;

#if TEST_LOG
            var sb = new StringBuilder(RowData.Id.ToString());
            sb.Append($" / {nameof(AttackCount)}: {AttackCount}");
            sb.Append($" / {nameof(AttackCountMax)}: {AttackCountMax}");
            sb.Append($" / {nameof(damageMultiplier)}: {damageMultiplier}");
            sb.Append($" / {nameof(damage)}: {damage}");
            Debug.LogWarning(sb.ToString());
#endif

            return damage;
        }

        public void Die()
        {
            OnDead();
        }

        protected virtual void OnDead()
        {
            var dead = new Dead((CharacterBase) Clone());
            Simulator.Log.Add(dead);
        }

        public void Heal(int heal)
        {
            CurrentHP += heal;
        }

        protected virtual void SetSkill()
        {
            if (!Simulator.SkillSheet.TryGetValue(GameConfig.DefaultAttackId, out var skillRow))
            {
                throw new KeyNotFoundException("100000");
            }

            var attack = SkillFactory.Get(skillRow, 0, 100);
            Skills.Add(attack);
        }

        public bool GetChance(int chance)
        {
            return chance > Simulator.Random.Next(0, 100);
        }

        private void Act()
        {
            if (IsAlive())
            {
                ReduceDurationOfBuffs();
                ReduceSkillCooldown();
                UseSkill();
                RemoveBuffs();
            }
            EndTurn();
        }

        [Obsolete("Use Act")]
        private void Act2()
        {
            if (IsAlive())
            {
                ReduceDurationOfBuffs();
                ReduceSkillCooldown();
                UseSkill2();
                RemoveBuffs();
            }
            EndTurn();
        }

        [Obsolete("Use Act")]
        private void Act3()
        {
            if (IsAlive())
            {
                ReduceDurationOfBuffs();
                ReduceSkillCooldown();
                UseSkill3();
                RemoveBuffs();
            }
            EndTurn();
        }
    }

    [Serializable]
    public class Skills : IEnumerable<Skill.Skill>
    {
        private readonly List<Skill.Skill> _skills = new List<Skill.Skill>();
        private Dictionary<int, int> _skillsCooldown = new Dictionary<int, int>();

        public void Add(Skill.Skill skill)
        {
            if (skill is null)
            {
                return;
            }

            _skills.Add(skill);
        }

        public void Clear()
        {
            _skills.Clear();
        }

        public IEnumerator<Skill.Skill> GetEnumerator()
        {
            return _skills.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void SetCooldown(int skillId, int cooldown)
        {
            if (_skills.All(e => e.SkillRow.Id != skillId))
                throw new Exception(
                    $"[{nameof(Skills)}.{nameof(SetCooldown)}()] Not found {nameof(skillId)}({skillId})");

            _skillsCooldown[skillId] = cooldown;
        }

        public int GetCooldown(int skillId)
        {
            return _skillsCooldown.ContainsKey(skillId)
                ? _skillsCooldown[skillId]
                : 0;
        }

        public void ReduceCooldown()
        {
#pragma warning disable LAA1002
            if (!_skillsCooldown.Any())
#pragma warning restore LAA1002
                return;

            foreach (var key in _skillsCooldown.Keys.OrderBy(i => i))
            {
                var value = _skillsCooldown[key];
                if (value <= 1)
                {
                    _skillsCooldown.Remove(key);
                    continue;
                }

                _skillsCooldown[key] = value - 1;
            }
        }

        public Skill.Skill Select(IRandom random)
        {
            return PostSelect(random, GetSelectableSkills());
        }

        [Obsolete("Use Select")]
        public Skill.Skill Select2(IRandom random)
        {
            return PostSelect2(random, GetSelectableSkills());
        }

        [Obsolete("Use Select")]
        public Skill.Skill Select3(IRandom random)
        {
            return PostSelect3(random, GetSelectableSkills());
        }

        private IEnumerable<Skill.Skill> GetSelectableSkills()
        {
            return _skills.Where(skill => !_skillsCooldown.ContainsKey(skill.SkillRow.Id));
        }

        private Skill.Skill PostSelect(IRandom random, IEnumerable<Skill.Skill> skills)
        {
            var defaultAttack = skills.FirstOrDefault(x => x.SkillRow.Id == GameConfig.DefaultAttackId);
            if (defaultAttack == null)
            {
                throw new Exception($"[There is no default attack");
            }

            var sortedSkills = skills.Where(x => x.SkillRow.Id != GameConfig.DefaultAttackId);
            var randomValue = random.Next(0, 100);
            var selectedSkills = sortedSkills.Where(x => x.Chance > randomValue).ToList();
            return selectedSkills.Any() ? selectedSkills[random.Next(selectedSkills.Count)] : defaultAttack;
        }

        [Obsolete("Use PostSelect")]
        private Skill.Skill PostSelect2(IRandom random, IEnumerable<Skill.Skill> skills)
        {
            var selected = skills
                .Select(skill => new {skill, chance = random.Next(0, 100)})
                .Where(t => t.skill.Chance > t.chance)
                .OrderBy(t => t.skill.SkillRow.Id)
                .ThenBy(t => t.chance == 0 ? 1m : (decimal) t.chance / t.skill.Chance)
                .Select(t => t.skill)
                .ToList();

            return selected.Any()
                ? selected[random.Next(selected.Count)]
                : throw new Exception($"[{nameof(Skills)}] There is no selected skills");
        }

        [Obsolete("Use PostSelect")]
        private Skill.Skill PostSelect3(IRandom random, IEnumerable<Skill.Skill> skills)
        {
            var selected = skills
                .OrderBy(skill => skill.SkillRow.Id)
                .Select(skill => new {skill, chance = random.Next(0, 100)})
                .Where(t => t.skill.Chance > t.chance)
                .OrderBy(t => t.skill.SkillRow.Id)
                .ThenBy(t => t.chance == 0 ? 1m : (decimal) t.chance / t.skill.Chance)
                .Select(t => t.skill)
                .ToList();

            return selected.Any()
                ? selected[random.Next(selected.Count)]
                : throw new Exception($"[{nameof(Skills)}] There is no selected skills");
        }
    }
}
