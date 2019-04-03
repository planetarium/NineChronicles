using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using BTAI;
using Nekoyume.Action;
using Nekoyume.Game;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class CharacterBase
    {
        public readonly List<CharacterBase> targets = new List<CharacterBase>();
        public int atk;
        public int def;
        public int hp;
        public int hpMax;
        public float criticalChance;
        private const float CriticalMultiplier = 1.5f;

        protected Elemental atkElement { get; set; } =
            Elemental.Create(Data.Table.Elemental.ElementalType.Normal);
        protected Elemental defElement { get; set; } =
            Elemental.Create(Data.Table.Elemental.ElementalType.Normal);

        [NonSerialized] private Root _root;
        [NonSerialized] public Simulator Simulator;
        private bool isDead => hp <= 0;
        public Guid id = Guid.NewGuid();

        internal void InitAI()
        {
            _root = new Root();
            _root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(IsAlive).OpenBranch(
                        BT.Call(Attack)
                    ),
                    BT.Terminate()
                )
            );
        }

        public void Tick()
        {
            _root.Tick();
        }

        private void Attack()
        {
            var target = targets.FirstOrDefault(t => !t.isDead);
            if (target != null)
            {
                var critical = IsCritical();
                var dmg = CalcDmg(target, critical);
                var attack = new Attack
                {
                    character = Copy(this),
                    target = Copy(target),
                    atk = dmg,
                    critical = critical,
                };
                Simulator.Log.Add(attack);
                target.OnDamage(dmg);
            }
        }

        private bool IsCritical()
        {
            var chance = Simulator.Random.NextDouble();
            return chance < criticalChance;
        }

        private int CalcDmg(CharacterBase target, bool critical)
        {
            int dmg = atkElement.CalculateDmg(atk, target.defElement);
            if (critical)
            {
                dmg = Convert.ToInt32(dmg * CriticalMultiplier);
            }
            return Math.Max(dmg - target.def, 1);
        }

        private bool IsAlive()
        {
            return !isDead;
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

        protected static T Copy<T>(T origin)
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

        private void OnDamage(int dmg)
        {
            hp -= dmg;
            if (isDead)
            {
                Die();
            }
        }
    }
}
