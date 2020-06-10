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
using Nekoyume.State;
using TentuPlay.Api;
using Nekoyume.Model.State;

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

        public List<Costume> Costumes =>
            Inventory.Items.Select(i => i.item).OfType<Costume>().Where(e => e.equipped).ToList();

        public List<Equipment> Equipments =>
            Inventory.Items.Select(i => i.item).OfType<Equipment>().Where(e => e.equipped).ToList();

        protected override float RunSpeedDefault => CharacterModel.RunSpeed;

        protected override Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);
        protected override Vector3 HudTextPosition => transform.TransformPoint(0f, 1.7f, 0f);

        public PlayerSpineController SpineController { get; private set; }
        public Model.Player Model => (Model.Player) CharacterModel;

        private bool IsFullCostumeEquipped =>
            Costumes.Any(costume => costume.ItemSubType == ItemSubType.FullCostume);

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

        public void Set(AvatarState avatarState)
        {
            Set(new Model.Player(avatarState, Game.instance.TableSheets));
        }

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
            EquipCostumes(model.Costumes);
            EquipEquipmentsAndUpdateCustomize(model.armor, model.weapon);

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

        #region Costumes

        private void EquipCostumes(IEnumerable<Costume> costumes)
        {
            foreach (var costume in costumes)
            {
                EquipCostume(costume);
            }
        }

        public void EquipCostume(Costume costume)
        {
            if (costume is null)
            {
                return;
            }

            // TODO: FullCostume 이외의 코스튬은 추가 구현한다.
            switch (costume.ItemSubType)
            {
                case ItemSubType.EarCostume:
                    // UpdateEar();
                    break;
                case ItemSubType.EyeCostume:
                    // UpdateEye();
                    break;
                case ItemSubType.FullCostume:
                    ChangeSpine(costume.SpineResourcePath);
                    break;
                case ItemSubType.HairCostume:
                    // UpdateHair();
                    break;
                case ItemSubType.TailCostume:
                    // UpdateTail();
                    break;
            }
        }

        public void UnequipCostume(Costume costume)
        {
            if (costume is null)
            {
                return;
            }

            // TODO: FullCostume 이외의 코스튬은 추가 구현한다.
            switch (costume.ItemSubType)
            {
                case ItemSubType.EarCostume:
                    // UpdateEar();
                    break;
                case ItemSubType.EyeCostume:
                    // UpdateEye();
                    break;
                case ItemSubType.FullCostume:
                    if (CharacterModel is Model.Player model)
                    {
                        EquipEquipmentsAndUpdateCustomize(model.armor, model.weapon);
                    }

                    break;
                case ItemSubType.HairCostume:
                    // UpdateHair();
                    break;
                case ItemSubType.TailCostume:
                    // UpdateTail();
                    break;
            }
        }

        #endregion

        #region Equipments

        public void EquipEquipmentsAndUpdateCustomize(Armor armor, Weapon weapon = null)
        {
            if (IsFullCostumeEquipped)
            {
                return;
            }

            EquipArmor(armor);
            EquipWeapon(weapon);
            UpdateCustomize();
        }

        private void EquipArmor(Armor armor)
        {
            if (IsFullCostumeEquipped)
            {
                return;
            }

            var armorId = armor?.Id ?? GameConfig.DefaultAvatarArmorId;
            var spineResourcePath = armor?.SpineResourcePath ?? $"Character/Player/{armorId}";
            ChangeSpine(spineResourcePath);
        }

        public void EquipWeapon(Weapon weapon)
        {
            if (IsFullCostumeEquipped ||
                !SpineController)
            {
                return;
            }

            var sprite = weapon.GetPlayerSpineTexture();
            SpineController.UpdateWeapon(sprite);
        }

        #endregion

        // TODO: 최초에 캐릭터 생성 시에만 커스터마이징하는 개념으로 개발되었으나 그 기능이 코스튬과 같기 때문에 이 둘을 적절하게 리펙토링 할 필요가 있습니다.
        // 각 부위의 코스튬을 개발할 때 진행하면 좋겠습니다.

        #region Customize

        private void UpdateCustomize()
        {
            if (IsFullCostumeEquipped)
            {
                return;
            }

            UpdateEar(Model.earIndex);
            UpdateEye(Model.lensIndex);
            UpdateHairByCustomizeIndex(Model.hairIndex);
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

            var spriteLeft = SpriteHelper.GetPlayerSpineTextureEarCostumeLeft(earLeftResource);
            var spriteRight = SpriteHelper.GetPlayerSpineTextureEarCostumeRight(earRightResource);
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

            var eyeHalfSprite = SpriteHelper.GetPlayerSpineTextureEyeCostumeHalf(eyeResources[0]);
            var eyeOpenSprite = SpriteHelper.GetPlayerSpineTextureEyeCostumeOpen(eyeResources[1]);
            SpineController.UpdateEye(eyeHalfSprite, eyeOpenSprite);
        }

        /// <summary>
        /// 기존에 커스텀 가능한 헤어 컬러들은 헤어 코스튬 중에서 첫 번째 부터 6번째까지를 대상으로 합니다.
        /// </summary>
        /// <param name="customizeColorIndex">0~5</param>
        public void UpdateHairByCustomizeIndex(int customizeColorIndex)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var firstHairRow = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.HairCostume);
            if (firstHairRow is null)
            {
                return;
            }

            UpdateHairById(firstHairRow.Id + customizeColorIndex);
        }

        private void UpdateHairById(int hairCostumeId)
        {
            if (SpineController is null)
            {
                return;
            }

            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            if (!sheet.TryGetValue(hairCostumeId, out var row, true))
            {
                return;
            }

            var sprites = Enumerable
                .Range(0, SpineController.HairSlotCount)
                .Select(index =>
                    $"{row.SpineResourcePath}_{SpineController.hairTypeIndex:00}_{index + 1:00}")
                .Select(Resources.Load<Sprite>)
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

            var sprite = SpriteHelper.GetPlayerSpineTextureTailCostume(tailResource);
            SpineController.UpdateTail(sprite);
        }

        #endregion

        private void ChangeSpine(string spineResourcePath)
        {
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
                //[TentuPlay] 아바타 레벨업 기록
                new TPStashEvent().CharacterLevelUp(
                    player_uuid: Game.instance.Agent.Address.ToHex(),
                    characterarchetype_slug: States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 4),
                    level_from: (int)level,
                    level_to: (int)Level
                    );

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
