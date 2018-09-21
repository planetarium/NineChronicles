using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Casting : Status
{
    public override IEnumerator Execute(Battle battle, BattleStatus status)
    {
        GameObject myObj = battle.characters[status.id_];
        Character myCharacter = myObj.GetComponent<Character>();

        
        if (myCharacter.castingBar.gameObject.activeSelf == false)
        {
            myCharacter.castingBar.gameObject.SetActive(true);
            myCharacter.castingBar.UpdatePosition(myObj, new Vector3(0.0f, -0.2f, 0.0f));
        }
        
        var slider = myCharacter.castingBar.gameObject.GetComponent<Slider>();
        slider.value = (float)status.tick_remain / (float)10;

        yield return null;
    }
}
