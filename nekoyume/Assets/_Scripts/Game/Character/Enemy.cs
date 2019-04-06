using System;
using System.Collections;
using BTAI;
using DG.Tweening;
using Nekoyume.Data;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        public int DataId = 0;
        public Guid id;

        protected override Vector3 _hpBarOffset => _castingBarOffset + new Vector3(0, 0 + 0.22f, 0.0f);

        protected override Vector3 _castingBarOffset
        {
            get
            {
                var spriteRenderer = GetComponentInChildren<Renderer>();
                var x = spriteRenderer.bounds.min.x - transform.position.x + spriteRenderer.bounds.size.x / 2;
                var y = spriteRenderer.bounds.max.y - transform.position.y;
                return new Vector3(x, y, 0.0f);
            }
        }

        public override float Speed => 0.0f;


        public void InitStats(Data.Table.Character statsData, int power)
        {
            HP = Mathf.FloorToInt((float)statsData.hp * ((float)power * 0.01f));
            ATK = statsData.damage;
            DEF = Mathf.FloorToInt((float)statsData.defense * ((float)power * 0.01f));

            Power = power;

            HPMax = HP;
        }

        public override IEnumerator CoProcessDamage(int dmg, bool critical)
        {
            yield return StartCoroutine(base.CoProcessDamage(dmg, critical));

            var position = transform.TransformPoint(0.1f, 0.8f, 0.0f);
            var force = new Vector3(0.02f, 0.4f);
            var txt = dmg.ToString();
            PopUpDmg(position, force, txt, critical);

            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                UnityEngine.Material mat = renderer.material;
                DG.Tweening.Sequence colorseq = DOTween.Sequence();
                colorseq.Append(mat.DOColor(Color.red, 0.1f));
                colorseq.Append(mat.DOColor(Color.white, 0.1f));
            }

            if (HP <= 0)
            {
                Die();
            }
        }

        protected override void OnDead()
        {
            Event.OnEnemyDead.Invoke(this);
            base.OnDead();
        }

        public void Init(Model.Monster spawnCharacter)
        {
            _hpBarOffset.Set(-0.0f, -0.11f, 0.0f);
            _castingBarOffset.Set(-0.0f, -0.33f, 0.0f);
            InitStats(spawnCharacter.data);
            id = spawnCharacter.id;
            StartRun();
        }

        private void InitStats(Data.Table.Character data)
        {
            HP = data.hp;
            ATK = data.damage;
            DEF = data.defense;
            Power = 0;
            HPMax = HP;
        }

        private void Awake()
        {
            _targetTag = Tag.Player;
        }

    }
}
