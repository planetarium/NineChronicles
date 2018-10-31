using System.Collections;
using UnityEngine;

using DG.Tweening;


namespace Nekoyume.Game.Status
{
    public class Spawn : Base
    {
        public override IEnumerator Execute(Stage stage,  Network.Response.BattleStatus status)
        {
            GameObject go = new GameObject(status.name);
            go.transform.parent = stage.transform;

            Character character = go.AddComponent<Character>();
            character.id = status.id_;
            character.group = status.character_type;

            var renderer = go.AddComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>(string.Format("images/character_{0}", status.class_));
            if (sprite == null)
                sprite = Resources.Load<Sprite>("images/pet");
            renderer.sprite = sprite;
            Material mat = renderer.material;
            Sequence colorseq = DOTween.Sequence();
            colorseq.Append(mat.DOColor(Color.white, 0.0f));

            yield return null;
        }
    }
}
