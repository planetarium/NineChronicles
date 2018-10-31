using System.Collections;


namespace Nekoyume.Game.Status
{
    public class Base
    {
        public virtual IEnumerator Execute(Stage stage,  Network.Response.BattleStatus status)
        {
            // override
            yield return null;
        }
    }
}
