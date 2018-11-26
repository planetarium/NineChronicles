using BTAI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        public int RewardExp = 0;

        public void InitAI()
        {
            _walkSpeed = -1.0f;
            Root = new Root();
            Root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(IsAlive).OpenBranch(
                        BT.Selector().OpenBranch(
                            BT.If(HasTargetInRange).OpenBranch(
                                BT.Call(Attack)
                            ),
                            BT.Call(Walk)
                        )
                    ),
                    BT.Sequence().OpenBranch(
                        BT.Call(Die),
                        BT.Terminate()
                    )
                )
            );

            _skills.Clear();
            // TODO: select skill
            var attack = this.GetOrAddComponent<Skill.MonsterAttack>();
            if (attack.Init("monster_attack"))
            {
                _skills.Add(attack);
            }
        }

        public void InitStats(Data.Table.Monster statsData, int power)
        {
            HP = statsData.Health;
            ATK = statsData.Attack;
            DEF = statsData.Defense;
            RewardExp = statsData.RewardExp;

            Power = power;
        }

        override protected void OnDead()
        {
            base.OnDead();
            Event.OnEnemyDead.Invoke();
        }
    }
}
