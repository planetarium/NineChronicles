using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using BTAI;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Skill;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class CharacterBase
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
        public float luck;

        public const float CriticalMultiplier = 1.5f;
        public int level;
        public abstract float TurnSpeed { get; set; }

        public Elemental ATKElement;
        public Elemental DEFElement;
        protected List<SkillBase> Skills;

        private SkillBase _selectedSkill;

        [NonSerialized] private Root _root;
        [NonSerialized] public Simulator Simulator;
        public bool IsDead => currentHP <= 0;
        public Guid id = Guid.NewGuid();
        public float attackRange = 0.5f;

        public void InitAI()
        {
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
            var target = _selectedSkill.GetTarget();
            var attack = _selectedSkill.Use();
            Simulator.Log.Add(attack);
            _selectedSkill = null;
        }

        public bool IsCritical()
        {
            var chance = Simulator.Random.NextDouble();
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
                character = Copy(this),
            };
            Simulator.Log.Add(dead);
        }

        public static T Copy<T>(T origin)
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, origin);
                stream.Seek(0, SeekOrigin.Begin);
                return (T) formatter.Deserialize(stream);
            }

        }

        public void OnDamage(int dmg)
        {
            currentHP -= dmg;
            if (IsDead)
            {
                Die();
            }
        }

        protected abstract void SetSkill();

        private void SelectSkill()
        {
            SetSkill();
            _selectedSkill = Skills.First();
        }
    }

    public class InformationFieldAttribute : Attribute
    {
    }
}
