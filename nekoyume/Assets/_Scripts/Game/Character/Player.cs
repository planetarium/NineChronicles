using BTAI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Player : Base
    {
        public void InitAI(Stage stage)
        {
            Walkable = stage.Id > 0;
            _walkSpeed = 0.6f;
            Root = new BTAI.Root();
            Root.OpenBranch(
                BT.If(() => Walkable).OpenBranch(
                    BT.Call(Walk)
                )
            );
        }
    }
}
