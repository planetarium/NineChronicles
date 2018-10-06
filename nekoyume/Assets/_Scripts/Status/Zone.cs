using System.Collections;
using UnityEngine;


public class Zone : Status
{
    public override IEnumerator Execute(Battle battle, BattleStatus status)
    {
        battle.SetBackground(status.id_);
        yield return null;
    }
}
