using System.Collections;
using UnityEngine;


public class Zone : Status
{
    public override IEnumerator Execute(Battle battle, BattleStatus status)
    {
        var renderer = battle.background.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>(string.Format("images/{0}", status.id_));
        yield return null;
    }
}
