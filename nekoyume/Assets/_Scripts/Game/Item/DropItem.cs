using DG.Tweening;
using System.Collections;
using UnityEngine;


namespace Nekoyume.Game.Item
{
    public class DropItem : MonoBehaviour
    {
        public ItemBase Item { get; private set; }

        public void Set(ItemBase item)
        {
            Item = item;

            StartCoroutine(Pick());
        }

        private IEnumerator Pick()
        {
            yield return new WaitForSeconds(1.0f);

            var renderer = GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 2000;
            
            var pos = Camera.main.transform.TransformPoint(-2.99f, -1.84f, 0.0f);
            transform.DOJump(pos, 1.0f, 1, 1.8f).onComplete = () => {
                gameObject.SetActive(false);
            };
        }
    }
}
