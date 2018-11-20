using System;
using System.Collections;
using System.Linq;
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

        public bool IsDead => Stats.Health <= 0;
        private Root _root = new Root();

        public IEnumerator Load(Avatar avatar, Stage stage)
        {
            _Load(avatar, stage);
            yield return null;
        }

        private void Walk()
        {
            Vector2 position = transform.position;
            position.x += Time.deltaTime * 40 / 160;
            transform.position = position;
        }

        private bool ClearStage()
        {
            return transform.position.x > 3;
        }

        private void Update()
        {
            if (_root.Children().Any())
            {
                _root.Tick();
            }
        }

        public void _Load(Avatar avatar, Stage stage)
        {
            Vector2 position = gameObject.transform.position;
            position.x = 0;
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
            _root.OpenBranch(
                BT.If(() => stage.Id > 0).OpenBranch(
                    BT.If(ClearStage).OpenBranch(
                        BT.Call(stage.OnStageEnter)
                    ),
                    BT.Call(Walk)
                )
            );
        }
    }
}