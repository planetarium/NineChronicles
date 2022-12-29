using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BTAI;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Quest;
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
        public Simulator Simulator;

        public ElementalType atkElementType;
        public float attackRange;
        public ElementalType defElementType;

        public readonly Skills Skills = new Skills();
        public readonly Skills BuffSkills = new Skills();
        public readonly Dictionary<int, Buff.Buff> Buffs = new Dictionary<int, Buff.Buff>();
        public IEnumerable<StatBuff> StatBuffs => Buffs.Values.OfType<StatBuff>();
        public IEnumerable<ActionBuff> ActionBuffs => Buffs.Values.OfType<ActionBuff>();
        public readonly List<CharacterBase> Targets = new List<CharacterBase>();

        public CharacterSheet.Row RowData { get; }
        public int CharacterId { get; }
        public SizeType SizeType { get; }
        public float RunSpeed { get; }
        public CharacterStats Stats { get; }

        public int Level
        {
            get => Stats.Level;
            set => Stats.SetStats(value);
        }

        public int HP => Stats.HP;
        public int ATK => Stats.ATK;
        public int DEF => Stats.DEF;
        public int CRI => Stats.CRI;
        public int HIT => Stats.HIT;
        public int SPD => Stats.SPD;
        public int DRV => Stats.DRV;
        public int DRR => Stats.DRR;
        public int CDMG => Stats.CDMG;

        public int CurrentHP
        {
            get => Stats.CurrentHP;
            set => Stats.CurrentHP = value;
        }

        public bool IsDead => CurrentHP <= 0;

        public int AttackCount { get; set; }
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

            SizeType = RowData.SizeType;
            atkElementType = RowData.ElementalType;
            defElementType = RowData.ElementalType;
            RunSpeed = RowData.RunSpeed;
            attackRange = RowData.AttackRange;
            CurrentHP = HP;
            AttackCountMax = 0;
        }

        protected CharacterBase(
            Simulator simulator,
            CharacterStats stat,
            int characterId,
            ElementalType elementalType,
            SizeType sizeType = SizeType.XL,
            float attackRange = 4,
            float runSpeed = 0.3f)
        {
            Simulator = simulator;
            Stats = stat;

            CharacterId = characterId;
            SizeType = sizeType;
            atkElementType = elementalType;
            defElementType = elementalType;
            this.attackRange = attackRange;
            RunSpeed = runSpeed;

            Skills.Clear();
            CurrentHP = HP;
            AttackCountMax = 0;
        }

        protected CharacterBase(CharacterBase value)
        {
            _root = value._root;
            Id = value.Id;
            Simulator = value.Simulator;

            CharacterId = value.CharacterId;
            SizeType = value.SizeType;
            atkElementType = value.atkElementType;
            defElementType = value.defElementType;
            attackRange = value.attackRange;
            RunSpeed = value.RunSpeed;

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
        }

        public abstract object Clone();

        #region Behaviour Tree

        [NonSerialized]
        private Root _root;

        protected CharacterBase(CharacterSheet.Row row)
        {
            RowData = row;
        }

        public virtual void InitAI()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Call(Act)
            );
        }

        [Obsolete("Use InitAI")]
        public void InitAIV1()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Call(ActV1)
            );
        }

        [Obsolete("Use InitAI")]
        public void InitAIV2()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Call(ActV2)
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
#pragma warning disable LAA1002
            foreach (var pair in Buffs)
#pragma warning restore LAA1002
            {
                pair.Value.RemainedDuration--;
            }
        }
        
        protected virtual void ReduceSkillCooldown()
        {
            Skills.ReduceCooldown();
        }

        [Obsolete("ReduceSkillCooldown")]
        private void ReduceSkillCooldownV1()
        {
            Skills.ReduceCooldownV1();
        }

        protected virtual BattleStatus.Skill UseSkill()
        {
            var selectedSkill = Skills.Select(Simulator.Random);
            var usedSkill = selectedSkill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    ATK,
                    selectedSkill,
                    Simulator.SkillBuffSheet,
                    Simulator.StatBuffSheet,
                    Simulator.SkillActionBuffSheet,
                    Simulator.ActionBuffSheet
                )
            );

            if (!Simulator.SkillSheet.TryGetValue(selectedSkill.SkillRow.Id, out var sheetSkill))
            {
                throw new KeyNotFoundException(selectedSkill.SkillRow.Id.ToString(CultureInfo.InvariantCulture));
            }

            Skills.SetCooldown(selectedSkill.SkillRow.Id, sheetSkill.Cooldown);
            Simulator.Log.Add(usedSkill);
            return usedSkill;
        }

        [Obsolete("Use UseSkill")]
        private BattleStatus.Skill UseSkillV1()
        {
            var selectedSkill = Skills.SelectV1(Simulator.Random);

            var usedSkill = selectedSkill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    ATK,
                    selectedSkill,
                    Simulator.SkillBuffSheet,
                    Simulator.StatBuffSheet,
                    Simulator.SkillActionBuffSheet,
                    Simulator.ActionBuffSheet
                )
            );

            Skills.SetCooldown(selectedSkill.SkillRow.Id, selectedSkill.SkillRow.Cooldown);
            Simulator.Log.Add(usedSkill);
            return usedSkill;
        }

        [Obsolete("Use UseSkill")]
        private BattleStatus.Skill UseSkillV2()
        {
            var selectedSkill = Skills.SelectV2(Simulator.Random);

            var usedSkill = selectedSkill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    ATK,
                    selectedSkill,
                    Simulator.SkillBuffSheet,
                    Simulator.StatBuffSheet,
                    Simulator.SkillActionBuffSheet,
                    Simulator.ActionBuffSheet
                )
            );

            Skills.SetCooldown(selectedSkill.SkillRow.Id, selectedSkill.SkillRow.Cooldown);
            Simulator.Log.Add(usedSkill);
            return usedSkill;
        }

        private void RemoveBuffs()
        {
            var isBuffRemoved = false;

            var keyList = Buffs.Keys.ToList();
            foreach (var key in keyList)
            {
                var buff = Buffs[key];
                if (buff.RemainedDuration > 0)
                    continue;

                Buffs.Remove(key);
                isBuffRemoved = true;
            }

            if (!isBuffRemoved)
                return;

            Stats.SetBuffs(StatBuffs);
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
            if (Buffs.TryGetValue(buff.BuffInfo.GroupId, out var outBuff) &&
                outBuff.BuffInfo.Id > buff.BuffInfo.Id)
                return;

            if (buff is StatBuff stat)
            {
                var clone = (StatBuff)stat.Clone();
                Buffs[stat.RowData.GroupId] = clone;
                Stats.AddBuff(clone, updateImmediate);
            }
            else if (buff is ActionBuff action)
            {
                var clone = (ActionBuff)action.Clone();
                Buffs[action.RowData.GroupId] = clone;
            }
        }

        public void RemoveRecentStatBuff()
        {
            StatBuff removedBuff = null;
            var minDuration = int.MaxValue;
            foreach (var buff in StatBuffs)
            {
                if (buff.RowData.StatModifier.Value < 0)
                {
                    continue;
                }

                var elapsedTurn = buff.OriginalDuration - buff.RemainedDuration;
                if (removedBuff is null)
                {
                    minDuration = elapsedTurn;
                    removedBuff = buff;
                }

                if (elapsedTurn > minDuration ||
                    buff.RowData.Id >= removedBuff.RowData.Id)
                {
                    continue;
                }

                minDuration = elapsedTurn;
                removedBuff = buff;
            }

            if (removedBuff != null)
            {
                Stats.RemoveBuff(removedBuff);
                Buffs.Remove(removedBuff.RowData.GroupId);
            }
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
                throw new KeyNotFoundException(GameConfig.DefaultAttackId.ToString(CultureInfo.InvariantCulture));
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
                OnPreSkill();
                var usedSkill = UseSkill();
                if (usedSkill != null)
                {
                    OnPostSkill(usedSkill);
                }
                RemoveBuffs();
            }
            EndTurn();
        }

        [Obsolete("Use Act")]
        private void ActV1()
        {
            if (IsAlive())
            {
                ReduceDurationOfBuffs();
                ReduceSkillCooldownV1();
                OnPreSkill();
                var usedSkill = UseSkillV1();
                if (usedSkill != null)
                {
                    OnPostSkill(usedSkill);
                }
                RemoveBuffs();
            }
            EndTurn();
        }

        [Obsolete("Use Act")]
        private void ActV2()
        {
            if (IsAlive())
            {
                ReduceDurationOfBuffs();
                ReduceSkillCooldownV1();
                OnPreSkill();
                var usedSkill = UseSkillV2();
                if (usedSkill != null)
                {
                    OnPostSkill(usedSkill);
                }
                RemoveBuffs();
            }
            EndTurn();
        }

        protected virtual void OnPreSkill()
        {

        }

        protected virtual void OnPostSkill(BattleStatus.Skill usedSkill)
        {
            var bleeds = Buffs.Values.OfType<Bleed>().OrderBy(x => x.BuffInfo.Id);
            foreach (var bleed in bleeds)
            {
                var effect = bleed.GiveEffect(this, Simulator.WaveTurn);
                Simulator.Log.Add(effect);
            }

            if (IsDead)
            {
                Die();
            }

            FinishTargetIfKilledForBeforeV100310(usedSkill);
            FinishTargetIfKilled(usedSkill);
        }

        private void FinishTargetIfKilledForBeforeV100310(BattleStatus.Skill usedSkill)
        {
            var isFirst = true;
            foreach (var info in usedSkill.SkillInfos)
            {
                if (!info.Target.IsDead)
                {
                    continue;
                }

                if (isFirst)
                {
                    isFirst = false;
                    continue;
                }

                var target = Targets.FirstOrDefault(i =>
                    i.Id == info.Target.Id);
                switch (target)
                {
                    case Player player:
                    {
                        var quest = new KeyValuePair<int, int>((int)QuestEventType.Die, 1);
                        player.eventMapForBeforeV100310.Add(quest);

                        break;
                    }
                    case Enemy enemy:
                    {
                        if (enemy.Targets[0] is Player targetPlayer)
                        {
                            var quest = new KeyValuePair<int, int>(enemy.CharacterId, 1);
                            targetPlayer.monsterMapForBeforeV100310.Add(quest);
                        }

                        break;
                    }
                }
            }
        }

        private void FinishTargetIfKilled(BattleStatus.Skill usedSkill)
        {
            var killedTargets = new List<CharacterBase>();
            foreach (var info in usedSkill.SkillInfos)
            {
                if (!info.Target.IsDead)
                {
                    continue;
                }

                var target = Targets.FirstOrDefault(i => i.Id == info.Target.Id);
                if (!killedTargets.Contains(target))
                {
                    killedTargets.Add(target);
                }
            }

            foreach (var target in killedTargets)
            {
                target?.Die();
            }
        }
    }
}
