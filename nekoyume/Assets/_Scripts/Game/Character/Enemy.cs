using BTAI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : Base
    {
        public override bool IsDead()
        {
            return HP <= 0;
        }

        public void InitAI()
        {
            _walkSpeed = -0.6f;
            Root = new BTAI.Root();
            Root.OpenBranch(
                BT.If(() => Walkable).OpenBranch(
                    BT.Call(Walk)
                )
            );
        }
    }
}
