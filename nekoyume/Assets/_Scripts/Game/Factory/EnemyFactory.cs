using DG.Tweening;
using UnityEngine;


namespace Nekoyume.Game.Factory
{
    public class EnemyFactory : MonoBehaviour
    {
        public GameObject Create(string monsterId)
        {
            Data.Tables tables = this.GetRootComponent<Data.Tables>();
            Data.Table.Monster monsterData;
            if (!tables.Monster.TryGetValue(monsterId, out monsterData))
                return null;

            var objectPool = GetComponent<ObjectPool>();
            var enemy = objectPool.Get<Character.Enemy>();
            if (enemy == null)
                return null;

            enemy.InitAI();

            // sprite
            var render = enemy.GetComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>($"images/character_{monsterId}");
            if (sprite == null)
                sprite = Resources.Load<Sprite>("images/pet");
            render.sprite = sprite;
            Material mat = render.material;
            Sequence colorseq = DOTween.Sequence();
            colorseq.Append(mat.DOColor(Color.white, 0.0f));

            return enemy.gameObject;
        }
    }
}
