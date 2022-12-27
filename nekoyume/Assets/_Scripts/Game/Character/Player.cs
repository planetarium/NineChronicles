using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using mixpanel;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.UI;
using UnityEngine;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Game.Character
{
    // NOTE: Avoid Ambiguous invocation:
    // System.IDisposable Subscribe<T>(this IObservable<T>, Action<T>)
    // System.ObservableExtensions and UniRx.ObservableExtensions
    using UniRx;

    // todo: 경험치 정보를 `CharacterBase`로 옮기는 것이 좋겠음.
    public class Player : CharacterBase
    {
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();
        private GameObject _cachedCharacterTitle;

        public long EXP = 0;
        public long EXPMax { get; private set; }

        public TouchHandler touchHandler;

        protected override float RunSpeedDefault => CharacterModel.RunSpeed;

        protected override Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);
        protected override Vector3 HudTextPosition => transform.TransformPoint(0f, 1.7f, 0f);

        public PlayerSpineController SpineController { get; private set; }

        public Model.Player Model => (Model.Player)CharacterModel;

        public bool AttackEnd => AttackEndCalled;

        private Costume fullCostume;

        public override string TargetTag => Tag.Enemy;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            OnUpdateHPBar.Subscribe(_ => Event.OnUpdatePlayerStatus.OnNext(this)).AddTo(gameObject);

            Animator = new PlayerAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale;

            touchHandler.OnClick.Merge(touchHandler.OnDoubleClick)
                .Merge(touchHandler.OnMultipleClick).Subscribe(_ =>
                {
                    if (Game.instance.IsInWorld || ActionCamera.instance.InPrologue)
                    {
                        return;
                    }

                    Animator.Touch();
                }).AddTo(gameObject);
        }

        protected override void Update()
        {
            base.Update();
            if (HudContainer)
            {
                if (Game.instance.IsInWorld)
                {
                    if (Game.instance.Stage.IsShowHud)
                    {
                        HudContainer.UpdateAlpha(IsDead ? 0 : 1);
                    }
                    else
                    {
                        HudContainer.UpdateAlpha(0);
                    }
                }
                else
                {
                    HudContainer.UpdateAlpha(SpineController.SkeletonAnimation.skeleton.A);
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Destroy(_cachedCharacterTitle);
        }

        private void OnDestroy()
        {
            Animator.Dispose();
        }

        #endregion

        public void Set(AvatarState avatarState)
        {
            var tableSheets = Game.instance.TableSheets;
            var model = new Model.Player(
                avatarState,
                tableSheets.CharacterSheet,
                tableSheets.CharacterLevelSheet,
                tableSheets.EquipmentItemSetEffectSheet);
            Set(model);
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
            Set(model, model.Costumes, model.armor, model.weapon, updateCurrentHP);
        }

        public void Set(
            Model.Player model,
            IEnumerable<Costume> costumes,
            Armor armor,
            Weapon weapon,
            bool updateCurrentHP)
        {
            InitStats(model);
            base.Set(model, updateCurrentHP);

            _disposablesForModel.DisposeAllAndClear();
            CharacterModel = model;
            EquipCostumes(costumes);
            EquipEquipmentsAndUpdateCustomize(armor, weapon);
            UpdateCustomize(costumes);

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
                SpeechBubble.Hide();
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

        private void UpdateTitle(Costume costume = null)
        {
            Destroy(_cachedCharacterTitle);

            if (costume == null)
            {
                return;
            }

            if (sortingGroup != null &&
                sortingGroup.sortingLayerID == SortingLayer.NameToID("UI"))
            {
                return;
            }

            if (HudContainer != null)
            {
                HudContainer.gameObject.SetActive(true);
                var clone = ResourcesHelper.GetCharacterTitle(costume.Grade,
                    costume.GetLocalizedNonColoredName(false));
                _cachedCharacterTitle = Instantiate(clone, HudContainer.transform);
                _cachedCharacterTitle.name = costume.Id.ToString();
                _cachedCharacterTitle.transform.SetAsFirstSibling();
            }
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

        public override float CalculateRange(CharacterBase target)
        {
            var attackRangeStartPosition = gameObject.transform.position.x + HitPointLocalOffset.x;
            var targetHitPosition = target.transform.position.x + target.HitPointLocalOffset.x;
            return targetHitPosition - attackRangeStartPosition;
        }

        #endregion

        #region Costumes

        private void EquipCostumes(IEnumerable<Costume> costumes)
        {
            if (costumes is null)
            {
                return;
            }

            fullCostume = null;
            foreach (var costume in costumes)
            {
                EquipCostume(costume);
            }
        }

        private void EquipCostume(Costume costume)
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
                    fullCostume = costume;
                    ChangeSpineObject(costume.SpineResourcePath, true);
                    break;
                case ItemSubType.HairCostume:
                    UpdateHairById(costume.Id);
                    break;
                case ItemSubType.TailCostume:
                    UpdateTailById(costume.Id);
                    break;
                case ItemSubType.Title:
                    UpdateTitle(costume);
                    break;
            }
        }

        #endregion

        #region Equipments

        private void EquipEquipmentsAndUpdateCustomize(Armor armor, Weapon weapon = null)
        {
            if (fullCostume is not null)
            {
                return;
            }

            EquipArmor(armor);
            EquipWeapon(weapon);
        }

        private void EquipArmor(Armor armor)
        {
            if (fullCostume is not null)
            {
                return;
            }

            var armorId = armor?.Id ?? GameConfig.DefaultAvatarArmorId;
            var spineResourcePath = armor?.SpineResourcePath ?? $"Character/Player/{armorId}";
            ChangeSpineObject(spineResourcePath, fullCostume is not null);
        }

        private void EquipWeapon(Weapon weapon)
        {
            if (fullCostume is not null || !SpineController)
            {
                return;
            }

            var id = weapon?.Id ?? 0;
            var level = weapon?.level ?? 0;
            var levelVFXPrefab = ResourcesHelper.GetAuraWeaponPrefab(id, level);
            var sprite = weapon.GetPlayerSpineTexture();
            SpineController.UpdateWeapon(id, sprite, levelVFXPrefab);
        }

        public void Equip(int armorId, int weaponId)
        {
            var spineResourcePath = $"Character/Player/{armorId}";
            ChangeSpineObject(spineResourcePath, fullCostume is not null);
            var sprite = SpriteHelper.GetPlayerSpineTextureWeapon(weaponId);
            SpineController.UpdateWeapon(weaponId, sprite);
        }

        #endregion

        #region Customize

        private void UpdateCustomize(IEnumerable<Costume> costumes)
        {
            if (fullCostume is not null)
            {
                return;
            }

            var subTypes = costumes.Select(x => x.ItemSubType).ToList();
            if (!subTypes.Exists(x => x == ItemSubType.EarCostume))
            {
                UpdateEarByCustomizeIndex(Model.earIndex);
            }

            if (!subTypes.Exists(x => x == ItemSubType.EyeCostume))
            {
                UpdateEyeByCustomizeIndex(Model.lensIndex);
            }

            if (!subTypes.Exists(x => x == ItemSubType.HairCostume))
            {
                UpdateHairByCustomizeIndex(Model.hairIndex);
            }

            if (!subTypes.Exists(x => x == ItemSubType.TailCostume))
            {
                UpdateTailByCustomizeIndex(Model.tailIndex);
            }
        }

        /// <summary>
        /// 기존에 커스텀 가능한 귀 디자인들은 EarCostume 중에서 첫 번째 부터 10번째 까지를 대상으로 합니다.
        /// </summary>
        /// <param name="customizeIndex">origin : 0 ~ 9,999 / partnership 10,000 ~ 19,999 </param>
        public void UpdateEarByCustomizeIndex(int customizeIndex)
        {
            if (fullCostume is not null || !SpineController)
            {
                return;
            }

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
            const string prefix = "Character/PlayerSpineTexture/EarCostume";
            var leftSprite = Resources.Load<Sprite>($"{prefix}/{earCostumeId}_left");
            var rightSprite = Resources.Load<Sprite>($"{prefix}/{earCostumeId}_right");
            SpineController.UpdateEar(leftSprite, rightSprite);
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
            var prefix = "Character/PlayerSpineTexture/EyeCostume";
            var halfSprite = Resources.Load<Sprite>($"{prefix}/{eyeCostumeId}_half");
            var openSprite = Resources.Load<Sprite>($"{prefix}/{eyeCostumeId}_open");
            SpineController.UpdateEye(halfSprite, openSprite);
        }

        /// <summary>
        /// 기존에 커스텀 가능한 머리카 디자인들은 HairCostume 중에서 첫 번째 부터 6번째 까지를 대상으로 합니다.
        /// </summary>
        /// <param name="customizeIndex">0~5</param>
        public void UpdateHairByCustomizeIndex(int customizeIndex)
        {
            if (fullCostume is not null || !SpineController)
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
            if (!TryGetCostumeRow(hairCostumeId, out var row))
            {
                return;
            }

            var sprites = Enumerable.Range(0, SpineController.HairSlotCount)
                .Select(index =>
                    $"{row.SpineResourcePath}_{SpineController.hairTypeIndex:00}_{index + 1:00}")
                .Select(Resources.Load<Sprite>).ToList();
            SpineController.UpdateHair(sprites);
        }

        /// <summary>
        /// 기존에 커스텀 가능한 꼬리 디자인들은 TailCostume 중에서 첫 번째 부터 10번째 까지를 대상으로 합니다.
        /// </summary>
        /// <param name="customizeIndex">0~9</param>
        public void UpdateTailByCustomizeIndex(int customizeIndex)
        {
            if (fullCostume is not null || !SpineController)
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
            SpineController.UpdateTail(tailCostumeId);
        }

        private bool TryGetCostumeRow(int costumeId, out CostumeItemSheet.Row row)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            return sheet.TryGetValue(costumeId, out row, false);
        }

        #endregion

        private void ChangeSpineObject(string spineResourcePath, bool isFullCostume, bool updateHitPoint = true)
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
            if (!origin)
            {
                throw new FailedToLoadResourceException<GameObject>(spineResourcePath);
            }

            var go = Instantiate(origin, gameObject.transform);
            SpineController = go.GetComponent<PlayerSpineController>();
            if (!isFullCostume)
            {
                SpineController.AttachTail();
            }

            Animator.ResetTarget(go);

            if (updateHitPoint)
            {
                UpdateHitPoint();
            }
        }

        public void ChangeSpineResource(string id, bool isFullCostume, bool updateHitPoint = true)
        {
            var spineResourcePath =
                isFullCostume ? $"Character/FullCostume/{id}" : $"Character/Player/{id}";

            ChangeSpineObject(spineResourcePath, isFullCostume, updateHitPoint);
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
                Analyzer.Instance.Track("Unity/User Level Up", new Dictionary<string, Value>()
                {
                    ["code"] = level,
                    ["AvatarAddress"] = Game.instance.States.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = Game.instance.States.AgentState.address.ToString(),
                });

                Widget.Find<LevelUpCelebratePopup>()?.Show(level, Level);
                InitStats(Model);
            }

            UpdateHpBar();
        }

        private void InitStats(Model.Player character)
        {
            EXP = character.Exp.Current;
            EXPMax = character.Exp.Max;
        }

        protected override void ProcessAttack(CharacterBase target,
            Model.BattleStatus.Skill.SkillInfo skill,
            bool isLastHit,
            bool isConsiderElementalType)
        {
            ShowSpeech("PLAYER_SKILL", (int)skill.ElementalType, (int)skill.SkillCategory);
            base.ProcessAttack(target, skill, isLastHit, isConsiderElementalType);
            ShowSpeech("PLAYER_ATTACK");
        }

        protected override IEnumerator CoAnimationCast(Model.BattleStatus.Skill.SkillInfo info)
        {
            ShowSpeech("PLAYER_SKILL", (int)info.ElementalType, (int)info.SkillCategory);
            yield return StartCoroutine(base.CoAnimationCast(info));
        }

        protected override void ShowCutscene()
        {
            AreaAttackCutscene.Show(Helper.Util.GetArmorId());
        }
    }
}
