using DG.Tweening;
using Nekoyume.Move;
using UnityEngine;


namespace Nekoyume.Game.Factory
{
    public class PlayerFactory : MonoBehaviour
    {
        public GameObject Create(bool initAI)
        {
            var avatar = MoveManager.Instance.Avatar;

            Data.Tables tables = this.GetRootComponent<Data.Tables>();
            Data.Table.Stats statsData;
            if (!tables.Stats.TryGetValue(avatar.level, out statsData))
                return null;

            var objectPool = GetComponent<Util.ObjectPool>();
            var player = objectPool.Get<Character.Player>();
            if (player == null)
                return null;

            if (initAI)
                player.InitAI();
            player.InitStats(statsData);
            if (!avatar.dead) player.HP = avatar.hp;

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
