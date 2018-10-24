using System.Collections;
using UnityEngine;

using DG.Tweening;


public class Spawn : Status
{
    public override IEnumerator Execute(Battle battle, BattleStatus status)
    {
        var group = battle.transform.Find(status.character_type.ToString());
        for (int i = 0; i < group.childCount; ++i)
        {
            var child = group.GetChild(i);
            var character = child.gameObject.GetComponent<Character>();
            if (character.IsDead())
            {
                battle.characters[status.id_] = child.gameObject;

                var renderer = child.gameObject.GetComponent<SpriteRenderer>();
                var sprite = Resources.Load<Sprite>(string.Format("images/character_{0}", status.class_));
                if (sprite == null)
                    sprite = Resources.Load<Sprite>("images/pet");
                renderer.sprite = sprite;
                Material mat = renderer.material;
                Sequence colorseq = DOTween.Sequence();
                colorseq.Append(mat.DOColor(Color.white, 0.0f));

                character.hp = status.hp;
                character.hpMax = status.hp_max;
                character.Spawn();

                break;
            }
        }
        
        yield return null;
    }
}
