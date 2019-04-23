using System;
using System.Collections;
using System.Linq;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Vfx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        private static readonly Vector3 DamageTextForce = new Vector3(0.1f, 0.5f);
        private static readonly Vector3 HpBarOffset = new Vector3(0f, 0.22f);
        
        public int DataId = 0;
        public Guid id;

        private Player _player;

        protected override Vector3 _hpBarOffset => _castingBarOffset + HpBarOffset;

        protected override Vector3 _castingBarOffset
        {
            get
            {
                var spriteRenderer = GetComponentsInChildren<Renderer>()
                    .OrderByDescending(r => r.transform.position.y)
                    .First();
                var y = spriteRenderer.bounds.max.y - transform.position.y;
                var body = GetComponentsInChildren<Transform>().First(g => g.name == "body");
                var bodyRenderer = body.GetComponent<Renderer>();
                var x = bodyRenderer.bounds.min.x - transform.position.x + bodyRenderer.bounds.size.x / 2;
                return new Vector3(x, y, 0.0f);

            }
        }

        public override float Speed => -1.8f;

        public override IEnumerator CoProcessDamage(int dmg, bool critical)
        {
            yield return StartCoroutine(base.CoProcessDamage(dmg, critical));

            var position = transform.TransformPoint(0f, 1f, 0f);
            var force = DamageTextForce;
            var txt = dmg.ToString();
            PopUpDmg(position, force, txt, critical);

            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                Sequence colorseq = DOTween.Sequence();
                colorseq.Append(mat.DOColor(Color.red, 0.1f));
                colorseq.Append(mat.DOColor(Color.white, 0.1f));
            }
        }

        protected override void OnDead()
        {
            Event.OnEnemyDead.Invoke(this);
            base.OnDead();
        }
        
        protected override void PopUpDmg(Vector3 position, Vector3 force, string dmg, bool critical)
        {
            base.PopUpDmg(position, force, dmg, critical);

            var pos = transform.position;
            pos.x -= 0.2f;
            pos.y += 0.32f;
            
            if (critical)
            {
                VfxController.instance.Create<VfxBattleAttackCritical01>(pos).Play();
            }
            else
            {
                VfxController.instance.Create<VfxBattleAttack01>(pos).Play();    
            }
        }

        public void Init(Model.Monster spawnCharacter, Player player)
        {
            _player = player;
            _hpBarOffset.Set(-0.0f, -0.11f, 0.0f);
            _castingBarOffset.Set(-0.0f, -0.33f, 0.0f);
            _anim = GetComponentInChildren<Animator>();
            SetAnimatorSpeed(AnimatorSpeed);
            InitStats(spawnCharacter);
            id = spawnCharacter.id;
            StartRun();
        }

        private void InitStats(Model.Monster character)
        {
            var stats = character.data.GetStats(character.level);
            HP = stats.HP;
            ATK = stats.Damage;
            DEF = stats.Defense;
            Power = 0;
            HPMax = HP;
        }

        protected override void Awake()
        {
            base.Awake();
            
            _targetTag = Tag.Player;
        }

        public void PlayAttackSfx()
        {
            // Fix me.
            // 이후 몬스터 별 공격 음이 재생된다.
        }

        protected override bool CanRun()
        {
            return base.CanRun() && !TargetInRange(_player);
        }

    }
}
