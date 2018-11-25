using System.Linq;
using BTAI;
using Nekoyume.Data.Table;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.Game.Character
{
    public class Player : Base
    {
        public int MP = 0;

        public void InitAI(Stage stage, Stats data)
        {
            HP = data.Health;
            ATK = data.Attack;
            DEF = data.Defense;
            MP = data.Mana;
            Walkable = InStage = stage.Id > 0;
            _walkSpeed = 0.6f;
            Root = new Root();
            Root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(() => InStage).OpenBranch(
                        BT.If(IsAlive).OpenBranch(
                            BT.Selector().OpenBranch(
                                BT.If(CanWalk).OpenBranch(
                                    BT.Call(Walk)
                                ),
                                BT.If(HasTarget).OpenBranch(
                                    BT.Wait(0.5f),
                                    BT.Call(PlayerAttack)
                                ),
                                BT.If(WaveEnd).OpenBranch(
                                    BT.Call(MoveNext)
                                )
                            )
                        )
                    ),
                    BT.Terminate()
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

        public void PlayerAttack()
        {
            foreach (var skill in _skills)
            {
                skill.Use();
            }
        }

        private bool WaveEnd()
        {
            bool end = InStage && !HasTarget();
            if (end)
            {
                Targets.Clear();
            }
            return end;
        }

        private void MoveNext()
        {
            Walkable = true;
        }
    }
}
