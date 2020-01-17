using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.Manager;
using Nekoyume.TableData;
using Nekoyume.UI;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    // todo: 경험치 정보를 `CharacterBase`로 옮기는 것이 좋겠음.
    public class Player : CharacterBase
    {
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public long EXP = 0;
        public long EXPMax { get; private set; }

        public Item.Inventory Inventory;
        public TouchHandler touchHandler;

        public List<Equipment> Equipments =>
            Inventory.Items.Select(i => i.item).OfType<Equipment>().Where(e => e.equipped).ToList();

        protected override float RunSpeedDefault => CharacterModel.RunSpeed;

        protected override Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);
        protected override Vector3 HudTextPosition => transform.TransformPoint(0f, 1.7f, 0f);

        public PlayerSpineController SpineController { get; private set; }
        public Model.Player Model => (Model.Player) CharacterModel;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            OnUpdateHPBar.Subscribe(_ => Event.OnUpdatePlayerStatus.OnNext(this)).AddTo(gameObject);

            Animator = new PlayerAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale;

            Inventory = new Item.Inventory();

            touchHandler.OnClick.Subscribe(_ =>
                {
                    if (Game.instance.Stage.IsInStage)
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
            CharacterModel = model;

            InitStats(model);
            UpdateEquipments(model.armor, model.weapon);
            UpdateCustomize();

            if (!SpeechBubble)
            {
                SpeechBubble = Widget.Create<SpeechBubble>();
            }

            SpeechBubble.speechBreakTime = GameConfig.PlayerSpeechBreakTime;
        }

        protected override IEnumerator Dying()
        {
            if (SpeechBubble)
            {
                SpeechBubble.Clear();
            }
            
            ShowSpeech("PLAYER_LOSE");

            yield return StartCoroutine(base.Dying());
        }

        protected override void OnDead()
        {
            gameObject.SetActive(false);
            Event.OnPlayerDead.Invoke();
        }

        protected override BoxCollider GetAnimatorHitPointBoxCollider()
        {
            return SpineController.BoxCollider;
        }
        
        #region AttackPoint & HitPoint

        protected override void UpdateHitPoint()
        {
            base.UpdateHitPoint();
            
            var center = HitPointBoxCollider.center;
            var size = HitPointBoxCollider.size;
            HitPointLocalOffset = new Vector3(center.x + size.x / 2, center.y - size.y / 2);
            attackPoint.transform.localPosition = new Vector3(HitPointLocalOffset.x + CharacterModel.attackRange, 0f);
        }
        
        #endregion

        #region Equipments & Customize

        public void UpdateEquipments(Armor armor, Weapon weapon = null)
        {
            UpdateArmor(armor);
            UpdateWeapon(weapon);
        }

        private void UpdateArmor(Armor armor)
        {
            var armorId = armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId;
            var spineResourcePath = armor?.Data.SpineResourcePath ?? $"Character/Player/{armorId}";

            if (!(Animator.Target is null))
            {
                var animatorTargetName = spineResourcePath.Split('/').Last();
                if (Animator.Target.name.Contains(animatorTargetName))
                    return;

                Animator.DestroyTarget();
            }

            var origin = Resources.Load<GameObject>(spineResourcePath);
            var go = Instantiate(origin, gameObject.transform);
            SpineController = go.GetComponent<PlayerSpineController>();
            Animator.ResetTarget(go);
            UpdateHitPoint();
        }

        public void UpdateWeapon(Weapon weapon)
        {
            if (!SpineController)
                return;

            var sprite = weapon.GetPlayerSpineTexture();
            SpineController.UpdateWeapon(sprite);
        }

        public void UpdateCustomize()
        {
            UpdateEye(Model.lensIndex);
            UpdateEar(Model.earIndex);
            UpdateTail(Model.tailIndex);
        }

        public void UpdateEar(int index)
        {
            UpdateEar($"ear_{index + 1:d4}_left", $"ear_{index + 1:d4}_right");
        }

        public void UpdateEar(string earLeftResource, string earRightResource)
        {
            if (!SpineController)
                return;

            if (string.IsNullOrEmpty(earLeftResource))
            {
                earLeftResource = $"ear_{Model.earIndex + 1:d4}_left";
            }

            if (string.IsNullOrEmpty(earRightResource))
            {
                earRightResource = $"ear_{Model.earIndex + 1:d4}_right";
            }

            var spriteLeft = SpriteHelper.GetPlayerSpineTextureEarLeft(earLeftResource);
            var spriteRight = SpriteHelper.GetPlayerSpineTextureEarRight(earRightResource);
            SpineController.UpdateEar(spriteLeft, spriteRight);
        }

        public void UpdateEye(int index)
        {
            UpdateEye(CostumeSheet.GetEyeOpenResourceByIndex(index), CostumeSheet.GetEyeHalfResourceByIndex(index));
        }

        public void UpdateEye(string eyeOpenResource, string eyeHalfResource)
        {
            if (!SpineController)
                return;

            if (string.IsNullOrEmpty(eyeOpenResource))
            {
                eyeOpenResource = CostumeSheet.GetEyeOpenResourceByIndex(Model.lensIndex);
            }

            if (string.IsNullOrEmpty(eyeHalfResource))
            {
                eyeHalfResource = CostumeSheet.GetEyeHalfResourceByIndex(Model.lensIndex);
            }

            var eyeOpenSprite = SpriteHelper.GetPlayerSpineTextureEyeOpen(eyeOpenResource);
            var eyeHalfSprite = SpriteHelper.GetPlayerSpineTextureEyeHalf(eyeHalfResource);
            SpineController.UpdateEye(eyeOpenSprite, eyeHalfSprite);
        }

        public void UpdateTail(int index)
        {
            UpdateTail($"tail_{index + 1:d4}");
        }

        public void UpdateTail(string tailResource)
        {
            if (!SpineController)
                return;

            if (string.IsNullOrEmpty(tailResource))
            {
                tailResource = $"tail_{Model.tailIndex + 1:d4}";
            }

            var sprite = SpriteHelper.GetPlayerSpineTextureTail(tailResource);
            SpineController.UpdateTail(sprite);
        }

        #endregion

        public IEnumerator CoGetExp(long exp)
        {
            if (exp <= 0)
            {
                yield break;
            }

            var level = Level;
            Model.GetExp(exp);
            EXP += exp;

            if (Level != level)
            {
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionStatusLevelUp, level);
                AudioController.instance.PlaySfx(AudioController.SfxCode.LevelUp);
                VFXController.instance.Create<BattleLevelUp01VFX>(transform, HUDOffset);
                InitStats(Model);
                var key = "";
                if (Level == GameConfig.CombinationRequiredLevel)
                {
                    key = "UI_UNLOCK_COMBINATION";
                }
                else if (Level == GameConfig.ShopRequiredLevel)
                {
                    key = "UI_UNLOCK_SHOP";
                }
                else if (Level == GameConfig.RankingRequiredLevel)
                {
                    key = "UI_UNLOCK_RANKING";
                }

                if (!string.IsNullOrEmpty(key))
                {
                    var w = Widget.Find<Alert>();
                    w.Show("UI_UNLOCK_TITLE", key);
                    yield return new WaitWhile(() => w.isActiveAndEnabled);
                }
            }

            UpdateHpBar();
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

        protected override void ProcessAttack(CharacterBase target, Model.Skill.SkillInfo skill, bool isLastHit,
            bool isConsiderElementalType)
        {
            ShowSpeech("PLAYER_SKILL", (int) skill.ElementalType, (int) skill.SkillCategory);
            base.ProcessAttack(target, skill, isLastHit, isConsiderElementalType);
            ShowSpeech("PLAYER_ATTACK");
        }

        protected override IEnumerator CoAnimationCast(Model.Skill.SkillInfo info)
        {
            ShowSpeech("PLAYER_SKILL", (int) info.ElementalType, (int) info.SkillCategory);
            yield return StartCoroutine(base.CoAnimationCast(info));
        }
    }
}
