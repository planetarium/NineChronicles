using BTAI;
using Nekoyume.Data.Table;
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
                BT.If(IsAlive).OpenBranch(
                    BT.Selector().OpenBranch(
                        BT.If(HasTargetInRange).OpenBranch(
                            BT.Call(EnemyAttack)
                        ),
                        BT.Call(Walk)
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

        public void InitStats(Data.Table.Monster statsData)
        {
            HP = statsData.Health;
            ATK = statsData.Attack;
            DEF = statsData.Defense;
            RewardExp = statsData.RewardExp;
        }

        private void EnemyAttack()
        {
            foreach (var skill in _skills)
            {
                skill.Use();
            }
        }

        override protected void OnDead()
        {
            base.OnDead();
            Event.OnEnemyDead.Invoke();
        }
    }
}
