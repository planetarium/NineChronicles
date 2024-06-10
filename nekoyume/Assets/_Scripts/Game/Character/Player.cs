using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Crypto;
using mixpanel;
using Nekoyume.Game.Avatar;
using Nekoyume.Game.Battle;
using Nekoyume.Model.Item;
using Nekoyume.UI;
using UnityEngine;
using Nekoyume.Model.State;

namespace Nekoyume.Game.Character
{
    using Nekoyume.Helper;
    // NOTE: Avoid Ambiguous invocation:
    // System.IDisposable Subscribe<T>(this IObservable<T>, Action<T>)
    // System.ObservableExtensions and UniRx.ObservableExtensions
    using UniRx;

    // todo: 경험치 정보를 `CharacterBase`로 옮기는 것이 좋겠음.
    public class Player : Actor
    {
        [SerializeField]
        private CharacterAppearance appearance;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();
        private GameObject _cachedCharacterTitle;

        public long EXP = 0;
        public long EXPMax { get; private set; }

        public TouchHandler touchHandler;

        public Pet Pet => appearance.Pet;

        protected override float RunSpeedDefault => CharacterModel.RunSpeed * Game.instance.Stage.AnimationTimeScaleWeight;

        protected override Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);
        protected override Vector3 HudTextPosition => transform.TransformPoint(0f, 1.7f, 0f);

        public AvatarSpineController SpineController => appearance.SpineController;

        public Model.Player Model => (Model.Player)CharacterModel;

        public bool AttackEnd => AttackEndCalled;

        private Costume _fullCostume;

        public override string TargetTag => Tag.Enemy;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            OnUpdateActorHud.Subscribe(_ => Event.OnUpdatePlayerStatus.OnNext(this)).AddTo(gameObject);

            Animator = new PlayerAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = Game.instance.Stage.AnimationTimeScaleWeight;

            touchHandler.OnClick.Merge(touchHandler.OnDoubleClick)
                .Merge(touchHandler.OnMultipleClick).Subscribe(_ =>
                {
                    if (BattleRenderer.Instance.IsOnBattle || ActionCamera.instance.InPrologue)
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
                if (BattleRenderer.Instance.IsOnBattle)
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
                    HudContainer.UpdateAlpha(SpineController.GetSpineAlpha());
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

        public override void Set(Model.CharacterBase model, bool updateCurrentHp = false)
        {
            if (!(model is Model.Player playerModel))
            {
                throw new ArgumentException(nameof(model));
            }

            var avatarState = Game.instance.States.CurrentAvatarState;
            Set(avatarState.address, playerModel, updateCurrentHp);
        }

        public void Set(Address avatarAddress, Model.Player model, bool updateCurrentHP)
        {
            Set(avatarAddress, model, model.Costumes, model.armor, model.weapon, model.aura, updateCurrentHP);
        }

        public void Set(
            Address avatarAddress,
            Model.Player model,
            IEnumerable<Costume> costumes,
            Armor armor,
            Weapon weapon,
            Aura aura,
            bool updateCurrentHP)
        {
            InitStats(model);
            base.Set(model, updateCurrentHP);

            _disposablesForModel.DisposeAllAndClear();
            CharacterModel = model;

            appearance.Set(
                avatarAddress,
                Animator,
                HudContainer,
                costumes.ToList(),
                armor,
                weapon,
                aura,
                model.earIndex,
                model.lensIndex,
                model.hairIndex,
                model.tailIndex);

            if (!SpeechBubble)
            {
                SpeechBubble = Widget.Create<SpeechBubble>();
            }

            SpeechBubble.speechBreakTime = GameConfig.PlayerSpeechBreakTime;
            if (!(this is EnemyPlayer))
            {
                Widget.Find<UI.Battle>().ComboText.comboMax = CharacterModel.AttackCountMax;
            }

            IsFlipped = false;
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
            return appearance.BoxCollider;
        }

        #region AttackPoint & HitPoint

        protected override void UpdateHitPoint()
        {
            var scale = Vector3.one;
            var center = GetAnimatorHitPointBoxCollider().center;
            var size = GetAnimatorHitPointBoxCollider().size;
            HitPointBoxCollider.center =
                new Vector3(center.x * scale.x, center.y * scale.y, center.z * scale.z);
            HitPointBoxCollider.size =
                new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
            HitPointLocalOffset = new Vector3(center.x + size.x / 2, center.y - size.y / 2);
            attackPoint.transform.localPosition =
                new Vector3(HitPointLocalOffset.x + CharacterModel.attackRange, 0f);
        }

        public override float CalculateRange(Actor target)
        {
            var attackRangeStartPosition = gameObject.transform.position.x + HitPointLocalOffset.x;
            var targetHitPosition = target.transform.position.x + target.HitPointLocalOffset.x;
            return targetHitPosition - attackRangeStartPosition;
        }

        #endregion

        public void EquipForPrologue(int armorId, int weaponId)
        {
            appearance.SetForPrologue(
                Animator,
                HudContainer,
                armorId,
                weaponId,
                0,
                0,
                0,
                0);
            SpineController.UpdateWeapon(weaponId);
        }

        // /// <summary>
        // /// 기존에 커스텀 가능한 귀 디자인들은 EarCostume 중에서 첫 번째 부터 10번째 까지를 대상으로 합니다.
        // /// </summary>
        // /// <param name="customizeIndex">origin : 0 ~ 9,999 / partnership 10,000 ~ 19,999 </param>
        public void UpdateEarByCustomizeIndex(int customizeIndex)
        {
            if (_fullCostume is not null || !SpineController)
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

            SpineController.UpdateEar(firstEarRow.Id + customizeIndex, false);
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

            SpineController.UpdateFace(firstEyeRow.Id + customizeIndex, false);
        }

        //
        // /// <summary>
        // /// 기존에 커스텀 가능한 머리카 디자인들은 HairCostume 중에서 첫 번째 부터 6번째 까지를 대상으로 합니다.
        // /// </summary>
        // /// <param name="customizeIndex">0~5</param>
        public void UpdateHairByCustomizeIndex(int customizeIndex)
        {
            if (_fullCostume is not null || !SpineController)
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

            SpineController.UpdateHair(firstHairRow.Id + customizeIndex, false);
        }

        //
        // /// <summary>
        // /// 기존에 커스텀 가능한 꼬리 디자인들은 TailCostume 중에서 첫 번째 부터 10번째 까지를 대상으로 합니다.
        // /// </summary>
        // /// <param name="customizeIndex">0~9</param>
        public void UpdateTailByCustomizeIndex(int customizeIndex)
        {
            if (_fullCostume is not null || !SpineController)
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

            SpineController.UpdateTail(firstTailRow.Id + customizeIndex, false);
        }

        public IEnumerator CoGetExp(long exp)
        {
            if (exp <= 0)
            {
                yield break;
            }

            var beforeLevel = Level;
            Model.GetExp(exp);
            EXP += exp;

            if (Level != beforeLevel)
            {
                Analyzer.Instance.Track("Unity/User Level Up", new Dictionary<string, Value>()
                {
                    ["code"] = beforeLevel,
                    ["AvatarAddress"] = Game.instance.States.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = Game.instance.States.AgentState.address.ToString(),
                });

                var evt = new AirbridgeEvent("User_Level_Up");
                evt.SetValue(beforeLevel);
                evt.AddCustomAttribute("agent-address", Game.instance.States.CurrentAvatarState.address.ToString());
                evt.AddCustomAttribute("avatar-address", Game.instance.States.AgentState.address.ToString());
                AirbridgeUnity.TrackEvent(evt);

                Widget.Find<LevelUpCelebratePopup>()?.Show(beforeLevel, Level);
                for (int interLevel = beforeLevel + 1; interLevel <= Level; interLevel++)
                {
                    Widget.Find<UI.Module.HeaderMenuStatic>().UpdatePortalRewardByLevel(interLevel);
                }
                InitStats(Model);
            }

            UpdateActorHud();
        }

        private void InitStats(Model.Player character)
        {
            EXP = character.Exp.Current;
            EXPMax = character.Exp.Max;
        }

        protected override void ProcessAttack(Actor target,
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

        public override void SetSpineColor(Color color, int propertyID = -1)
        {
            base.SetSpineColor(color, propertyID);
            if (appearance != null)
            {
                appearance.SetSpineColor(color, propertyID);
            }
        }
    }
}
