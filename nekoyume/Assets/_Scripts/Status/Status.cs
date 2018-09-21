using System.Collections;


public class Status
{
    public virtual IEnumerator Execute(Battle battle,  BattleStatus status)
    {
        // override
        yield return null;
    }
}
