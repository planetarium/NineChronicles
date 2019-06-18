using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private const int DefaultSetId = 101000;
        
        protected override Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);
        
        public int MP = 0;
        public long EXP = 0;
        public int Level = 0;
        public int MPMax = 0;
        public float RunSpeedMax = 3.0f;
        public override float Speed => RunSpeedMax;
        
        public List<Equipment> equipments =>
            Inventory.items.Select(i => i.Item).OfType<Equipment>().Where(e => e.equipped).ToList();

        public Model.Player model;
        public Item.Inventory Inventory;

        public long EXPMax { get; private set; }

        protected override WeightType WeightType => WeightType.Small;

        protected override Vector3 HUDOffset => animator.GetHUDPosition();

        public override Guid Id => model.id;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            animator = new PlayerAnimator(this);
            animator.OnEvent.Subscribe(OnAnimatorEvent);
            animator.TimeScale = AnimatorTimeScale;

            Inventory = new Item.Inventory();

            targetTag = Tag.Enemy;
            Event.OnUpdateStatus.AddListener(UpdateHpBar);
        }

        private void OnDestroy()
        {
            animator.Dispose();
        }

        #endregion

        public override IEnumerator CoProcessDamage(Model.Skill.SkillInfo info)
        {
            yield return StartCoroutine(base.CoProcessDamage(info));

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = DamageTextForce;
            animator.Hit();
            PopUpDmg(position, force, info);

            Event.OnUpdateStatus.Invoke();
        }

        protected override void OnDead()
        {
            gameObject.SetActive(false);
            Event.OnPlayerDead.Invoke();
        }

        public void Init(Model.Player character)
        {
            model = character;
            UpdateArmor(model.armor);
            InitStats(character);

            if (ReferenceEquals(_speechBubble, null))
                _speechBubble = Widget.Create<SpeechBubble>();
            if (!ReferenceEquals(_speechBubble, null))
            {
                Setting speechSetting;
                if (Data.Tables.instance.Settings.TryGetValue("player_speech_break_time", out speechSetting))
                {
                    _speechBubble.speechBreakTime = speechSetting.GetValueAsFloat();
                }
                else
                {
                    _speechBubble.speechBreakTime = 5.0f; // set default
                }
            }
        }

        public void UpdateArmor(Armor item)
        {
            var itemId = item?.Data.resourceId ?? DefaultSetId;
            if (!ReferenceEquals(animator.Target, null))
            {
                if (!animator.Target.name.Contains(itemId.ToString()))
                {
                    animator.DestroyTarget();
                }
                else
                {
                    return;
                }
            }

            var origin = Resources.Load<GameObject>($"Character/Player/{itemId}");
            var go = Instantiate(origin, gameObject.transform);
            animator.ResetTarget(go);
        }

        public IEnumerator CoGetExp(long exp)
        {
            if (exp <= 0)
            {
                yield break;
            }

            PopupText.Show(
                transform.TransformPoint(-0.6f, 1.0f, 0.0f),
                new Vector3(0.0f, 2.0f, 0.0f),
                $"+{exp}"
            );
            var level = model.level;
            model.GetExp(exp);
            EXP += exp;

            if (model.level != level)
            {
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionStatusLevelUp, level);
                AudioController.instance.PlaySfx(AudioController.SfxCode.LevelUp);
                VFXController.instance.Create<BattleLevelUp01VFX>(transform, HUDOffset);
                yield return new WaitForSeconds(0.3f);
                PopupText.Show(
                    transform.TransformPoint(-0.6f, 1.0f, 0.0f),
                    new Vector3(0.0f, 2.0f, 0.0f),
                    "LEVEL UP"
                );
                InitStats(model);
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
            characterSize = character.characterSize;
        }

        private void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "attackStart":
                    AudioController.PlaySwing();
                    ShowSpeech("PLAYER_ATTACK");
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
    }
}
