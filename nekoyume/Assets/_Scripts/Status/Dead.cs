using System.Collections;
using UnityEngine;


public class Dead : Status
{
    public override IEnumerator Execute(Battle battle, BattleStatus status)
    {
        GameObject myObj = battle.characters[status.id_];

        SinScale ani = myObj.GetComponent<SinScale>();
        ani.enabled = false;

        myObj.transform.localScale = new Vector3(1.0f, -0.5f, 0.0f);

        Character character = myObj.GetComponent<Character>();
        character.hpBar.gameObject.SetActive(false);
        character.castingBar.gameObject.SetActive(false);

        yield break;
    }
}
