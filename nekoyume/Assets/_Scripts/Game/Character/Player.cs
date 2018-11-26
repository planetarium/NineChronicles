using System.Linq;
using BTAI;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

namespace Nekoyume.Game.Character
{
    public class Player : Base
    {
        public int MP = 0;

        public void InitAI()
        {
            _walkSpeed = 1.0f;
            Root = new Root();
            Root.OpenBranch(
                BT.If(IsAlive).OpenBranch(
                    BT.Selector().OpenBranch(
                        BT.If(HasTargetInRange).OpenBranch(
                            BT.Call(PlayerAttack)
                        ),
                        BT.Call(Walk)
                    )
                )
            );

            _skills.Clear();
            // TODO: select skill
            var attack = this.GetOrAddComponent<Skill.Attack>();
            if (attack.Init("attack"))
            {
                _skills.Add(attack);
            }
        }

        public void InitStats(Data.Table.Stats statsData)
        {
            HP = statsData.Health;
            ATK = statsData.Attack;
            DEF = statsData.Defense;
            MP = statsData.Mana;
        }

        public void PlayerAttack()
        {
            foreach (var skill in _skills)
            {
                if (skill.Use())
                {
                    _anim.SetBool("Walk", false);
                    _anim.SetTrigger("Attack");
                }
            }
        }

        protected override void OnDead()
        {
            Event.OnPlayerDead.Invoke();
        }
    }
}
