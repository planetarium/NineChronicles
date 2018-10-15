using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Delaying : Status
{
    public override IEnumerator Execute(Battle battle, BattleStatus status)
    {
        GameObject myObj = battle.characters[status.id_];
        Character myCharacter = myObj.GetComponent<Character>();
        
        yield return null;
    }
}
