using Nekoyume.Action;
using UnityEngine;


namespace Nekoyume.Game.Factory
{
    public class PlayerFactory : MonoBehaviour
    {
        public GameObject Create()
        {
            var log = ActionManager.Instance.battleLog;
            var objectPool = GetComponent<Util.ObjectPool>();
            var player = objectPool.Get<Character.Player>();
            if (player == null)
                return null;
            player.Init(log);

            // sprite
            // var render = player.GetComponent<SpriteRenderer>();
            // var sprite = Resources.Load<Sprite>($"images/character_{avatar.class_}");
            // if (sprite == null)
            //     sprite = Resources.Load<Sprite>("images/pet");
            // render.sprite = sprite;
            // Material mat = render.material;
            // Sequence colorseq = DOTween.Sequence();
            // colorseq.Append(mat.DOColor(Color.white, 0.0f));

            return player.gameObject;
        }
    }
}
