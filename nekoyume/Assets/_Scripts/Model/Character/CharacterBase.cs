using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class CharacterBase : ICloneable
    {
        public readonly List<CharacterBase> targets = new List<CharacterBase>();
        [InformationField]
        public int atk;
        [InformationField]
        public int def;
        public int currentHP;
        [InformationField]
        public int hp;
        [InformationField]
        public decimal luck;

        public const decimal CriticalMultiplier = 1.5m;
        public int level;
        public abstract float TurnSpeed { get; set; }

        public Game.Elemental atkElement;
        public Game.Elemental defElement;
        public readonly Skills Skills = new Skills();

        private SkillBase _selectedSkill;

        [NonSerialized] private Root _root;
        [NonSerialized] public Simulator Simulator;
        public bool IsDead => currentHP <= 0;
        public Guid id = Guid.NewGuid();
        public float attackRange = 1.0f;
        public float runSpeed = 1.0f;
        public string characterSize = "s";

        public void InitAI()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(IsAlive).OpenBranch(
                        BT.Sequence().OpenBranch(
                            BT.Call(SelectSkill),
                            BT.Call(UseSkill)
                        )
                    ),
                    BT.Terminate()
                )
            );
        }

        public void Tick()
        {
            _root.Tick();
        }

        private void UseSkill()
        {
            var attack = _selectedSkill.Use(this);
            Simulator.Log.Add(attack);
            _selectedSkill = null;
        }

        public bool IsCritical()
        {
            var chance = Simulator.Random.Next(0, 100000) * 0.00001m;
            return chance < luck;
        }

        private bool IsAlive()
        {
            return !IsDead;
        }

        private void Die()
        {
            OnDead();
        }

        protected virtual void OnDead()
        {
            var dead = new Dead
            {
                character = (CharacterBase) Clone(),
            };
            Simulator.Log.Add(dead);
        }

        public void OnDamage(int dmg)
        {
            currentHP -= dmg;
            if (IsDead)
            {
                Die();
            }
        }

        protected virtual void SetSkill()
        {
            //기본공격 설정
            var attack = SkillFactory.Get(1.0m, new SkillEffect(), Elemental.ElementalType.Normal, atk);
            Skills.Add(attack);
        }

        private void SelectSkill()
        {
            _selectedSkill = Skills.Select(Simulator.Random);
        }

        public void Heal(int heal)
        {
            var current = currentHP;
            currentHP = Math.Min(heal + current, hp);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

    }

    public class InformationFieldAttribute : Attribute
    {
    }

    [Serializable]
    public class Skills : IEnumerable<SkillBase>
    {
        private readonly List<SkillBase> _skills = new List<SkillBase>();

        public void Add(SkillBase s)
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

        public IEnumerator<SkillBase> GetEnumerator()
        {
            return _skills.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public SkillBase Select(IRandom random)
        {
            var skills = _skills.OrderBy(s => s.chance).ToArray();
            foreach (var skill in skills)
            {
                var chance = random.Next(0, 100000) * 0.00001m;
                if (skill.chance > chance)
                {
                    return skill;
                }
            }
            return skills.Last();
        }
    }
}
