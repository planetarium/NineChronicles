using System.Linq;
using Boo.Lang;
using BTAI;
using Nekoyume.Data.Table;
using UnityEngine;

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
                BT.Condition(IsDead),
                BT.If(() => InStage).OpenBranch(
                    BT.If(HasTarget).OpenBranch(
                        BT.Wait(0.5f),
                        BT.Call(Slash)
                    ),
                    BT.If(() => !HasTarget()).OpenBranch(
                        BT.If(() => Walkable).OpenBranch(
                            BT.Call(Walk)
                        ),
                        BT.Call(() =>
                        {
                            Walkable = true;
                        })
                    )
                )
            );
        }

        public void Slash()
        {
            var i = Random.Range(0, Targets.Count);
            var target = Targets[i];
            var enemy = target.GetComponent<Enemy>();
            Debug.Log(enemy.HP);
            int dmg = ATK - enemy.DEF;
            Debug.Log(dmg);
            enemy.HP -= dmg;
            Debug.Log(enemy.HP);
            if (!enemy.IsDead()) return;
            Debug.Log("Kill!");
            Targets.Remove(target);
            target.SetActive(false);
        }

        private bool HasTarget()
        {
            return Targets.Any();
        }
    }
}
