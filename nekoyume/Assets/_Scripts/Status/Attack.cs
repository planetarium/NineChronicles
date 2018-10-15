using System.Collections;
using UnityEngine;


public class Attack : Status
{
    public override IEnumerator Execute(Battle battle, BattleStatus status)
    {
        GameObject myObj = battle.characters[status.id_];
        Character myCharacter = myObj.GetComponent<Character>();
        myCharacter.Attack();

        yield return new WaitForSeconds(0.2f);

        GameObject targetObj = battle.characters[status.target_id];
        Character targetCharacter = targetObj.GetComponent<Character>();
        targetCharacter.hp = status.target_hp;
        targetCharacter.Hit();
        #if !UNITY_EDITOR
        Battle.OnSkill(status.name);
        #endif

        if (status.target_remain > 0)
            yield break;

        yield return new WaitForSeconds(1.0f);
    }
}
