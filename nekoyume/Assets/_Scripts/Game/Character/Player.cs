using System.Linq;
using Boo.Lang;
using BTAI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Player : Base
    {
        public List<GameObject> Targets = new List<GameObject>();

        public void InitAI(Stage stage)
        {
            Walkable = Instage = stage.Id > 0;
            _walkSpeed = 0.6f;
            Root = new Root();
            Root.OpenBranch(
                BT.Condition(IsDead),
                BT.If(() => Instage).OpenBranch(
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
            enemy.HP -= 1;
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
