using System.Collections;
using DG.Tweening;
using Nekoyume.Data.Table;
using UnityEngine;


namespace Nekoyume.Game
{
    public class Character : MonoBehaviour
    {
        public string id;
        public int group;
        public Stats stats;

        public IEnumerator Walk()
        {
            while (true)
            {
                Vector2 position = transform.position;
                position.x += Time.deltaTime * 40 / 160;
                transform.position = position;
                yield return null;
            }
        }

        public IEnumerator Load(GameObject go, string class_)
        {
            _Load(go, class_);
            yield return null;
        }

        public IEnumerator Stop()
        {
            StopCoroutine(Walk());
            yield return null;
        }

        public void _Load(GameObject go, string class_)
        {
            Vector2 position = go.transform.position;
            position.y = -1;
            go.transform.position = position;
            var render = go.AddComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>(string.Format("images/character_{0}", class_));
            if (sprite == null)
                sprite = Resources.Load<Sprite>("images/pet");
            render.sprite = sprite;
            render.sortingOrder = 1;
            Material mat = render.material;
            Sequence colorseq = DOTween.Sequence();
            colorseq.Append(mat.DOColor(Color.white, 0.0f));
        }
    }
}
