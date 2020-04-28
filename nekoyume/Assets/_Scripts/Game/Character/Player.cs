using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.Manager;
using Nekoyume.Model.Item;
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

        public Model.Item.Inventory Inventory;
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

            Inventory = new Model.Item.Inventory();

            touchHandler.OnClick
                .Merge(touchHandler.OnDoubleClick)
                .Merge(touchHandler.OnMultipleClick)
                .Subscribe(_ =>
                {
                    if (Game.instance.Stage.IsInStage)
                    {
                        return;
                    }

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
            {
                throw new ArgumentException(nameof(model));
            }

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
            if (!(this is EnemyPlayer))
            {
                Widget.Find<UI.Battle>().comboText.comboMax = CharacterModel.AttackCountMax;
            }
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

        protected override void OnDeadEnd()
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
            attackPoint.transform.localPosition =
                new Vector3(HitPointLocalOffset.x + CharacterModel.attackRange, 0f);
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
                {
                    return;
                }

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
            {
                return;
            }

            var sprite = weapon.GetPlayerSpineTexture();
            SpineController.UpdateWeapon(sprite);
        }

        public void UpdateCustomize()
        {
            UpdateEar(Model.earIndex);
            UpdateEye(Model.lensIndex);
            UpdateHair(Model.hairIndex);
            UpdateTail(Model.tailIndex);
        }

        public void UpdateEar(int index)
        {
            UpdateEar($"ear_{index + 1:d4}_left", $"ear_{index + 1:d4}_right");
        }

        private void UpdateEar(string earLeftResource, string earRightResource)
        {
            if (!SpineController ||
                string.IsNullOrEmpty(earLeftResource) ||
                string.IsNullOrEmpty(earRightResource))
            {
                return;
            }

            var spriteLeft = SpriteHelper.GetPlayerSpineTextureEarLeft(earLeftResource);
            var spriteRight = SpriteHelper.GetPlayerSpineTextureEarRight(earRightResource);
            SpineController.UpdateEar(spriteLeft, spriteRight);
        }

        public void UpdateEye(int index)
        {
            UpdateEye(CostumeSheet.GetEyeResources(index));
        }

        private void UpdateEye(IReadOnlyList<string> eyeResources)
        {
            if (eyeResources is null ||
                eyeResources.Count < 2 ||
                !SpineController)
            {
                return;
            }

            var eyeHalfSprite = SpriteHelper.GetPlayerSpineTextureEyeHalf(eyeResources[0]);
            var eyeOpenSprite = SpriteHelper.GetPlayerSpineTextureEyeOpen(eyeResources[1]);
            SpineController.UpdateEye(eyeHalfSprite, eyeOpenSprite);
        }

        /// <param name="colorIndex">0~5</param>
        public void UpdateHair(int colorIndex)
        {
            if (SpineController is null)
            {
                return;
            }

            UpdateHair(CostumeSheet.GetHairResources(SpineController.hairTypeIndex, colorIndex));
        }

        private void UpdateHair(IReadOnlyCollection<string> hairResources)
        {
            if (hairResources is null||
                hairResources.Count < 6 ||
                !SpineController)
            {
                return;
            }

            var sprites = hairResources
                .Select(SpriteHelper.GetPlayerSpineTextureHair)
                .ToList();

            SpineController.UpdateHair(sprites);
        }

        public void UpdateTail(int index)
        {
            UpdateTail($"tail_{index + 1:d4}");
        }

        private void UpdateTail(string tailResource)
        {
            if (!SpineController ||
                string.IsNullOrEmpty(tailResource))
            {
                return;
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
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionStatusLevelUp,
                    level);
                AudioController.instance.PlaySfx(AudioController.SfxCode.LevelUp);
                VFXController.instance.Create<BattleLevelUp01VFX>(transform, HUDOffset);
                InitStats(Model);
            }

            UpdateHpBar();
        }

        private void InitStats(Model.Player character)
        {
            EXP = character.Exp.Current;
            EXPMax = character.Exp.Max;
            Inventory = character.Inventory;
        }

        protected override void ProcessAttack(CharacterBase target,
            Model.BattleStatus.Skill.SkillInfo skill, bool isLastHit,
            bool isConsiderElementalType)
        {
            ShowSpeech("PLAYER_SKILL", (int) skill.ElementalType, (int) skill.SkillCategory);
            base.ProcessAttack(target, skill, isLastHit, isConsiderElementalType);
            ShowSpeech("PLAYER_ATTACK");
        }

        protected override IEnumerator CoAnimationCast(Model.BattleStatus.Skill.SkillInfo info)
        {
            ShowSpeech("PLAYER_SKILL", (int) info.ElementalType, (int) info.SkillCategory);
            yield return StartCoroutine(base.CoAnimationCast(info));
        }
    }
}
