using System.Collections;


namespace Nekoyume.Game.Status
{
    public class Base
    {
        public virtual IEnumerator Execute(Stage stage,  Model.BattleStatus status)
        {
            // override
            yield return null;
        }
    }
}
