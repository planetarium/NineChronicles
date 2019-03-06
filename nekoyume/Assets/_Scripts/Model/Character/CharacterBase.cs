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

        [NonSerialized] private Root root;
        [NonSerialized] public Simulator simulator;
        public bool isDead => hp <= 0;
        public Guid id = Guid.NewGuid();

        public void InitAI()
        {
            root = new Root();
            root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(isAlive).OpenBranch(
                        BT.Call(Attack)
                    ),
                    BT.Sequence().OpenBranch(
                        BT.Call(Die),
                        BT.Terminate()
                    )
                )
            );
        }

        public void Tick()
        {
            root.Tick();
        }

        private void Attack()
        {
            var target = targets.FirstOrDefault(t => !t.isDead);
            if (target != null)
            {
                var dmg = Math.Max(atk - target.def, 1);
                target.hp -= dmg;
                var attack = new Attack
                {
                    character = Copy(this),
                    target = Copy(target),
                    atk = dmg,
                    characterId = id,
                    targetId = target.id,
                };
                simulator.Log.Add(attack);
            }
        }

        private bool isAlive()
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
            simulator.Log.Add(dead);
        }

        public void Spawn()
        {
            InitAI();
            var spawn = new Spawn
            {
                character = Copy(this),
                characterId = id,
            };
            simulator.Log.Add(spawn);
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

    }
}
