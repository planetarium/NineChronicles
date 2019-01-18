using System.Collections.Generic;
using System.Linq;
using BTAI;
using Nekoyume.Action;
using Nekoyume.Model.BattleLog;

namespace Nekoyume.Model
{
    public abstract class CharacterBase
    {
        public bool isDead => hp <= 0;
        public int hpMax;
        public int hp;
        public int atk;
        public readonly List<CharacterBase> targets = new List<CharacterBase>();
        public Simulator simulator;

        private Root root;

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
                target.hp -= atk;
                var log = new Attack(this, target, atk);
                simulator.logs.Add(log);
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
            var dead = new Dead(this);
            simulator.logs.Add(dead);
        }
    }
}
