using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class CharacterBase : ICloneable
    {
        public const decimal CriticalMultiplier = 1.5m;

        [NonSerialized] private Root _root;
        private Skill.Skill _selectedSkill;
        private BattleStatus.Skill _usedSkill;

        public readonly Guid Id = Guid.NewGuid();
        [NonSerialized] public readonly Simulator Simulator;

        public ElementalType atkElementType;
        public float attackRange;
        public ElementalType defElementType;

        public readonly Skills Skills = new Skills();
        public readonly Skills BuffSkills = new Skills();
        public readonly Dictionary<int, Buff.Buff> Buffs = new Dictionary<int, Buff.Buff>();
        public readonly List<CharacterBase> Targets = new List<CharacterBase>();

        public CharacterSheet.Row RowData { get; }
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
        public int DOG => Stats.DOG;
        public int SPD => Stats.SPD;

        public int CurrentHP
        {
            get => Stats.CurrentHP;
            set => Stats.CurrentHP = Math.Min(Math.Max(value, 0), HP);
        }

        public bool IsDead => CurrentHP <= 0;

        protected CharacterBase(Simulator simulator, TableSheets sheets, int characterId, int level)
        {
            Simulator = simulator;

            if (!sheets.CharacterSheet.TryGetValue(characterId, out var row))
                throw new SheetRowNotFoundException("CharacterSheet", characterId.ToString());

            RowData = row;
            Stats = new CharacterStats(RowData, level, sheets);
            Skills.Clear();

            atkElementType = RowData.ElementalType;
            attackRange = RowData.AttackRange;
            defElementType = RowData.ElementalType;
            CurrentHP = HP;
        }

        protected CharacterBase(CharacterBase value)
        {
            _root = value._root;
            _selectedSkill = value._selectedSkill;
            Id = value.Id;
            Simulator = value.Simulator;
            atkElementType = value.atkElementType;
            attackRange = value.attackRange;
            defElementType = value.defElementType;
            // 스킬은 변하지 않는다는 가정 하에 얕은 복사.
            Skills = value.Skills;
            // 버프는 컨테이너도 옮기고,
            Buffs = new Dictionary<int, Buff.Buff>();
            foreach (var pair in value.Buffs)
            {
                // 깊은 복사까지 꼭.
                Buffs.Add(pair.Key, (Buff.Buff) pair.Value.Clone());
            }

            // 타갯은 컨테이너만 옮기기.
            Targets = new List<CharacterBase>(value.Targets);
            // 캐릭터 테이블 데이타는 변하지 않는다는 가정 하에 얕은 복사.
            RowData = value.RowData;
            Stats = (CharacterStats) value.Stats.Clone();
        }

        public abstract object Clone();

        #region Behaviour Tree

        public void InitAI()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(IsAlive).OpenBranch(
                        BT.Sequence().OpenBranch(
                            BT.Call(BeginningOfTurn),
                            BT.Call(ReduceDurationOfBuffs),
                            BT.Call(SelectSkill),
                            BT.Call(UseSkill),
                            BT.Call(RemoveBuffs),
                            BT.Call(EndTurn)
                        )
                    ),
                    BT.Call(EndTurn),
                    BT.Terminate()
                )
            );
        }

        public void Tick()
        {
            _root.Tick();
        }

        private void BeginningOfTurn()
        {
            _selectedSkill = null;
            _usedSkill = null;
        }

        private void ReduceDurationOfBuffs()
        {
            // 자신의 기존 버프 턴 조절.
            foreach (var pair in Buffs)
            {
                pair.Value.remainedDuration--;
            }
        }

        private void SelectSkill()
        {
            _selectedSkill = Skills.Select(Simulator.Random);
        }

        private void UseSkill()
        {
            // 스킬 사용.
            _usedSkill = _selectedSkill.Use(this, Simulator.WaveTurn);
            Simulator.Log.Add(_usedSkill);

            foreach (var info in _usedSkill.SkillInfos)
            {
                if (info.Target.IsDead)
                {
                    var target = Targets.FirstOrDefault(i => i.Id == info.Target.Id);
                    if (target != null)
                        target.Die();
                }
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

        public bool IsCritical()
        {
            var chance = Simulator.Random.Next(0, 100);
            return chance < CRI;
        }

        public bool IsCritical(ElementalResult result)
        {
            var correction = result == ElementalResult.Win ? 50 : 0;
            var chance = Simulator.Random.Next(0, 100);
            return CRI + correction > chance;
        }

        public bool IsHit(ElementalResult result)
        {
            var correction = result == ElementalResult.Lose ? 50 : 0;
            var chance = Simulator.Random.Next(0, 100);
            return chance >= Stats.DOG + correction;
        }

        private bool IsAlive()
        {
            return !IsDead;
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
            if (!Simulator.TableSheets.SkillSheet.TryGetValue(100000, out var skillRow))
                throw new KeyNotFoundException("100000");

            var attack = SkillFactory.Get(skillRow, 0, 100);
            Skills.Add(attack);
        }

        public bool GetChance(int chance)
        {
            return chance > Simulator.Random.Next(0, 100);
        }

        protected virtual void EndTurn()
        {
        }
    }

    public class InformationFieldAttribute : Attribute
    {
    }

    [Serializable]
    public class Skills : IEnumerable<Skill.Skill>
    {
        private readonly List<Skill.Skill> _skills = new List<Skill.Skill>();

        public void Add(Skill.Skill s)
        {
            if (s is null)
            {
                return;
            }

            _skills.Add(s);
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

        public Skill.Skill Select(IRandom random)
        {
            var selected = _skills
                .Select(skill => new {skill, chance = random.Next(0, 100)})
                .Where(t => t.skill.chance > t.chance)
                .OrderBy(t => t.skill.skillRow.Id)
                .ThenBy(t => t.chance == 0 ? 1m : (decimal) t.chance / t.skill.chance)
                .Select(t => t.skill)
                .ToList();

            return selected[random.Next(selected.Count)];
        }
    }
}
