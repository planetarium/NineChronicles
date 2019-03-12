using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using BTAI;
using Nekoyume.Action;

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
        private const float CriticalChance = 0.5f;
        private const float CriticalMultiplier = 1.5f;

        [NonSerialized] private Root _root;
        [NonSerialized] public Simulator Simulator;
        private bool isDead => hp <= 0;
        public Guid id = Guid.NewGuid();

        private void InitAI()
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
                    characterId = id,
                    targetId = target.id,
                    critical = critical,
                };
                Simulator.Log.Add(attack);
                target.OnDamage(dmg);
            }
        }

        private bool IsCritical()
        {
            var chance = Simulator.Random.NextDouble();
            return chance < CriticalChance;
        }

        private int CalcDmg(CharacterBase target, bool critical)
        {
            int dmg = atk;
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
                characterId = id,
            };
            Simulator.Log.Add(dead);
        }

        public void Spawn()
        {
            InitAI();
            var spawn = new Spawn
            {
                character = Copy(this),
                characterId = id,
            };
            Simulator.Log.Add(spawn);
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
