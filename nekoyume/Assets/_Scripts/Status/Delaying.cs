using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Delaying : Status
{
    public override IEnumerator Execute(Battle battle, BattleStatus status)
    {
        GameObject myObj = battle.characters[status.id_];
        Character myCharacter = myObj.GetComponent<Character>();
        
        var slider = myCharacter.castingBar.gameObject.GetComponent<Slider>();
        slider.value = (float)status.tick_remain / (float)10;

        yield return null;
    }
}
