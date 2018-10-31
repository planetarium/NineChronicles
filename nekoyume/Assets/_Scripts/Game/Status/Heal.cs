using System.Collections;
using UnityEngine;


namespace Nekoyume.Game.Status
{
    public class Heal : Base
    {
        public override IEnumerator Execute(Stage stage,  Network.Response.BattleStatus status)
        {
            // TODO

            yield return new WaitForSeconds(1.0f);
        }
    }
}
