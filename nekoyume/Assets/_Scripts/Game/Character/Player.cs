using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.Manager;
using Nekoyume.Model.Item;
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

        public bool AttackEnd => AttackEndCalled;

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
                    if (Game.instance.Stage.IsInStage || ActionCamera.instance.InPrologue)
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
            // NOTE: InitStats()를 호출한 후에 base.Set()을 호출합니다.
            // 이는 InitStats()내에서 Inventory가 할당되기 때문입니다.
            // base.Set()에서 updateCurrentHP 파라메터가 true일 때 내부적으로 InitializeHpBar()가 호출되는데,
            // 이때 Inventory가 채워져 있어야 하기 때문입니다.
            InitStats(model);
            base.Set(model, updateCurrentHP);

            _disposablesForModel.DisposeAllAndClear();
            CharacterModel = model;

            EquipCostumes(model.Costumes);
            EquipEquipmentsAndUpdateCustomize(model.armor, model.weapon);

            if (!SpeechBubble)
            {
                SpeechBubble = Widget.Create<SpeechBubble>();
            }

            SpeechBubble.speechBreakTime = GameConfig.PlayerSpeechBreakTime;
            if (!(this is EnemyPlayer))
            {
                Widget.Find<UI.Battle>().ComboText.comboMax = CharacterModel.AttackCountMax;
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

        protected override void InitializeHpBar()
        {
            base.InitializeHpBar();

            var title = Costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);
            HPBar.SetTitle(title);
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

            switch (costume.ItemSubType)
            {
                case ItemSubType.EarCostume:
                    UpdateEarById(costume.Id);
                    break;
                case ItemSubType.EyeCostume:
                    UpdateEyeById(costume.Id);
                    break;
                case ItemSubType.FullCostume:
                    ChangeSpine(costume.SpineResourcePath);
                    break;
                case ItemSubType.HairCostume:
                    UpdateHairById(costume.Id);
                    break;
                case ItemSubType.TailCostume:
                    UpdateTailById(costume.Id);
                    break;
                case ItemSubType.Title:
                    // TODO: 구현!
                    break;
            }
        }

        public void UnequipCostume(Costume costume, bool ignoreEquipmentsAndCustomize = false)
        {
            if (costume is null)
            {
                return;
            }

            switch (costume.ItemSubType)
            {
                case ItemSubType.EarCostume:
                    UpdateEar();
                    break;
                case ItemSubType.EyeCostume:
                    UpdateEye();
                    break;
                case ItemSubType.FullCostume:
                    if (!ignoreEquipmentsAndCustomize)
                    {
                        var armor = (Armor) Equipments.FirstOrDefault(equipment =>
                            equipment.ItemSubType == ItemSubType.Armor);
                        var weapon = (Weapon) Equipments.FirstOrDefault(equipment =>
                            equipment.ItemSubType == ItemSubType.Weapon);
                        EquipEquipmentsAndUpdateCustomize(armor, weapon);
                    }

                    break;
                case ItemSubType.HairCostume:
                    UpdateHair();
                    break;
                case ItemSubType.TailCostume:
                    UpdateTail();
                    break;
                case ItemSubType.Title:
                    // TODO: 구현!
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

        public void Equip(int armorId, int weaponId)
        {
            var spineResourcePath = $"Character/Player/{armorId}";
            ChangeSpine(spineResourcePath);
            var sprite = SpriteHelper.GetPlayerSpineTextureWeapon(weaponId);
            SpineController.UpdateWeapon(sprite);
        }

        #endregion

        #region Customize

        private void UpdateCustomize()
        {
            if (IsFullCostumeEquipped)
            {
                return;
            }

            UpdateEar();
            UpdateEye();
            UpdateHair();
            UpdateTail();
        }

        private void UpdateEar()
        {
            if (IsFullCostumeEquipped)
            {
                return;
            }

            var earCostume =
                Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.EarCostume);
            if (earCostume is null)
            {
                UpdateEarByCustomizeIndex(Model.earIndex);
            }
            else
            {
                UpdateEarById(earCostume.Id);
            }
        }

        /// <summary>
        /// 기존에 커스텀 가능한 귀 디자인들은 EarCostume 중에서 첫 번째 부터 10번째 까지를 대상으로 합니다.
        /// </summary>
        /// <param name="customizeIndex">0~9</param>
        public void UpdateEarByCustomizeIndex(int customizeIndex)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var firstEarRow =
                sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.EarCostume);
            if (firstEarRow is null)
            {
                return;
            }

            UpdateEarById(firstEarRow.Id + customizeIndex);
        }

        private void UpdateEarById(int earCostumeId)
        {
            if (IsFullCostumeEquipped ||
                SpineController is null)
            {
                return;
            }

            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            if (!sheet.TryGetValue(earCostumeId, out var row, true))
            {
                return;
            }

            var leftSprite = Resources.Load<Sprite>($"{row.SpineResourcePath}_left");
            var rightSprite = Resources.Load<Sprite>($"{row.SpineResourcePath}_right");
            SpineController.UpdateEar(leftSprite, rightSprite);
        }

        private void UpdateEye()
        {
            if (IsFullCostumeEquipped)
            {
                return;
            }

            var eyeCostume =
                Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.EyeCostume);
            if (eyeCostume is null)
            {
                UpdateEyeByCustomizeIndex(Model.lensIndex);
            }
            else
            {
                UpdateEyeById(eyeCostume.Id);
            }
        }

        /// <summary>
        /// 기존에 커스텀 가능한 눈 디자인들은 EyeCostume 중에서 첫 번째 부터 6번째 까지를 대상으로 합니다.
        /// </summary>
        /// <param name="customizeIndex">0~5</param>
        public void UpdateEyeByCustomizeIndex(int customizeIndex)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var firstEyeRow =
                sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.EyeCostume);
            if (firstEyeRow is null)
            {
                return;
            }

            UpdateEyeById(firstEyeRow.Id + customizeIndex);
        }

        private void UpdateEyeById(int eyeCostumeId)
        {
            if (IsFullCostumeEquipped ||
                SpineController is null)
            {
                return;
            }

            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            if (!sheet.TryGetValue(eyeCostumeId, out var row, true))
            {
                return;
            }

            var halfSprite = Resources.Load<Sprite>($"{row.SpineResourcePath}_half");
            var openSprite = Resources.Load<Sprite>($"{row.SpineResourcePath}_open");
            SpineController.UpdateEye(halfSprite, openSprite);
        }

        private void UpdateHair()
        {
            if (IsFullCostumeEquipped)
            {
                return;
            }

            var hairCostume =
                Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.HairCostume);
            if (hairCostume is null)
            {
                UpdateHairByCustomizeIndex(Model.hairIndex);
            }
            else
            {
                UpdateHairById(hairCostume.Id);
            }
        }

        /// <summary>
        /// 기존에 커스텀 가능한 머리카 디자인들은 HairCostume 중에서 첫 번째 부터 6번째 까지를 대상으로 합니다.
        /// </summary>
        /// <param name="customizeIndex">0~5</param>
        public void UpdateHairByCustomizeIndex(int customizeIndex)
        {
            if (IsFullCostumeEquipped)
            {
                return;
            }

            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var firstHairRow =
                sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.HairCostume);
            if (firstHairRow is null)
            {
                return;
            }

            UpdateHairById(firstHairRow.Id + customizeIndex);
        }

        private void UpdateHairById(int hairCostumeId)
        {
            if (IsFullCostumeEquipped ||
                SpineController is null)
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

        private void UpdateTail()
        {
            if (IsFullCostumeEquipped)
            {
                return;
            }

            var tailCostume =
                Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.TailCostume);
            if (tailCostume is null)
            {
                UpdateTailByCustomizeIndex(Model.tailIndex);
            }
            else
            {
                UpdateTailById(tailCostume.Id);
            }
        }

        /// <summary>
        /// 기존에 커스텀 가능한 꼬리 디자인들은 TailCostume 중에서 첫 번째 부터 10번째 까지를 대상으로 합니다.
        /// </summary>
        /// <param name="customizeIndex">0~9</param>
        public void UpdateTailByCustomizeIndex(int customizeIndex)
        {
            if (IsFullCostumeEquipped)
            {
                return;
            }

            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var firstTailRow =
                sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.TailCostume);
            if (firstTailRow is null)
            {
                return;
            }

            UpdateTailById(firstTailRow.Id + customizeIndex);
        }

        private void UpdateTailById(int tailCostumeId)
        {
            if (IsFullCostumeEquipped ||
                SpineController is null)
            {
                return;
            }

            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            if (!sheet.TryGetValue(tailCostumeId, out var row, true))
            {
                return;
            }

            var sprite = Resources.Load<Sprite>(row.SpineResourcePath);
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
                    characterarchetype_slug: States.Instance.CurrentAvatarState.address.ToHex()
                        .Substring(0, 4),
                    level_from: (int) level,
                    level_to: (int) Level
                );

                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionStatusLevelUp,
                    level);
                Widget.Find<LevelUpCelebratePopup>()?.Show(level, Level);
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
