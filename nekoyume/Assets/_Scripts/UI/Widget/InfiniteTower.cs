using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Libplanet.Crypto;
using Nekoyume.L10n;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using Nekoyume.ValueControlComponents.Shader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ease = DG.Tweening.Ease;

namespace Nekoyume.UI
{
    using UniRx;

    public class InfiniteTower : Widget
    {
        [SerializeField] private RectTransform towerRect; // 타워 스크롤 컨테이너
        [SerializeField] private float towerCenterAdjuster = 52;
        [SerializeField] private Ease towerMoveEase = Ease.OutCirc;
        [SerializeField] private InfiniteTowerFloorView[] floorViews; // 층 표시 뷰 배열
        [SerializeField] private TextMeshProUGUI remainingTimeText;
        [SerializeField] private ShaderPropertySlider remainingTimeSlider;

        // 도전 중 정보 (우측 상단)
        [SerializeField] private TextMeshProUGUI challengeInfoTitle;
        [SerializeField] private TextMeshProUGUI floorRewardText;
        [SerializeField] private BaseItemView[] rewardItemViews; // 클리어 보상 아이템 (최대 5개)

        // 클리어 정보 (우측 하단)
        [SerializeField] private TextMeshProUGUI clearInfoText;
        [SerializeField] private TextMeshProUGUI clearTitleText;
        [SerializeField] private TextMeshProUGUI clearCountText;
        [SerializeField] private ConditionalButton enterButton; // 도전 버튼

        // 전투/버프 조건 (좌측)
        [SerializeField] private InfiniteTowerConditionView battleConditionView;
        [SerializeField] private InfiniteTowerConditionView buffConditionView;

        // 닫기 버튼
        [SerializeField] private Button closeButton;

        private readonly List<IDisposable> _disposables = new ();
        private int _currentFloor;
        private InfiniteTowerFloorSheet.Row _currentFloorData;
        private List<InfiniteTowerBattleCondition> _battleConditions = new ();
        private List<InfiniteTowerCondition> _buffConditions = new ();

        private const float _floorHeight = 170f;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(OnClickClose);
            CloseWidget = OnClickClose;

            enterButton.OnClickSubject.Subscribe(_ => OnClickEnter()).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            ShowAsync(ignoreShowAnimation).Forget();
        }

        public async UniTask ShowAsync(bool ignoreShowAnimation = false)
        {
            try
            {
                // 로딩 화면 표시
                Find<LoadingScreen>().Show(LoadingScreen.LoadingType.InfiniteTower);

                // 타워 위치 초기화 (AdventureBoss와 동일)
                towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, 0);

                // 데이터 로드
                await LoadDataAsync();

                // UI 설정
                SetupUI();

                // 로딩 화면 닫기
                Find<LoadingScreen>().Close();

                // 위젯 표시
                base.Show(ignoreShowAnimation);

                // 헤더 메뉴 업데이트 (무한의 탑 티켓 표시)
                Find<HeaderMenuStatic>()?.UpdateAssets(HeaderMenuStatic.AssetVisibleState.InfiniteTower);

                // 위젯 표시 후 현재 층으로 포커스 이동
                // ChangeFloor는 배열 인덱스를 받으므로, 현재 층을 배열 인덱스로 변환
                var startFloor = Math.Max(1, _currentFloor - 15);
                var relativeIndex = _currentFloor - startFloor;
                // 인덱스를 1 더해서 정확한 위치로 조정
                ChangeFloor(relativeIndex + 1, false, false);

                // 완료 대기 (UI 애니메이션 등)
                await UniTask.Delay(100);
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                Find<LoadingScreen>().Close();
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("NOTIFICATION_INFINITE_TOWER_ERROR"),
                    NotificationCell.NotificationType.Alert);
            }
        }

        private async UniTask LoadDataAsync()
        {
            // InfiniteTowerInfo는 RxProps에서 관리되므로 여기서는 로드하지 않음
            InitializeData();
            await LoadClearCount();
        }

        private void SetupUI()
        {
            // UI 텍스트 설정
            challengeInfoTitle.text = L10nManager.Localize("UI_INFINITETOWER_FLOOR_INFO");
            floorRewardText.text = L10nManager.Localize("UI_INFINITETOWER_FLOOR_REWARD");
            clearInfoText.text = L10nManager.Localize("UI_INFINITETOWER_CLEAR_INFO");
            clearTitleText.text = L10nManager.Localize("UI_INFINITETOWER_CLEAR_TITLE");

            // UI 업데이트
            UpdateView();
        }

        private async Task  LoadClearCount()
        {
            if (clearCountText == null || _currentFloorData == null) return;

            try
            {
                var scheduleInfo = GetCurrentScheduleInfo();
                if (scheduleInfo == null)
                {
                    clearCountText.text = "0";
                    return;
                }


                // InfiniteTowerBoardState 주소 생성
                var seasonAddress = new Address($"{scheduleInfo.InfiniteTowerId:X40}");

                // State 조회
                var state = await Game.Game.instance.Agent.GetStateAsync(Addresses.InfiniteTowerBoard, seasonAddress);
                // InfiniteTowerBoardState 역직렬화
                if (state is Bencodex.Types.List serialized)
                {
                    var boardState = new InfiniteTowerBoardState(serialized);
                    var clearCount = boardState.GetFloorClearCount(_currentFloorData.Id);
                    clearCountText.text = clearCount.ToString("#,0");
                }
                else
                {
                    clearCountText.text = "0";
                }
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[InfiniteTower] Failed to get clear count: {e}");
                clearCountText.text = "0";
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _disposables.DisposeAllAndClear();
        }

        private void InitializeData()
        {
            // 현재 시즌 정보 조회
            var scheduleInfo = GetCurrentScheduleInfo();
            if (scheduleInfo == null)
            {
                NcDebug.LogError("[InfiniteTower] No active schedule found");
                return;
            }

            // 현재 도전 가능한 층 조회 (유저 상태 기반)
            _currentFloor = GetCurrentChallengeFloor();

            // 해당 층 데이터 로드
            var tableSheets = Game.Game.instance.TableSheets;
            if (tableSheets?.InfiniteTowerFloorSheet == null)
            {
                NcDebug.LogError("[InfiniteTower] InfiniteTowerFloorSheet is null");
                return;
            }

            _currentFloorData = tableSheets.InfiniteTowerFloorSheet.Values
                .FirstOrDefault(f => f.Floor == _currentFloor);

            if (_currentFloorData == null)
            {
                NcDebug.LogError($"[InfiniteTower] Floor data not found for floor {_currentFloor}");
                return;
            }

            // 층별 조건 조회
            LoadFloorConditions();
            LoadBattleConditions();
        }

        private void LoadFloorConditions()
        {
            _battleConditions.Clear();
            _buffConditions.Clear();
            var tableSheets = Game.Game.instance.TableSheets;

            if (tableSheets?.InfiniteTowerConditionSheet == null)
            {
                NcDebug.LogError("[InfiniteTower] InfiniteTowerConditionSheet is null");
                return;
            }

            // Guaranteed condition
            if (_currentFloorData.GuaranteedConditionId > 0)
            {
                var guaranteedCondition = tableSheets.InfiniteTowerConditionSheet.Values
                    .FirstOrDefault(c => c.Id == _currentFloorData.GuaranteedConditionId);

                if (guaranteedCondition != null)
                {
                    var condition = new InfiniteTowerCondition(guaranteedCondition);
                    CategorizeCondition(condition, true);
                }
            }

            // Random conditions (테스트용으로 최소 조건만 적용)
            var condtionWithWeights = _currentFloorData.GetRandomConditionsWithWeights();
            if (_currentFloorData.MinRandomConditions > 0 &&
                condtionWithWeights.Count > 0)
            {
                var availableConditions = tableSheets.InfiniteTowerConditionSheet.Values
                    .Where(c => condtionWithWeights.Select(t => t.conditionId).Contains(c.Id))
                    .Where(c =>
                        _currentFloorData.GuaranteedConditionId == 0 ||
                        c.Id != _currentFloorData.GuaranteedConditionId)
                    .ToList();

                for (int i = 0; i < availableConditions.Count; i++)
                {
                    var selectedCondition = availableConditions[i];
                    var condition = new InfiniteTowerCondition(selectedCondition);
                    CategorizeCondition(condition, false);
                }
            }
        }

        private void CategorizeCondition(InfiniteTowerCondition condition, bool isGuaranteed)
        {
            // 버프 조건: 스탯 수정자 조건들
            _buffConditions.Add(condition);
        }

        private void LoadBattleConditions()
        {
            _battleConditions.Clear();
            if (_currentFloorData == null) return;

            // GetBattleConditions() 메서드 사용
            var battleConditions = _currentFloorData.GetBattleConditions();
            _battleConditions.AddRange(battleConditions);
        }

        private void UpdateView()
        {
            UpdateSeasonInfo();
            UpdateFloorViews();
            UpdateRewards();
            UpdateConditions();
            UpdateEnterButton();
        }

        private void UpdateSeasonInfo()
        {
            var scheduleInfo = GetCurrentScheduleInfo();
            if (scheduleInfo != null)
            {
                var currentBlock = Game.Game.instance.Agent.BlockIndex;
                var remainingBlocks = scheduleInfo.EndBlockIndex - currentBlock;
                remainingTimeText.text =
                    $"{remainingBlocks:#,0}({remainingBlocks.BlockRangeToTimeSpanString()})";

                if (remainingTimeSlider != null)
                {
                    var progress = Mathf.InverseLerp(scheduleInfo.StartBlockIndex,
                        scheduleInfo.EndBlockIndex, currentBlock);
                    remainingTimeSlider.NormalizedValue = progress;
                }
            }
        }

        private void UpdateFloorViews()
        {
            if (floorViews == null) return;

            var scheduleInfo = GetCurrentScheduleInfo();
            if (scheduleInfo == null) return;

            // 현재 층을 중심으로 위아래 15층씩 총 30개 층 표시
            var startFloor = Math.Max(1, _currentFloor - 15);
            var endFloor = _currentFloor + 14;

            for (int i = 0; i < floorViews.Length; i++)
            {
                var floorView = floorViews[i];
                if (floorView == null) continue;

                var floorNumber = startFloor + i;

                // 범위를 벗어나면 비활성화
                if (floorNumber > endFloor)
                {
                    floorView.gameObject.SetActive(false);
                    continue;
                }

                floorView.gameObject.SetActive(true);
                var floorState = GetFloorState(floorNumber);

                // 해당 층의 데이터 조회
                var tableSheets = Game.Game.instance.TableSheets;
                var floorData = tableSheets?.InfiniteTowerFloorSheet?.Values
                    .FirstOrDefault(f => f.Floor == floorNumber);

                floorView.SetState(floorState, floorNumber, floorData);
            }

            // 현재 층을 중심으로 화면 조정 (AdventureBoss와 동일한 방식)
            AdjustTowerPosition(_currentFloor, startFloor);
        }

        public void ChangeFloor(int targetIndex, bool isStartPointRefresh = true,
            bool isAnimation = true)
        {
            var targetCenter = targetIndex * _floorHeight + _floorHeight / 2;
            var startY = -(targetCenter - MainCanvas.instance.RectTransform.rect.height / 2 -
                towerCenterAdjuster);

            if (isAnimation)
            {
                if (isStartPointRefresh)
                {
                    towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, 0);
                }

                towerRect.DoAnchoredMoveY(Math.Min(startY, 0), 0.35f).SetEase(towerMoveEase);
            }
            else
            {
                towerRect.anchoredPosition =
                    new Vector2(towerRect.anchoredPosition.x, Math.Min(startY, 0));
            }
        }

        private void AdjustTowerPosition(int targetFloor, int startFloor)
        {
            if (towerRect == null || floorViews == null || floorViews.Length == 0) return;

            const float floorHeight = 170f; // AdventureBoss와 동일한 층 높이
            const float towerCenterAdjuster = 52f; // AdventureBoss와 동일한 조정값

            // MainCanvas의 높이 사용 (AdventureBoss와 동일)
            var screenHeight = MainCanvas.instance.RectTransform.rect.height;

            // AdventureBoss와 동일한 방식: 배열 인덱스를 기반으로 위치 계산
            // 시작 층과의 차이를 계산하여 배열 인덱스 결정
            // 예: targetFloor=21, startFloor=6이면 relativeIndex=15, floorViews[15]가 21층
            var relativeIndex = targetFloor - startFloor;

            // 첫 번째 층의 실제 위치를 확인하여 offset 계산
            // anchoredPosition.y는 pivot 기준 위치이므로, pivot이 중심이 아닐 수 있음
            // rectTransform.rect.height를 사용하여 층의 중심 위치 계산
            float firstFloorOffset = 0f;
            if (floorViews[0] != null && floorViews[0].gameObject.activeSelf)
            {
                var firstFloorRect = floorViews[0].GetComponent<RectTransform>();
                if (firstFloorRect != null)
                {
                    var firstFloorActualY = firstFloorRect.anchoredPosition.y;
                    // pivot이 중심이 아닐 수 있으므로, rect.height를 사용하여 중심 위치 계산
                    var pivotOffset = (firstFloorRect.pivot.y - 0.5f) * firstFloorRect.rect.height;
                    var firstFloorCenterY = firstFloorActualY - pivotOffset;
                    var firstFloorExpectedY = 0 * floorHeight + floorHeight / 2; // 85
                    firstFloorOffset = firstFloorCenterY - firstFloorExpectedY;
                }
            }

            // AdventureBoss와 동일한 계산 방식 사용 + 첫 번째 층의 offset 적용
            // targetCenter = relativeIndex * floorHeight + floorHeight / 2 + firstFloorOffset
            // 이는 타워 좌표계에서 해당 층의 중심 Y 위치를 계산합니다
            var targetCenter = relativeIndex * floorHeight + floorHeight / 2 + firstFloorOffset;

            // AdventureBoss와 정확히 동일한 계산식 사용
            // startY = -(targetCenter - screenHeight / 2 - towerCenterAdjuster)
            // 이는 타워를 아래로 이동시켜서 현재 층이 화면 중앙에 오도록 함
            var startY = -(targetCenter - screenHeight / 2 - towerCenterAdjuster);

            // 화면 상단을 넘지 않도록 제한 (타워가 위로 올라가지 않도록)
            var clampedY = Math.Min(startY, 0);

            towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, clampedY);
        }

        private InfiniteTowerFloorView.FloorState GetFloorState(int floorNumber)
        {
            // InfiniteTowerInfo는 RxProps에서 관리
            var infiniteTowerInfo = RxProps.InfiniteTowerInfo.Value;
            if (infiniteTowerInfo == null)
            {
                return floorNumber == _currentFloor
                    ? InfiniteTowerFloorView.FloorState.Current
                    : InfiniteTowerFloorView.FloorState.Locked;
            }

            var clearedFloor = infiniteTowerInfo.ClearedFloor;

            if (floorNumber <= clearedFloor)
            {
                return InfiniteTowerFloorView.FloorState.Cleared;
            }
            else if (floorNumber == clearedFloor + 1)
            {
                return InfiniteTowerFloorView.FloorState.Current;
            }
            else
            {
                return InfiniteTowerFloorView.FloorState.Locked;
            }
        }

        private void UpdateRewards()
        {
            if (rewardItemViews == null || _currentFloorData == null) return;

            // 모든 보상 슬롯 비활성화
            foreach (var rewardView in rewardItemViews)
            {
                if (rewardView != null)
                {
                    rewardView.gameObject.SetActive(false);
                }
            }

            // 보상 데이터 수집
            var itemRewards = _currentFloorData.GetItemRewards();
            var fungibleAssetRewards = _currentFloorData.GetFungibleAssetRewards();

            // 보상 표시 (최대 5개)
            var rewardIndex = 0;

            // Item rewards 표시
            foreach (var (itemId, count) in itemRewards)
            {
                if (rewardIndex >= rewardItemViews.Length) break;

                var rewardView = rewardItemViews[rewardIndex];
                if (rewardView != null)
                {
                    rewardView.ItemViewSetItemData(itemId, count);
                    rewardView.gameObject.SetActive(true);
                    rewardIndex++;
                }
            }

            // Fungible Asset rewards 표시
            foreach (var (ticker, amount) in fungibleAssetRewards)
            {
                if (rewardIndex >= rewardItemViews.Length) break;

                var rewardView = rewardItemViews[rewardIndex];
                if (rewardView != null)
                {
                    rewardView.ItemViewSetCurrencyData(ticker, amount);
                    rewardView.gameObject.SetActive(true);
                    rewardIndex++;
                }
            }

        }

        private void UpdateConditions()
        {
            if (battleConditionView != null)
            {
                battleConditionView.SetTitle(
                    L10nManager.Localize("UI_INFINITETOWER_BATTLE_CONDITION"));
                battleConditionView.SetConditions(_battleConditions,
                    new List<InfiniteTowerCondition>(), _currentFloorData);
            }

            if (buffConditionView != null)
            {
                buffConditionView.SetTitle(L10nManager.Localize("UI_INFINITETOWER_BUFF_CONDITION"));
                buffConditionView.SetConditions(new List<InfiniteTowerBattleCondition>(),
                    _buffConditions, _currentFloorData);
            }
        }

        private void UpdateEnterButton()
        {
            if (enterButton != null)
            {
                enterButton.SetText(L10nManager.Localize("UI_CHALLENGE"));
                enterButton.Interactable = true;
            }
        }


        private void OnClickEnter()
        {
            if (_currentFloorData == null)
            {
                NcDebug.LogError("[InfiniteTower] Current floor data is null");
                return;
            }

            var scheduleInfo = GetCurrentScheduleInfo();
            if (scheduleInfo == null)
            {
                NcDebug.LogError("[InfiniteTower] No active schedule found");
                return;
            }

            // 전투 준비 화면 표시
            var preparation = Find<InfiniteTowerPreparation>();
            if (preparation != null)
            {
                preparation.Show(
                    L10nManager.Localize("UI_CLOSE"),
                    _currentFloorData.NcgCost ?? 0,
                    _currentFloorData,
                    _battleConditions,
                    _buffConditions,
                    scheduleInfo.InfiniteTowerId,
                    _currentFloorData.Id
                );
            }
            else
            {
                NcDebug.LogError("[InfiniteTower] InfiniteTowerPreparation not found");
            }
        }

        /// <summary>
        /// 특정 FloorId로 preparation 화면을 엽니다.
        /// InfiniteTowerResultPopup에서 재진입 시 사용됩니다.
        /// </summary>
        /// <param name="floorId">열고자 하는 층의 ID</param>
        public void ShowPreparationForFloor(int floorId)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            if (tableSheets?.InfiniteTowerFloorSheet == null)
            {
                NcDebug.LogError("[InfiniteTower] InfiniteTowerFloorSheet is null");
                return;
            }

            // FloorId로 floorData 조회
            if (!tableSheets.InfiniteTowerFloorSheet.TryGetValue(floorId, out var floorData))
            {
                NcDebug.LogError($"[InfiniteTower] Floor data not found for floorId {floorId}");
                return;
            }

            // 현재 floor 정보 업데이트
            _currentFloor = floorData.Floor;
            _currentFloorData = floorData;

            // 층별 조건 로드
            LoadFloorConditions();
            LoadBattleConditions();

            // OnClickEnter와 동일한 로직으로 preparation 열기
            var scheduleInfo = GetCurrentScheduleInfo();
            if (scheduleInfo == null)
            {
                NcDebug.LogError("[InfiniteTower] No active schedule found");
                return;
            }

            var preparation = Find<InfiniteTowerPreparation>();
            if (preparation != null)
            {
                preparation.Show(
                    L10nManager.Localize("UI_BACK"),
                    _currentFloorData.NcgCost ?? 0,
                    _currentFloorData,
                    _battleConditions,
                    _buffConditions,
                    scheduleInfo.InfiniteTowerId,
                    _currentFloorData.Id
                );
            }
            else
            {
                NcDebug.LogError("[InfiniteTower] InfiniteTowerPreparation not found");
            }
        }

        private void OnClickClose()
        {
            Close();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            // 헤더 메뉴를 기본 상태로 복원
            Find<HeaderMenuStatic>()?.UpdateAssets(HeaderMenuStatic.AssetVisibleState.Main);
            base.Close(ignoreCloseAnimation);
        }

        private InfiniteTowerScheduleSheet.Row GetCurrentScheduleInfo()
        {
            return RxProps.InfiniteTowerScheduleRow.Value;
        }

        private int GetCurrentChallengeFloor()
        {
            // InfiniteTowerInfo는 RxProps에서 관리
            var infiniteTowerInfo = RxProps.InfiniteTowerInfo.Value;
            if (infiniteTowerInfo == null)
            {
                NcDebug.LogWarning("[InfiniteTower] InfiniteTowerInfo is null, defaulting to floor 1");
                return 1;
            }

            // 클리어한 층이 있으면 다음 층, 없으면 1층부터 시작
            var currentFloor = infiniteTowerInfo.ClearedFloor + 1;
            return currentFloor;
        }
    }
}
