using System.Collections;
using BTAI;
using DG.Tweening;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;
using Sequence = DG.Tweening.Sequence;


namespace Nekoyume.Game
{
    public class Character : MonoBehaviour
    {
        public Stats Stats;
        public Root Root = BT.Root();

        public bool IsDead => Stats.Health <= 0;

        public IEnumerator Load(Avatar avatar)
        {
            _Load(avatar);
            yield return null;
        }

        private void Walk()
        {
            Vector2 position = transform.position;
            position.x += Time.deltaTime * 40 / 160;
            transform.position = position;
        }

        private void Update()
        {
            if (Root.Children().Count <= 0) return;
            Root.Tick();
        }

        public void _Load(Avatar avatar)
        {
            Vector2 position = gameObject.transform.position;
            position.y = -1;
            gameObject.transform.position = position;
            var render = gameObject.GetComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>($"images/character_{avatar.class_}");
            if (sprite == null)
                sprite = Resources.Load<Sprite>("images/pet");
            render.sprite = sprite;
            render.sortingOrder = 1;
            Material mat = render.material;
            Sequence colorseq = DOTween.Sequence();
            colorseq.Append(mat.DOColor(Color.white, 0.0f));
            var tables = this.GetRootComponent<Tables>();
            var statsTable = tables.Stats;
            Stats = statsTable[avatar.level];
            var stage = GameObject.Find("Stage").GetComponent<Stage>();
            if (stage.Id != 0)
            {
                Root.OpenBranch(
                    BT.Call(Walk)
                );
            }
        }
    }
}
