using System.Linq;
using Boo.Lang;
using BTAI;
using Nekoyume.Data.Table;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.Game.Character
{
    public class Player : Base
    {
        public List<GameObject> Targets = new List<GameObject>();
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
                BT.While(IsAlive).OpenBranch(
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
            );
        }

        public void PlayerAttack()
        {
            var i = Random.Range(0, Targets.Count);
            var target = Targets[i];
            var enemy = target.GetComponent<Enemy>();
            Attack(enemy);
        }

        protected override bool HasTarget()
        {
            return Targets.Any();
        }

        private bool WaveEnd()
        {
            return InStage && Targets.Count == 0;
        }

        private void MoveNext()
        {
            Walkable = true;
        }

        public void OnTargetDead(GameObject target)
        {
            Debug.Log("Kill!");
            Targets.Remove(target);
        }
    }
}
