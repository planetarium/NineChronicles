using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.VFX;
using Nekoyume.Manager;
using Nekoyume.UI;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    // todo: Exp. Mode to CharacterBase.
    public class Player : CharacterBase
    {
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public long EXP = 0;
        public long EXPMax { get; private set; }
        
        public Item.Inventory Inventory;
        public TouchHandler touchHandler;

        public new readonly ReactiveProperty<Model.Player> Model = new ReactiveProperty<Model.Player>();

        public List<Equipment> Equipments =>
            Inventory.Items.Select(i => i.item).OfType<Equipment>().Where(e => e.equipped).ToList();

        protected override float RunSpeedDefault => Model.Value.RunSpeed;

        protected Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            OnUpdateHPBar.Subscribe(_ => Event.OnUpdatePlayerStatus.OnNext(this)).AddTo(gameObject);

            Animator = new PlayerAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale;

            Inventory = new Item.Inventory();

            touchHandler.onPointerClick.Subscribe(_ =>
                {
                    if (Game.instance.stage.IsInStage)
                        return;
                    
                    Animator.Touch();
                })
                .AddTo(gameObject);

            TargetTag = Tag.Enemy;
        }

        private void OnDestroy()
        {
            Animator.Dispose();
        }

        #endregion

        public override void Set(Model.CharacterBase model, bool updateCurrentHP = false)
        {
            if (!(model is Model.Player playerModel))
                throw new ArgumentException(nameof(model));

            Set(playerModel, updateCurrentHP);
        }

        public void Set(Model.Player model, bool updateCurrentHP)
        {
            base.Set(model, updateCurrentHP);

            _disposablesForModel.DisposeAllAndClear();
            Model.SetValueAndForceNotify(model);

            InitStats(model);
            StartCoroutine(CoUpdateSet(Model.Value.armor));

            if (ReferenceEquals(SpeechBubble, null))
            {
                SpeechBubble = Widget.Create<SpeechBubble>();
            }

            SpeechBubble.speechBreakTime = GameConfig.PlayerSpeechBreakTime;
        }

        protected override IEnumerator CoProcessDamage(Model.Skill.SkillInfo info, bool isConsiderDie, bool isConsiderElementalType)
        {
            yield return StartCoroutine(base.CoProcessDamage(info, isConsiderDie, isConsiderElementalType));
            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = DamageTextForce;
            PopUpDmg(position, force, info, isConsiderElementalType);
        }
        
        protected override IEnumerator Dying()
        {
            SpeechBubble?.Clear();
            ShowSpeech("PLAYER_LOSE");
            StopRun();
            Animator.Die();
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

        public void UpdateSet(Armor armor, Weapon weapon = null)
        {
            StartCoroutine(CoUpdateSet(armor, weapon));
        }

        private IEnumerator CoUpdateSet(Armor armor, Weapon weapon = null)
        {
            if (weapon == null)
                weapon = Model.Value.weapon;

            var itemId = armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId;
            if (!ReferenceEquals(Animator.Target, null))
            {
                if (Animator.Target.name.Contains(itemId.ToString()))
                {
                    UpdateWeapon(weapon);
                    yield break;
                }
                Animator.DestroyTarget();
                // 오브젝트가 파괴될때까지 기다립니다.
                // https://docs.unity3d.com/ScriptReference/Object.Destroy.html
                yield return new WaitForEndOfFrame();
            }
            var origin = Resources.Load<GameObject>($"Character/Player/{itemId}");
            var go = Instantiate(origin, gameObject.transform);
            Animator.ResetTarget(go);
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

            var level = Level;
            Model.Value.GetExp(exp);
            EXP += exp;

            if (Level != level)
            {
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionStatusLevelUp, level);
                AudioController.instance.PlaySfx(AudioController.SfxCode.LevelUp);
                VFXController.instance.Create<BattleLevelUp01VFX>(transform, HUDOffset);
                HPBar.SetLevel(Level);
                InitStats(Model.Value);
            }

            Event.OnUpdatePlayerStatus.OnNext(this);
        }

        private void InitStats(Model.Player character)
        {
            EXP = character.Exp.Current;
            EXPMax = character.Exp.Max;
            Inventory = character.Inventory;
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
                .Where(c => c.gameObject.CompareTag(TargetTag))
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (enemy != null)
            {
                return canRun && !TargetInRange(enemy);
            }

            return canRun;
        }

        protected override void ProcessAttack(CharacterBase target, Model.Skill.SkillInfo skill, bool isLastHit, bool isConsiderElementalType)
        {
            ShowSpeech("PLAYER_SKILL", (int) skill.ElementalType, (int)skill.SkillCategory);
            base.ProcessAttack(target, skill, isLastHit, isConsiderElementalType);
            ShowSpeech("PLAYER_ATTACK");
        }

        protected override IEnumerator CoAnimationCast(Model.Skill.SkillInfo info)
        {
            ShowSpeech("PLAYER_SKILL", (int) info.ElementalType, (int)info.SkillCategory);
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
