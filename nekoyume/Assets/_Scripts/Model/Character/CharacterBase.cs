using System;
using BTAI;

namespace Nekoyume.Model
{
    public abstract class CharacterBase
    {
        public bool isDead => hp <= 0;
        public int hpMax;
        public int hp;
        public int atk;
        public CharacterBase target;

        private Root root;

        public void InitAI()
        {
            root = new Root();
            root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(isAlive).OpenBranch(
                        BT.Call(Attack)
                    )
                ),
                BT.Sequence().OpenBranch(
                    BT.Terminate()
                )
            );
        }
        public void Tick()
        {
            root.Tick();
        }

        private void Attack()
        {
            target.hp -= atk;
        }

        private bool isAlive()
        {
            return !isDead;
        }
    }
}
