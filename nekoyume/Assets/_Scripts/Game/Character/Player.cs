using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.Data.Table;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.VFX;
using Nekoyume.Manager;
using Nekoyume.UI;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Player : CharacterBase
    {
        protected override Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);
        
        public int MP = 0;
        public long EXP = 0;
        public int Level = 0;
        public int MPMax = 0;
        public float RunSpeedMax = 3.0f;
        
        public Item.Inventory Inventory;
        public TouchHandler touchHandler;
        
        public override float Speed => RunSpeedMax;
        
        public List<Equipment> equipments =>
            Inventory.Items.Select(i => i.item).OfType<Equipment>().Where(e => e.equipped).ToList();

        public long EXPMax { get; private set; }

        protected override WeightType WeightType => WeightType.Small;

        protected override Vector3 HUDOffset => animator.GetHUDPosition();

        public override Guid Id => model.id;
        public Model.Player Model => (Model.Player) model;

        public bool InBattle { get; set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            animator = new PlayerAnimator(this);
            animator.OnEvent.Subscribe(OnAnimatorEvent);
            animator.TimeScale = AnimatorTimeScale;

            Inventory = new Item.Inventory();

            touchHandler.onPointerClick.Subscribe(_ =>
                {
                    if (InBattle)
                        return;
                    
                    animator.Touch();
                })
                .AddTo(gameObject);

            InBattle = false;

            targetTag = Tag.Enemy;
            Event.OnUpdateStatus.AddListener(UpdateHpBar);
        }

        private void OnDestroy()
        {
            animator.Dispose();
        }

        #endregion

        public override IEnumerator CoProcessDamage(Model.Skill.SkillInfo info, bool isConsiderDie, bool isConsiderElementalType)
        {
            yield return StartCoroutine(base.CoProcessDamage(info, isConsiderDie, isConsiderElementalType));

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = DamageTextForce;
            PopUpDmg(position, force, info, isConsiderElementalType);

            Event.OnUpdateStatus.Invoke();
        }

        protected override IEnumerator Dying()
        {
            _speechBubble?.Clear();
            ShowSpeech("PLAYER_LOSE");
            StopRun();
            animator.Die();
            yield return new WaitForSeconds(.5f);
            DisableHUD();
            yield return new WaitForSeconds(.8f);
            OnDead();
        }

        protected override void OnDead()
        {
            gameObject.SetActive(false);
            Event.OnPlayerDead.Invoke();
        }

        public void Init(Model.Player character)
        {
            model = character;
            StartCoroutine(CoUpdateSet(Model.armor));
            InitStats(character);

            if (ReferenceEquals(_speechBubble, null))
            {
                _speechBubble = Widget.Create<SpeechBubble>();
            }

            _speechBubble.speechBreakTime = GameConfig.PlayerSpeechBreakTime;
        }

        public void UpdateSet(Armor armor, Weapon weapon = null)
        {
            StartCoroutine(CoUpdateSet(armor, weapon));
        }

        private IEnumerator CoUpdateSet(Armor armor, Weapon weapon = null)
        {
            if (weapon == null)
                weapon = Model.weapon;

            var itemId = armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId;
            if (!ReferenceEquals(animator.Target, null))
            {
                if (animator.Target.name.Contains(itemId.ToString()))
                {
                    UpdateWeapon(weapon);
                    yield break;
                }
                animator.DestroyTarget();
                // 오브젝트가 파괴될때까지 기다립니다.
                // https://docs.unity3d.com/ScriptReference/Object.Destroy.html
                yield return new WaitForEndOfFrame();
            }
            var origin = Resources.Load<GameObject>($"Character/Player/{itemId}");
            var go = Instantiate(origin, gameObject.transform);
            animator.ResetTarget(go);
            UpdateWeapon(weapon);
        }

        public void UpdateWeapon(Weapon weapon)
        {
            var controller = GetComponentInChildren<SkeletonAnimationController>();
            if (!controller)
            {
                return;
            }
            
            var sprite = Weapon.GetSprite(weapon);
            controller.UpdateWeapon(sprite);
        }

        public IEnumerator CoGetExp(long exp)
        {
            if (exp <= 0)
            {
                yield break;
            }

            var level = model.level;
            Model.GetExp(exp);
            EXP += exp;

            if (model.level != level)
            {
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionStatusLevelUp, level);
                AudioController.instance.PlaySfx(AudioController.SfxCode.LevelUp);
                VFXController.instance.Create<BattleLevelUp01VFX>(transform, HUDOffset);
                _hpBar.UpdateLevel(model.level);
                InitStats(Model);
            }

            Event.OnUpdateStatus.Invoke();
        }

        private void InitStats(Model.Player character)
        {
            HP = character.currentHP;
            HPMax = character.hp;
            ATK = character.atk;
            DEF = character.def;
            EXP = character.exp;
            Level = character.level;
            EXPMax = character.expMax;
            Inventory = character.inventory;
            Range = character.attackRange;
            RunSpeedMax = character.runSpeed;
            sizeType = character.sizeType;
        }

        private void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "attackStart":
                    AudioController.PlaySwing();
                    break;
                case "attackPoint":
                    Event.OnAttackEnd.Invoke(this);
                    break;
                case "footstep":
                    AudioController.PlayFootStep();
                    break;
            }
        }

        protected override bool CanRun()
        {
            var canRun = base.CanRun();
            var enemy = GetComponentsInChildren<CharacterBase>()
                .Where(c => c.gameObject.CompareTag(targetTag))
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (enemy != null)
            {
                return canRun && !TargetInRange(enemy);
            }

            return canRun;
        }

        protected override void ProcessAttack(CharacterBase target, Model.Skill.SkillInfo skill, bool isLastHit, bool isConsiderElementalType)
        {
            ShowSpeech("PLAYER_SKILL", (int)(skill.Elemental ?? 0), (int)skill.skillCategory);
            base.ProcessAttack(target, skill, isLastHit, isConsiderElementalType);
            ShowSpeech("PLAYER_ATTACK");
            Widget.Find<Status>().UpdateBuff(model.buffs);
        }

        protected override IEnumerator CoAnimationCast(Model.Skill.SkillInfo info)
        {
            ShowSpeech("PLAYER_SKILL", (int)(info.Elemental ?? 0), (int)info.skillCategory);
            yield return StartCoroutine(base.CoAnimationCast(info));
        }

        public void DoFade(float endValue, float sec)
        {
            var controller = GetComponentInChildren<SkeletonAnimationController>();
            DOTween.Sequence()
                .Append(DOTween.To(
                    () => controller.SkeletonAnimation.skeleton.A,
                    co => controller.SkeletonAnimation.skeleton.A = co, 0, 0f
                ))
                .Append(DOTween.To(
                    () => controller.SkeletonAnimation.skeleton.A,
                    co => controller.SkeletonAnimation.skeleton.A = co, endValue, sec
                ))
                .Play();
        }
    }
}
