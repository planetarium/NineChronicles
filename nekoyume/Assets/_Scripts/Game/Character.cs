using System.Collections;
using DG.Tweening;
using UnityEngine;


namespace Nekoyume.Game
{
    public class Character : MonoBehaviour
    {
        public string id;
        public int group;

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

        public IEnumerator Load(string class_)
        {
            var render = transform.gameObject.AddComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>(string.Format("images/character_{0}", class_));
            render.sprite = sprite;
            render.sortingOrder = 1;
            Material mat = render.material;
            Sequence colorseq = DOTween.Sequence();
            colorseq.Append(mat.DOColor(Color.white, 0.0f));
            yield return null;
        }

        public IEnumerator Stop()
        {
            StopCoroutine(Walk());
            yield return null;
        }
    }

}
