using BTAI;
using Nekoyume.Data.Table;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : Base
    {
        public int RewardExp = 0;

        public void InitAI(Monster data)
        {
            HP = data.Health;
            ATK = data.Attack;
            DEF = data.Defense;
            RewardExp = data.RewardExp;
            _walkSpeed = -0.6f;
            Root = new Root();
            Root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(CanWalk).OpenBranch(
                        BT.Call(Walk)
                    ),
                    BT.If(HasTarget).OpenBranch(
                        BT.If(IsAlive).OpenBranch(
                            BT.Wait(0.5f),
                            BT.Call(EnemyAttack)
                        )
                    ),
                    BT.Terminate()
                )
            );

            _skills.Clear();
            // TODO: select skill
            var attack = this.GetOrAddComponent<Skill.MonsterAttack>();
            if (attack.Init("attack"))
            {
                _skills.Add(attack);
            }
        }

        private void EnemyAttack()
        {
            foreach (var skill in _skills)
            {
                skill.Use();
            }
        }
    }
}
