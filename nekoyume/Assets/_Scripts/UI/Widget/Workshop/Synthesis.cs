#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;
using Inventory = Nekoyume.Model.Item.Inventory;
using ToggleGroup = Nekoyume.UI.Module.ToggleGroup;

namespace Nekoyume.UI
{
    using UniRx;

    public class Synthesis : Widget
    {
        public const int MaxSynthesisCount = 12;

        private const ItemSubType DefaultItemSubType = ItemSubType.Aura;

        private static string TutorialCheckKey => $"Tutorial_Check_Synthesis_{Game.Game.instance.States.CurrentAvatarKey}";

        private readonly List<IDisposable> _activeDisposables = new();
        private readonly ToggleGroup _toggleGroup = new();

        [Serializable]
        private struct SynthesizeTapGroup
        {
            public ItemSubType iemSubType;
            public CategoryTabButton tabButton;
        }

        #region SerializeField

        [SerializeField]
        private SynthesisModule synthesisModule = null!;

        [SerializeField]
        private SynthesisScroll synthesisScroll = null!;

        [SerializeField]
        private SynthesizeTapGroup[] synthesisTapGroup = null!;

        [SerializeField]
        private Button closeButton = null!;

        #endregion SerializeField

        #region Field

        private ItemSubType _currentItemSubType = DefaultItemSubType;

        private Inventory? _cachedInventory;

        private readonly List<SynthesizeModel> _gradeItems = new();

        #endregion Field

        #region Properties

        private ItemSubType CurrentItemSubType
        {
            get => _currentItemSubType;
            set
            {
                if (_currentItemSubType == value)
                {
                    return;
                }

                _currentItemSubType = value;
                UpdateGradeItems();
            }
        }

        public SynthesisModule SynthesisModule => synthesisModule;

        #endregion Properties

        #region MonoBehaviour

        protected override void Awake()
        {
            CheckNull();
            base.Awake();

            closeButton.onClick.AddListener(OnCloseWidget);
            CloseWidget = OnCloseWidget;

            foreach (var tapGroup in synthesisTapGroup)
            {
                var tapButton = tapGroup.tabButton;
                _toggleGroup.RegisterToggleable(tapButton);
                tapButton.OnClick.Subscribe(_ =>
                {
                    CurrentItemSubType = tapGroup.iemSubType;
                }).AddTo(gameObject);
            }
        }

        private void OnCloseWidget()
        {
            Close(true);
            Find<CombinationMain>().Show();
            Find<HeaderMenuStatic>()?.UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            ReactiveAvatarState.Inventory
                               .Subscribe(UpdateInventory)
                               .AddTo(_activeDisposables);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _activeDisposables.DisposeAllAndClear();
        }

        #endregion MonoBehaviour

        #region Widget

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (PlayerPrefs.GetInt(TutorialCheckKey, 0) == 0)
            {
                // Play Tutorial - for old user
                Game.Game.instance.Stage.TutorialController.Play(1510002);
                PlayerPrefs.SetInt(TutorialCheckKey, 1);
            }

            base.Show(ignoreShowAnimation);

            if (CurrentItemSubType == DefaultItemSubType)
            {
                // Show메서드 호출 시 DefaultItemSubType인 경우 UpdateItems가 호출되지 않아 강제로 호출
                UpdateGradeItems();
            }

            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Synthesis);
            CurrentItemSubType = DefaultItemSubType;
            foreach (var tapItem in synthesisTapGroup)
            {
                tapItem.tabButton.SetToggledOff();
            }
            synthesisTapGroup.First().tabButton.SetToggledOn();
        }

        #endregion Widget

        public void OnClickGradeItem(SynthesizeModel? model)
        {
            if (model == null)
            {
                NcDebug.LogError("model is null.");
                return;
            }

            var registrationPopup = Find<SynthesisRegistrationPopup>();
            System.Action showRegistrationPopup = () => registrationPopup.Show(model, RegisterItems);

            if (synthesisModule.PossibleSynthesis && !registrationPopup.HasModel(model))
            {
                Find<TwoButtonSystem>().Show(
                    L10nManager.Localize("UI_SYNTHESIZE_MATERIAL_CHANGE"),
                    L10nManager.Localize("UI_YES"),
                    L10nManager.Localize("UI_NO"),
                    showRegistrationPopup);
                return;
            }

            showRegistrationPopup.Invoke();
        }

        private void RegisterItems(IList<InventoryItem> items, SynthesizeModel model)
        {
            synthesisModule.UpdateData(items, model);
        }

        #region PrivateUtils

        private void CheckNull()
        {
            if (synthesisModule == null)
            {
                throw new NullReferenceException("activeBackgroundObject is null");
            }
        }

        private void UpdateInventory(Inventory inventory)
        {
            _cachedInventory = inventory;
            UpdateGradeItems();
        }

        private readonly Dictionary<Grade, int> _gradeItemCountDict = new ();
        /// <summary>
        /// When updating the items, inventory should be updated or change the item sub type.
        /// </summary>
        private void UpdateGradeItems()
        {
            if (_cachedInventory == null)
            {
                _cachedInventory = States.Instance.CurrentAvatarState.inventory;

                if (_cachedInventory == null)
                {
                    NcDebug.LogWarning($"[{nameof(Synthesis)} inventory is null");
                    return;
                }
            }

            var itemList = _cachedInventory.Items.Where(CheckInventoryItemSubType)
                                           .ToList();
            FillGradeItemCountDict(itemList);

            _gradeItems.Clear();
            foreach (var kvp in _gradeItemCountDict)
            {
                var grade = kvp.Key;
                var inventoryItemCount = kvp.Value;

                // TODO: 아이템 타입별로 숫자 달라짐
                var sheet = TableSheets.Instance.SynthesizeSheet;
                var row = sheet.Values.FirstOrDefault(row => row.GradeId == (int)grade);
                if (row == null)
                {
                    // 특정 grade에 대한 row가 없을 수 있음
                    continue;
                }

                if (!row.RequiredCountDict.ContainsKey(_currentItemSubType))
                {
                    // 특정 subType에 대한 value가 없을 수 있음
                    continue;
                }

                var requiredItemCount = row.RequiredCountDict[_currentItemSubType].RequiredCount;
                var model = new SynthesizeModel(grade, CurrentItemSubType, inventoryItemCount, requiredItemCount);
                _gradeItems.Add(model);
            }

            synthesisScroll.UpdateData(_gradeItems);
        }

        private bool CheckInventoryItemSubType(Inventory.Item item) => item.item.ItemSubType == CurrentItemSubType;

        private void FillGradeItemCountDict(List<Inventory.Item> itemList)
        {
            _gradeItemCountDict.Clear();
            foreach (var item in itemList)
            {
                if (!RestrictionHelper.CanUseAsSynthesizeMaterial(item.item.Id))
                {
                    continue;
                }

                if (_gradeItemCountDict.ContainsKey((Grade)item.item.Grade))
                {
                    _gradeItemCountDict[(Grade)item.item.Grade]++;
                }
                else
                {
                    _gradeItemCountDict[(Grade)item.item.Grade] = 1;
                }
            }
        }

        #endregion PrivateUtils

        #region Utils

        public static HashSet<(int, Grade)>? GetSynthesizeResultPool(Grade grade, ItemSubType itemSubType)
        {
            HashSet<(int, Grade)>? resultPool = null;
            switch (itemSubType)
            {
                case ItemSubType.Aura:
                case ItemSubType.Grimoire:
                    var equipmentItem = TableSheets.Instance.EquipmentItemSheet;
                    resultPool = SynthesizeSimulator.GetSynthesizeResultPool(
                        GetResultGrades(
                            grade,
                            itemSubType,
                            SynthesizeSimulator.GetUpgradeGrade(grade, itemSubType, equipmentItem)),
                        itemSubType,
                        equipmentItem);
                    break;
                case ItemSubType.FullCostume:
                case ItemSubType.Title:
                    var costumeItem = TableSheets.Instance.CostumeItemSheet;
                    resultPool = SynthesizeSimulator.GetSynthesizeResultPool(
                        GetResultGrades(
                            grade,
                            itemSubType,
                            SynthesizeSimulator.GetUpgradeGrade(grade, itemSubType, costumeItem)),
                        itemSubType,
                        costumeItem);
                    break;
            }

            if (resultPool == null)
            {
                NcDebug.LogError($"Failed to get SynthesizeResultPool for {grade} and {itemSubType}");
                return null;
            }

            // 뽑힐 수 있는 것만 남긴다. 판정은 체인이 쓰는 함수를 그대로 부른다 - 규칙을 여기에
            // 다시 적으면 SynthesizeWeightSheet.DefaultWeight 가 바뀔 때 또 어긋난다. 실제로
            // 기본값이 10000 에서 0 으로 뒤집힌 뒤, 미등재 아이템을 포함하던 옛 규칙이 남아
            // 절대 안 나오는 아이템을 결과 미리보기에 보여주고 있었다.
            var weightSheet = TableSheets.Instance.SynthesizeWeightSheet;
            var result = resultPool
                .Where(pair => SynthesizeSimulator.GetWeight(pair.Item1, weightSheet) > 0)
                .ToHashSet();

            // "미리보기가 비었다" 는 원인이 둘인데 증상이 같다 — 결과 등급을 잘못 골랐거나
            // (PLD-1587), 등급은 맞는데 weight 행이 없거나. 로그에서 갈라 둔다.
            if (resultPool.Count > 0 && result.Count == 0)
            {
                NcDebug.LogWarning(
                    $"[Synthesis] {grade}/{itemSubType} 결과 후보 {resultPool.Count}개가 모두" +
                    " weight 0 이라 비었습니다. SynthesizeWeightSheet 행을 확인하세요" +
                    " (체인에서도 합성이 실패합니다).");
            }

            return result;
        }

        /// <summary>
        /// 합성 결과로 나올 수 있는 등급들. 체인이 결과 등급을 가르는 두 조건을 그대로 따른다.
        /// </summary>
        /// <param name="grade">재료 등급.</param>
        /// <param name="itemSubType">재료 부위.</param>
        /// <param name="upgradeGrade">
        /// <see cref="SynthesizeSimulator.GetUpgradeGrade(Grade, ItemSubType, TableData.EquipmentItemSheet)"/>
        /// 결과. 상위 등급 행이 시트에 없으면 소스 등급이 그대로 돌아온다.
        /// </param>
        /// <remarks>
        /// 체인은 <c>succeed_rate</c> 로 성공/실패를 뽑고(실패면 소스 등급), 성공이어도 상위 등급
        /// 행이 없으면 소스 등급 풀로 되돌린다. 미리보기가 둘 중 하나라도 빠뜨리면 실제로 나올 수
        /// 없는 아이템을 보여주거나 화면이 빈다.
        /// <para>
        /// 실제로 등급 9에서 화면이 비었다(PLD-1587). lib9c 가 최상위 상한을 없애면서
        /// <c>GetTargetGrade</c> 가 시트에 없는 등급을 돌려주기 시작했는데, 미리보기는 그 값을
        /// 그대로 조회하고 폴백이 없었다. 상한을 되살리는 대신 호출자를 체인에 맞춘다 —
        /// 상한은 이제 시트가 정한다.
        /// </para>
        /// <para>
        /// <c>succeed_rate</c> 가 0 과 10000 사이면 두 등급이 다 나올 수 있으므로 둘 다 담는다.
        /// 지금 시트는 0 아니면 10000 뿐이라 눈에 보이는 변화는 없다.
        /// </para>
        /// </remarks>
        private static HashSet<Grade> GetResultGrades(
            Grade grade,
            ItemSubType itemSubType,
            Grade upgradeGrade)
        {
            var succeedRate = GetSucceedRate(grade, itemSubType);
            var grades = new HashSet<Grade>();

            if (succeedRate < SynthesizeSheet.SucceedRateMax)
            {
                // 실패할 수 있다 → 소스 등급이 그대로 결과가 된다.
                grades.Add(grade);
            }

            if (succeedRate > SynthesizeSheet.SucceedRateMin)
            {
                // 성공할 수 있다 → 상위 등급. 시트에 없으면 upgradeGrade 가 소스 등급이다.
                grades.Add(upgradeGrade);
            }

            return grades;
        }

        /// <summary>
        /// <c>SynthesizeSheet</c> 의 (등급, 부위) 성공률. 행이 없으면 0.
        /// </summary>
        /// <remarks>
        /// 행이 없으면 체인에서 합성 자체가 성립하지 않는다(액션이 던진다). 미리보기는
        /// 던질 자리가 아니므로 0(항상 실패 = 소스 등급)으로 보수적으로 둔다.
        /// </remarks>
        private static int GetSucceedRate(Grade grade, ItemSubType itemSubType)
        {
            // 키가 GradeId 라 TryGetValue 로 바로 찾는다. 인덱서는 행이 없으면 던진다.
            if (!TableSheets.Instance.SynthesizeSheet.TryGetValue((int)grade, out var row) ||
                !row.RequiredCountDict.TryGetValue(itemSubType, out var data))
            {
                return SynthesizeSheet.SucceedRateMin;
            }

            return data.SucceedRate;
        }

        public static void NotificationMaxSynthesisCount(int requiredItemCount)
        {
            var maxRequiredCount = requiredItemCount * MaxSynthesisCount;
            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize(
                    "UI_SYNTHESIZE_MAX_COUNT_CHECK",
                    maxRequiredCount,
                    MaxSynthesisCount),
                NotificationCell.NotificationType.Alert);
        }

        public static bool IsStrong(ItemBase itemBase)
        {
            if (itemBase is Equipment equipment)
            {
                return equipment.level > 0;
            }

            return false;
        }

        #endregion Utils

        public void CheckTutorial()
        {
            PlayerPrefs.SetInt(TutorialCheckKey, 1);
        }
    }
}
