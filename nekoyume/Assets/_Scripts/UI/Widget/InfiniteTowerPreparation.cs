using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using mixpanel;
using Nekoyume.Battle;
using Nekoyume.Blockchain;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
using TMPro;
using UnityEngine.UI;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using Nekoyume.Action;
using Nekoyume.UI.Model;
using Libplanet.Types.Assets;
using Lib9c;

namespace Nekoyume.UI
{
    using UniRx;

    public class InfiniteTowerPreparation : Widget
    {
        [SerializeField]
        private AvatarInformation information;

        [SerializeField]
        private TextMeshProUGUI closeButtonText;

        [SerializeField]
        private ParticleSystem[] particles;

        [SerializeField]
        private ConditionalCostButton startButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Transform buttonStarImageTransform;

        [SerializeField][Range(.5f, 3.0f)]
        private float animationTime = 1f;

        [SerializeField]
        private bool moveToLeft = false;

        [SerializeField][Range(0f, 10f)][Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        [SerializeField]
        private GameObject coverToBlockClick = null;

        [SerializeField]
        private GameObject hasBg;

        [SerializeField]
        private GameObject blockStartingTextObject;

        // 무한의 탑 전용 컴포넌트
        [SerializeField]
        private InfiniteTowerConditionView battleConditionView;

        [SerializeField]
        private InfiniteTowerConditionView buffConditionView;

        [SerializeField]
        private TextMeshProUGUI floorInfoText;

        // 캐릭터 모델 비활성화용
        [SerializeField]
        private GameObject characterModelObject;

        // 로비 캐릭터 비활성화용
        private bool _wasLobbyCharacterActive;

        private long _requiredCost;
        private InfiniteTowerFloorSheet.Row _floorData;
        private List<InfiniteTowerBattleCondition> _battleConditions = new();
        private List<InfiniteTowerCondition> _buffConditions = new();
        private int _infiniteTowerId;
        private int _floorId;

        private readonly List<IDisposable> _disposables = new();

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent &&
            startButton.Interactable;

        #region override

        protected override void Awake()
        {
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                AudioController.PlayClick();
            });

            CloseWidget = () => Close(true);
            base.Awake();

            BattleRenderer.Instance.OnPrepareStage += GoToPrepareStage;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            BattleRenderer.Instance.OnPrepareStage -= GoToPrepareStage;
        }

        public override void Initialize()
        {
            base.Initialize();

            information.Initialize();

            startButton.OnSubmitSubject
                .Where(_ => !BattleRenderer.Instance.IsOnBattle)
                .ThrottleFirst(TimeSpan.FromSeconds(1f))
                .Subscribe(_ => OnClickBattle())
                .AddTo(gameObject);
        }

        public void Show(
            string closeButtonName,
            long requiredCost,
            InfiniteTowerFloorSheet.Row floorData,
            List<InfiniteTowerBattleCondition> battleConditions,
            List<InfiniteTowerCondition> buffConditions,
            int infiniteTowerId,
            int floorId,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            // 헤더 메뉴 업데이트 (무한의 탑 티켓 표시)
            Find<HeaderMenuStatic>()?.UpdateAssets(HeaderMenuStatic.AssetVisibleState.InfiniteTower);

            _requiredCost = requiredCost;
            _floorData = floorData;
            _battleConditions = battleConditions ?? new List<InfiniteTowerBattleCondition>();
            _buffConditions = buffConditions ?? new List<InfiniteTowerCondition>();
            _infiniteTowerId = infiniteTowerId;
            _floorId = floorId;

            Analyzer.Instance.Track("Unity/Click InfiniteTower Preparation", new Dictionary<string, Value>()
            {
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                ["InfiniteTowerId"] = infiniteTowerId,
                ["FloorId"] = floorId
            });

            // 인벤토리 업데이트 (장비는 InfiniteTower, 룬은 Adventure, 캐릭터 모델 비활성화)
            information.SetFloorData(_floorData);
            information.UpdateInventoryForInfiniteTower(BattleType.InfiniteTower);
            UpdateRequiredCostByFloorId();

            closeButtonText.text = closeButtonName;

            startButton.gameObject.SetActive(true);
            startButton.Interactable = true;
            coverToBlockClick.SetActive(false);

            // 층 정보 표시
            if (floorInfoText != null && _floorData != null)
            {
                floorInfoText.text = L10nManager.Localize("UI_INFINITETOWER_FLOOR_NUMBER", _floorData.Floor);
            }

            // 조건 정보 표시
            UpdateConditions();

            // 금지된 장비/코스튬 자동 해제
            UnequipItemsFromForbiddenSlots();

            // 룬 슬롯 잠금 처리 (ForbiddenRuneTypes 확인)
            UpdateRuneSlotsLock();

            // 캐릭터 모델 비활성화
            if (characterModelObject != null)
            {
                characterModelObject.SetActive(false);
            }

            // 로비 캐릭터 비활성화
            var lobbyCharacter = Game.Game.instance.Lobby.Character;
            if (lobbyCharacter != null)
            {
                _wasLobbyCharacterActive = lobbyCharacter.gameObject.activeInHierarchy;
                lobbyCharacter.gameObject.SetActive(false);
            }

            // 인벤토리 변경 시 버튼 업데이트
            ReactiveAvatarState.Inventory.Subscribe(_ => UpdateStartButton()).AddTo(_disposables);

            // InfiniteTowerInfo 구독 (RxProps에서 관리)
            RxProps.InfiniteTowerInfo
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    UpdateTicketInfo();
                    UpdateStartButton();
                })
                .AddTo(_disposables);

            // 초기 밸리데이션 수행
            UpdateStartButton();

            // 장착된 아이템 변경을 위한 인벤토리 설정 (밸리데이션 후)
            UpdateInventory();

            // 튜토리얼 타겟 설정
            if (information.TryGetCellByIndex(0, out var firstCell))
            {
                Game.Game.instance.Stage.TutorialController.SetTutorialTarget(new TutorialTarget
                {
                    type = TutorialTargetType.InventoryFirstCell,
                    rectTransform = (RectTransform)firstCell.transform
                });
            }

            if (information.TryGetCellByIndex(1, out var secondCell))
            {
                Game.Game.instance.Stage.TutorialController.SetTutorialTarget(new TutorialTarget
                {
                    type = TutorialTargetType.InventorySecondCell,
                    rectTransform = (RectTransform)secondCell.transform
                });
            }
        }

        public void UpdateInventory()
        {
            NcDebug.Log("[InfiniteTowerPreparation] UpdateInventory called");
            information.UpdateInventoryForInfiniteTower(BattleType.InfiniteTower, null, () => {
                NcDebug.Log("[InfiniteTowerPreparation] Item equipped/unequipped - calling UpdateStartButton");
                UpdateStartButton();
            }, () => {
                NcDebug.Log("[InfiniteTowerPreparation] Stat updated - calling UpdateStartButton");
                UpdateStartButton();
            });

            // 초기 진입 시 현재 밸리데이션 상태를 인벤토리에 반영
            NcDebug.Log("[InfiniteTowerPreparation] Performing initial validation for inventory dimming");
            var isValid = ValidateBattleConditions(
                out var invalidEquipmentIds,
                out var invalidCostumeIds,
                out var invalidRuneSlotIndices,
                out var isCpInvalid);

            NcDebug.Log($"[InfiniteTowerPreparation] Initial validation result: isValid={isValid}, " +
                       $"invalidEquipmentIds={invalidEquipmentIds.Count}, " +
                       $"invalidCostumeIds={invalidCostumeIds.Count}");

            // 밸리데이션 결과를 인벤토리에 즉시 적용
            UpdateInventoryDim(invalidEquipmentIds, invalidCostumeIds);
        }

        public void UpdateInventoryView()
        {
            information.UpdateInventoryForInfiniteTower(BattleType.InfiniteTower, null, () => {
                NcDebug.Log("[InfiniteTowerPreparation] Item equipped/unequipped in UpdateInventoryView - calling UpdateStartButton");
                UpdateStartButton();
            }, () => {
                NcDebug.Log("[InfiniteTowerPreparation] Stat updated in UpdateInventoryView - calling UpdateStartButton");
                UpdateStartButton();
            });
            information.UpdateViewForInfiniteTower(BattleType.InfiniteTower);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            // 캐릭터 모델 다시 활성화
            if (characterModelObject != null)
            {
                characterModelObject.SetActive(true);
            }

            // 로비 캐릭터 다시 활성화
            var lobbyCharacter = Game.Game.instance.Lobby.Character;
            if (lobbyCharacter != null && _wasLobbyCharacterActive)
            {
                lobbyCharacter.gameObject.SetActive(true);
            }

            // 헤더 메뉴를 무한의 탑 상태로 유지 (InfiniteTower 위젯이 열려있을 수 있음)
            // InfiniteTower 위젯이 닫힐 때만 Main으로 복원
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void UpdateTicketInfo()
        {
            // HeaderMenuStatic에 티켓 정보 업데이트 요청
            // TODO: InfiniteTowerTickets Currency 모듈이 추가되면 여기서 업데이트
            // 현재는 RxProps.InfiniteTowerTicketProgress를 사용
            var ticketProgress = RxProps.InfiniteTowerTicketProgress.Value;
            if (ticketProgress != null)
            {
                NcDebug.Log($"[InfiniteTowerPreparation] Ticket info updated - CurrentTickets: {ticketProgress.currentTickets}");
            }
        }

        private void UpdateRequiredCostByFloorId()
        {
            // 무한의 탑은 티켓을 사용 (이벤트던전과 동일)
            startButton.SetCost(CostType.InfiniteTowerTicket, 1);
        }

        private void UpdateConditions()
        {
            if (battleConditionView != null)
            {
                battleConditionView.SetTitle(
                    L10nManager.Localize("UI_INFINITETOWER_BATTLE_CONDITION"));
                battleConditionView.SetConditions(_battleConditions,
                    new List<InfiniteTowerCondition>(), _floorData);
            }

            if (buffConditionView != null)
            {
                buffConditionView.SetTitle(L10nManager.Localize("UI_INFINITETOWER_BUFF_CONDITION"));
                buffConditionView.SetConditions(new List<InfiniteTowerBattleCondition>(),
                    _buffConditions, _floorData);
            }
        }

        private void OnClickBattle()
        {
            AudioController.PlayClick();

            if (BattleRenderer.Instance.IsOnBattle)
            {
                return;
            }

            // 티켓 체크 (RxProps에서 관리 - 자동 리필 계산 반영된 값 사용)
            var ticketProgress = RxProps.InfiniteTowerTicketProgress.Value;
            if (ticketProgress == null)
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_INFINITETOWER_INFO_NOT_LOADED"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            // InfiniteTowerInfo는 ShowTicketPurchasePopup()에서 NumberOfTicketPurchases를 위해 필요
            var infiniteTowerInfo = RxProps.InfiniteTowerInfo.Value;
            if (infiniteTowerInfo == null)
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_INFINITETOWER_INFO_NOT_LOADED"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            var hasTicket = infiniteTowerInfo.RemainingTickets >= 1;
            if (hasTicket)
            {
                // 티켓이 충분한 경우 바로 배틀 시작 (buyTicketIfNeeded = false)
                StartCoroutine(CoBattleStart(false, false));
            }
            else
            {
                // 티켓이 부족한 경우 구매 팝업 표시
                ShowTicketPurchasePopup();
            }

            coverToBlockClick.SetActive(true);
        }

        private IEnumerator CoBattleStart(bool buyTicketIfNeeded = false, bool useNcgForTicket = false)
        {
            var game = Game.Game.instance;
            game.Stage.IsShowHud = true;
            BattleRenderer.Instance.IsOnBattle = true;

            var headerMenuStatic = Find<HeaderMenuStatic>();

            var currencyImage = headerMenuStatic.InfiniteTowerTickets.IconImage;
            if (buyTicketIfNeeded)
            {
                currencyImage = headerMenuStatic.Gold.IconImage;
            }
            var itemMoveAnimation = ItemMoveAnimation.Show(
                currencyImage.sprite,
                currencyImage.transform.position,
                buttonStarImageTransform.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
            yield return new WaitWhile(() => itemMoveAnimation.IsPlaying);

            InfiniteTowerBattleAction(buyTicketIfNeeded, useNcgForTicket);
        }

        private void InfiniteTowerBattleAction(bool buyTicketIfNeeded = false, bool useNcgForTicket = false)
        {
            Find<WorldMap>().Close(true);
            Find<InfiniteTower>().Close(true);
            Find<LoadingScreen>().Show(LoadingScreen.LoadingType.InfiniteTower);
            startButton.gameObject.SetActive(false);

            // InfiniteTower BattleType으로 장비/코스튬 가져오기
            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.InfiniteTower];
            var costumes = itemSlotState.Costumes;
            var equipments = itemSlotState.Equipments;

            // Adventure BattleType으로 룬 가져오기 (사용자 요구사항)
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Adventure]
                .GetEquippedRuneSlotInfos();

            var consumables = information.GetEquippedConsumables().Select(x => x.ItemId).ToList();

            var stage = Game.Game.instance.Stage;
            stage.IsExitReserved = false;
            stage.foodCount = consumables.Count;
            ActionRenderHandler.Instance.Pending = true;

            ActionManager.Instance.InfiniteTowerBattle(
                equipments,
                costumes,
                consumables,
                runeInfos,
                _infiniteTowerId,
                _floorId,
                buyTicketIfNeeded,
                useNcgForTicket)
                .Subscribe(
                    _ =>
                    {
                        // Action render handler will process the battle
                    },
                    error =>
                    {
                        Find<LoadingScreen>().Close();
                        NcDebug.LogError($"InfiniteTowerBattle action failed: {error}");
                    })
                .AddTo(gameObject);
        }

        private void GoToPrepareStage(BattleLog battleLog)
        {
            if (!IsActive() || !Find<LoadingScreen>().IsActive())
            {
                return;
            }

            StartCoroutine(CoGoToStage(battleLog));
        }

        private IEnumerator CoGoToStage(BattleLog battleLog)
        {
            yield return BattleRenderer.Instance.LoadStageResources(battleLog);

            Find<LoadingScreen>().Close();
            Close(true);
        }

        public void UpdateStartButton()
        {
            NcDebug.Log("[InfiniteTowerPreparation] UpdateStartButton called");

            startButton.UpdateObjects();
            foreach (var particle in particles)
            {
                if (startButton.IsSubmittable)
                {
                    particle.Play();
                }
                else
                {
                    particle.Stop();
                }
            }

            // 배틀 조건 밸리데이션 수행
            var isValid = ValidateBattleConditions(
                out var invalidEquipmentIds,
                out var invalidCostumeIds,
                out var invalidRuneSlotIndices,
                out var isCpInvalid);

            // 디버그 로그 추가
            NcDebug.Log($"[InfiniteTowerPreparation] Validation result: isValid={isValid}, " +
                       $"invalidEquipmentIds={invalidEquipmentIds.Count}, " +
                       $"invalidCostumeIds={invalidCostumeIds.Count}, " +
                       $"invalidRuneSlotIndices={invalidRuneSlotIndices.Count}, " +
                       $"isCpInvalid={isCpInvalid}");

            // 현재 CP와 제한값 상세 로그
            if (_floorData != null)
            {
                var currentCp = Util.TotalCP(BattleType.InfiniteTower);
                NcDebug.Log($"[InfiniteTowerPreparation] Current CP: {currentCp}, " +
                           $"Required CP: {_floorData.RequiredCp}, Max CP: {_floorData.MaxCp}");
            }

            // InfiniteTower BattleType으로 장비 확인
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.InfiniteTower);
            var consumables = information.GetEquippedConsumables().Select(x => x.Id).ToList();
            var canBattle = Util.CanBattle(equipments, costumes, consumables);

            if (isValid && canBattle)
            {
                // 밸리데이션 통과 시
                startButton.gameObject.SetActive(true);
                blockStartingTextObject.SetActive(false);
                ClearSlotsDim();
                information.ResetCpValidationState();
                information.SetCpColor(Color.white);
                NcDebug.Log("[InfiniteTowerPreparation] Validation passed - button enabled, CP color set to white");
            }
            else
            {
                // 밸리데이션 실패 시
                startButton.gameObject.SetActive(false);
                blockStartingTextObject.SetActive(true);
                UpdateSlotsDim(invalidEquipmentIds, invalidCostumeIds, invalidRuneSlotIndices);

                // CP 밸리데이션 실패 시 붉은색으로 표시
                if (isCpInvalid)
                {
                    var redColor = Palette.GetColor(ColorType.TextDenial);
                    information.SetCpColor(redColor);
                    NcDebug.Log($"[InfiniteTowerPreparation] CP validation failed - setting red color: {redColor}");
                }
                else
                {
                    information.SetCpColor(Color.white);
                    NcDebug.Log("[InfiniteTowerPreparation] CP validation passed - setting white color");
                }

                NcDebug.Log("[InfiniteTowerPreparation] Validation failed - button disabled");
            }
        }

        public void TutorialActionClickBattlePreparationFirstInventoryCellView()
        {
            try
            {
                if (information.TryGetFirstCell(out var item))
                {
                    item.Selected.Value = true;
                }
                else
                {
                    NcDebug.LogError($"TutorialActionClickBattlePreparationFirstInventoryCellView() throw error.");
                }

                Find<EquipmentTooltip>().OnEnterButtonArea(true);
            }
            catch
            {
                NcDebug.LogError($"TryGetFirstCell throw error.");
            }
        }

        public void TutorialActionClickBattlePreparationSecondInventoryCellView()
        {
            try
            {
                var itemCell = information.GetBestEquipmentInventoryItems();
                if (itemCell is null)
                {
                    NcDebug.LogError($"information.GetBestEquipmentInventoryItems().ElementAtOrDefault(0) is null");
                    return;
                }

                itemCell.Selected.Value = true;
                Find<EquipmentTooltip>().OnEnterButtonArea(true);
            }
            catch
            {
                NcDebug.LogError($"GetSecondCell throw error.");
            }
        }

        public void TutorialActionClickBattlePreparationHackAndSlash()
        {
            OnClickBattle();
        }

        private bool ValidateBattleConditions(
            out List<Guid> invalidEquipmentIds,
            out List<Guid> invalidCostumeIds,
            out List<int> invalidRuneSlotIndices,
            out bool isCpInvalid)
        {
            invalidEquipmentIds = new List<Guid>();
            invalidCostumeIds = new List<Guid>();
            invalidRuneSlotIndices = new List<int>();
            isCpInvalid = false;

            if (_floorData == null)
            {
                return true;
            }

            try
            {
                // 현재 장착된 장비/코스튬 가져오기
                var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.InfiniteTower);

                // 현재 장착된 룬 정보 가져오기
                var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Adventure]
                    .GetEquippedRuneSlotInfos();

                // CP 계산
                var currentCp = Util.TotalCP(BattleType.InfiniteTower);

                // 1. 장비/코스튬 제한 검증
                try
                {
                    _floorData.ValidateFloorRestrictions(equipments, costumes);
                }
                catch (Exception ex)
                {
                    NcDebug.Log($"[InfiniteTowerPreparation] Floor restrictions validation failed: {ex.Message}");

                    // 위반된 아이템들을 식별
                    foreach (var equipment in equipments)
                    {
                        try
                        {
                            var testList = new List<Equipment> { equipment };
                            _floorData.ValidateItemTypeRestrictions(testList);
                            _floorData.ValidateItemGradeRestrictions(testList);
                            _floorData.ValidateItemLevelRestrictions(testList);
                        }
                        catch (Exception itemEx)
                        {
                            NcDebug.Log($"[InfiniteTowerPreparation] Equipment {equipment.ItemId} failed validation: {itemEx.Message}");
                            invalidEquipmentIds.Add(equipment.ItemId);
                        }
                    }

                    foreach (var costume in costumes)
                    {
                        try
                        {
                            var testList = new List<Costume> { costume };
                            _floorData.ValidateItemTypeRestrictions(testList);
                            _floorData.ValidateItemGradeRestrictions(testList);
                            _floorData.ValidateItemLevelRestrictions(testList);
                        }
                        catch (Exception itemEx)
                        {
                            NcDebug.Log($"[InfiniteTowerPreparation] Costume {costume.ItemId} failed validation: {itemEx.Message}");
                            invalidCostumeIds.Add(costume.ItemId);
                        }
                    }
                }

                // 2. 룬 타입 제한 검증
                try
                {
                    var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
                    _floorData.ValidateRuneTypes(runeInfos, runeListSheet);
                }
                catch (Exception)
                {
                    // 위반된 룬 슬롯 식별
                    var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
                    foreach (var runeInfo in runeInfos)
                    {
                        try
                        {
                            var testRuneInfos = new List<RuneSlotInfo> { runeInfo };
                            _floorData.ValidateRuneTypes(testRuneInfos, runeListSheet);
                        }
                        catch
                        {
                            invalidRuneSlotIndices.Add(runeInfo.SlotIndex);
                        }
                    }
                }

                // 3. CP 요구사항 검증
                try
                {
                    _floorData.ValidateCpRequirements(currentCp);
                    NcDebug.Log($"[InfiniteTowerPreparation] CP validation passed: {currentCp}");
                }
                catch (Exception ex)
                {
                    isCpInvalid = true;
                    NcDebug.Log($"[InfiniteTowerPreparation] CP validation failed: {currentCp}, error: {ex.Message}");
                }

                // 모든 검증이 통과했는지 확인
                return invalidEquipmentIds.Count == 0 &&
                       invalidCostumeIds.Count == 0 &&
                       invalidRuneSlotIndices.Count == 0 &&
                       !isCpInvalid;
            }
            catch (Exception ex)
            {
                NcDebug.LogError($"[InfiniteTowerPreparation] Validation error: {ex.Message}");
                return false;
            }
        }

        private void UpdateSlotsDim(List<Guid> invalidEquipmentIds, List<Guid> invalidCostumeIds, List<int> invalidRuneSlotIndices)
        {
            // 장비/코스튬 슬롯 딤 처리
            information.SetEquipmentSlotsDim(invalidEquipmentIds, invalidCostumeIds);

            // 룬 슬롯 딤 처리
            information.SetRuneSlotsDim(invalidRuneSlotIndices);

            // 인벤토리 아이템 딤 처리
            UpdateInventoryDim(invalidEquipmentIds, invalidCostumeIds);
        }

        private void ClearSlotsDim()
        {
            // 모든 슬롯 딤 처리 해제
            information.SetEquipmentSlotsDim(new List<Guid>(), new List<Guid>());
            information.SetRuneSlotsDim(new List<int>());

            // 룬 슬롯 임시 잠금 해제
            information.SetRuneSlotsTemporaryLock(null);

            // 인벤토리 딤 처리 해제
            UpdateInventoryDim(new List<Guid>(), new List<Guid>());
        }

        private void UpdateRuneSlotsLock()
        {
            // ForbiddenRuneTypes가 있으면 해당 타입의 모든 룬 슬롯 잠금 처리
            if (_floorData != null && _floorData.ForbiddenRuneTypes != null && _floorData.ForbiddenRuneTypes.Count > 0)
            {
                // 잠금 처리할 슬롯에 장착된 룬 자동 해제
                UnequipRunesFromForbiddenSlots(_floorData.ForbiddenRuneTypes);

                information.SetRuneSlotsTemporaryLock(_floorData.ForbiddenRuneTypes);
            }
            else
            {
                information.SetRuneSlotsTemporaryLock(null);
            }
        }

        private void UnequipItemsFromForbiddenSlots()
        {
            if (_floorData == null)
            {
                return;
            }

            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.InfiniteTower];
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.InfiniteTower);

            bool anyUnequipped = false;

            // 장비 검증 및 해제
            foreach (var equipment in equipments)
            {
                try
                {
                    var testList = new List<Equipment> { equipment };
                    _floorData.ValidateItemTypeRestrictions(testList);
                    _floorData.ValidateItemGradeRestrictions(testList);
                    _floorData.ValidateItemLevelRestrictions(testList);
                }
                catch (Exception ex)
                {
                    NcDebug.Log($"[InfiniteTowerPreparation] Unequipping forbidden equipment - ItemId: {equipment.ItemId}, Error: {ex.Message}");
                    itemSlotState.Equipments.Remove(equipment.ItemId);
                    anyUnequipped = true;
                }
            }

            // 코스튬 검증 및 해제
            foreach (var costume in costumes)
            {
                try
                {
                    var testList = new List<Costume> { costume };
                    _floorData.ValidateItemTypeRestrictions(testList);
                    _floorData.ValidateItemGradeRestrictions(testList);
                    _floorData.ValidateItemLevelRestrictions(testList);
                }
                catch (Exception ex)
                {
                    NcDebug.Log($"[InfiniteTowerPreparation] Unequipping forbidden costume - ItemId: {costume.ItemId}, Error: {ex.Message}");
                    itemSlotState.Costumes.Remove(costume.ItemId);
                    anyUnequipped = true;
                }
            }

            // 아이템이 해제되었으면 뷰 업데이트
            if (anyUnequipped)
            {
                information.UpdateItemViewForInfiniteTowerPublic();
            }
        }

        private void UnequipRunesFromForbiddenSlots(List<RuneType> forbiddenRuneTypes)
        {
            // 무한의 탑에서는 룬을 Adventure 타입으로 사용
            var runeBattleType = BattleType.Adventure;
            var states = States.Instance.CurrentRuneSlotStates[runeBattleType].GetRuneSlot();

            bool anyUnequipped = false;
            foreach (var slot in states)
            {
                // ForbiddenRuneTypes에 포함된 타입이고, 룬이 장착되어 있으면 해제
                if (forbiddenRuneTypes.Contains(slot.RuneType) && slot.RuneId.HasValue)
                {
                    NcDebug.Log($"[InfiniteTowerPreparation] Unequipping rune from forbidden slot - SlotIndex: {slot.Index}, RuneType: {slot.RuneType}, RuneId: {slot.RuneId.Value}");
                    slot.Unequip();
                    anyUnequipped = true;
                }
            }

            // 룬이 해제되었으면 뷰 업데이트
            if (anyUnequipped)
            {
                information.UpdateRuneViewPublic();
            }
        }

        private void UpdateInventoryDim(List<Guid> invalidEquipmentIds, List<Guid> invalidCostumeIds)
        {
            NcDebug.Log($"[InfiniteTowerPreparation] UpdateInventoryDim called - invalidEquipmentIds: {invalidEquipmentIds?.Count ?? 0}, invalidCostumeIds: {invalidCostumeIds?.Count ?? 0}");

            var dimConditions = new List<(ItemType type, Predicate<Nekoyume.UI.Model.InventoryItem> predicate)>();

            // 장비/코스튬 딤 처리는 UpdateEquipmentEquipped와 UpdateCostumeEquipped에서 처리하므로 여기서는 제거
            // 단, 특정 invalidEquipmentIds/invalidCostumeIds에 대한 딤 처리는 유지

            // Additionally, dim any specifically identified invalid equipped items
            if (invalidEquipmentIds != null && invalidEquipmentIds.Count > 0)
            {
                NcDebug.Log($"[InfiniteTowerPreparation] Adding equipment dim conditions for {invalidEquipmentIds.Count} items");
                dimConditions.Add((ItemType.Equipment, item =>
                    item.ItemBase is Equipment eq && invalidEquipmentIds.Contains(eq.ItemId)));
            }

            if (invalidCostumeIds != null && invalidCostumeIds.Count > 0)
            {
                NcDebug.Log($"[InfiniteTowerPreparation] Adding costume dim conditions for {invalidCostumeIds.Count} items");
                dimConditions.Add((ItemType.Costume, item =>
                    item.ItemBase is Costume cs && invalidCostumeIds.Contains(cs.ItemId)));
            }

            NcDebug.Log($"[InfiniteTowerPreparation] Total dim conditions: {dimConditions.Count}");

            // 적용. 빈 리스트면 아무 변화 없음
            information.SetInventoryDimConditions(dimConditions);
        }



        private void ShowTicketPurchasePopup()
        {
            if (_floorData == null)
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_INFINITETOWER_FLOOR_DATA_NOT_FOUND"),
                    NotificationCell.NotificationType.Alert);
                coverToBlockClick.SetActive(false);
                return;
            }

            // InfiniteTowerInfo는 RxProps에서 관리
            var infiniteTowerInfo = RxProps.InfiniteTowerInfo.Value;
            if (infiniteTowerInfo == null)
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_INFINITETOWER_INFO_NOT_LOADED"),
                    NotificationCell.NotificationType.Alert);
                coverToBlockClick.SetActive(false);
                return;
            }

            // 두 가지 옵션이 모두 없는 경우
            if (!_floorData.NcgCost.HasValue &&
                (!_floorData.MaterialCostId.HasValue || !_floorData.MaterialCostCount.HasValue))
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_INFINITETOWER_TICKET_COST_NOT_CONFIGURED"),
                    NotificationCell.NotificationType.Alert);
                coverToBlockClick.SetActive(false);
                return;
            }

            // InfiniteTowerTicketPurchasePopup 사용 (AdventureBoss 패턴 참고)
            var popup = Find<InfiniteTowerTicketPurchasePopup>();
            popup.Show(
                _floorId,
                _floorData.NcgCost,
                _floorData.MaterialCostId,
                _floorData.MaterialCostCount,
                infiniteTowerInfo.NumberOfTicketPurchases,
                () => StartCoroutine(CoBattleStart(true, true)),  // NCG로 구매
                () => StartCoroutine(CoBattleStart(true, false)), // Material로 구매
                () => coverToBlockClick.SetActive(false)          // 닫힐 때 coverToBlockClick 해제
            );
        }

        private void StartInfiniteTowerTestBattle()
        {
            try
            {
                var random = new ActionRenderHandler.LocalRandom(System.DateTime.Now.Millisecond);
                var tableSheets = TableSheets.Instance;
                if (tableSheets == null)
                {
                    NcDebug.LogError("[InfiniteTowerTest] TableSheets is null");
                    return;
                }
                var scheduleSheet = tableSheets.InfiniteTowerScheduleSheet;
                var floorSheet = tableSheets.InfiniteTowerFloorSheet;
                var waveSheet = tableSheets.InfiniteTowerFloorWaveSheet;
                var conditionSheet = tableSheets.InfiniteTowerConditionSheet;

                // 첫 번째 타워 조회 (활성화 여부 무시)
                var firstSchedule = scheduleSheet.Values.FirstOrDefault();
                var infiniteTowerId = firstSchedule?.InfiniteTowerId ?? 1;

                NcDebug.Log($"[InfiniteTowerTest] Using Tower ID: {infiniteTowerId}");

                var floorRow = _floorData;
                // 웨이브 데이터 조회
                var waveRows = new List<InfiniteTowerFloorWaveSheet.WaveData>();
                if (waveSheet.TryGetValue(floorRow.Id, out var waves))
                {
                    waveRows = waves.Waves;
                    NcDebug.Log($"[InfiniteTowerTest] Found {waveRows.Count} waves for floor {floorRow.Id}");
                }
                else
                {
                    NcDebug.LogWarning($"[InfiniteTowerTest] No waves found for floor {floorRow.Id}");
                }

                // 조건 조회 (InfiniteTowerBattle.cs 패턴 참고)
                var conditions = new List<InfiniteTowerCondition>();

                // Guaranteed condition 조회
                if (floorRow.GuaranteedConditionId > 0)
                {
                    var guaranteedCondition = conditionSheet.Values
                        .FirstOrDefault(c => c.Id == floorRow.GuaranteedConditionId);

                    if (guaranteedCondition != null)
                    {
                        conditions.Add(new InfiniteTowerCondition(guaranteedCondition));
                        NcDebug.Log($"[InfiniteTowerTest] Added guaranteed condition: {guaranteedCondition.Id}");
                    }
                    else
                    {
                        NcDebug.LogWarning($"[InfiniteTowerTest] Guaranteed condition not found: {floorRow.GuaranteedConditionId}");
                    }
                }

                // Random conditions 조회
                var conditionWithWeights = floorRow.GetRandomConditionsWithWeights();
                if (floorRow.MinRandomConditions > 0 && conditionWithWeights.Count > 0)
                {
                    var availableConditions = conditionSheet.Values
                        .Where(c => conditionWithWeights.Select(t => t.conditionId).Contains(c.Id))
                        .Where(c => floorRow.GuaranteedConditionId == 0 || c.Id != floorRow.GuaranteedConditionId)
                        .ToList();

                    var randomCount = Math.Min(
                        random.Next(floorRow.MinRandomConditions, floorRow.MaxRandomConditions + 1),
                        availableConditions.Count
                    );

                    for (int i = 0; i < randomCount && availableConditions.Count > 0; i++)
                    {
                        var randomIndex = random.Next(0, availableConditions.Count);
                        var selectedCondition = availableConditions[randomIndex];
                        conditions.Add(new InfiniteTowerCondition(selectedCondition));
                        availableConditions.RemoveAt(randomIndex);
                        NcDebug.Log($"[InfiniteTowerTest] Added random condition: {selectedCondition.Id}");
                    }
                }

                NcDebug.Log($"[InfiniteTowerTest] Using {conditions.Count} conditions (Guaranteed: {floorRow.GuaranteedConditionId}, Random: {conditions.Count - (floorRow.GuaranteedConditionId > 0 ? 1 : 0)})");

                // 아바타 상태 준비
                var avatar = States.Instance.CurrentAvatarState;
                var equipments = States.Instance.CurrentItemSlotStates[BattleType.InfiniteTower].Equipments;
                var costumes = States.Instance.CurrentItemSlotStates[BattleType.InfiniteTower].Costumes;
                avatar.EquipItems(equipments.Concat(costumes).ToList());

                // 시뮬레이터 생성 및 실행
                var simulator = new InfiniteTowerSimulator(
                    random,
                    avatar,
                    new List<Guid>(), // foods
                    States.Instance.AllRuneState,
                    States.Instance.CurrentRuneSlotStates[BattleType.Adventure],
                    infiniteTowerId,
                    floorRow.Id,
                    floorRow,
                    waveRows,
                    false, // isCleared
                    0, // exp
                    tableSheets.GetSimulatorSheets(),
                    tableSheets.EnemySkillSheet,
                    tableSheets.CostumeStatSheet,
                    tableSheets.ItemSheet,
                    States.Instance.CollectionState.GetEffects(tableSheets.CollectionSheet),
                    tableSheets.BuffLimitSheet,
                    tableSheets.BuffLinkSheet,
                    conditions,
                    (int)States.Instance.GameConfigState.ShatterStrikeMaxDamage,
                    logEvent: true
                );
                Game.Game.instance.Stage.OnEnterToStageEnd
                    .First()
                    .Subscribe(_ =>
                    {
                        UniTask.Void(() =>
                        {
                            try
                            {
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            }
                            catch (Exception e)
                            {
                                NcDebug.LogException(e);
                            }

                            return default;
                        });
                    });

                NcDebug.Log($"[InfiniteTowerTest] Starting simulation...");
                simulator.Simulate();

                // 시뮬레이션 결과 상세 로그
                NcDebug.Log($"[InfiniteTowerTest] Simulation completed - Events: {simulator.Log.events.Count}, IsClear: {simulator.Log.IsClear}");

                // 전투 화면 렌더링
                var stage = Game.Game.instance.Stage;
                stage.StageType = StageType.InfiniteTower;

                NcDebug.Log($"[InfiniteTowerTest] Battle result - Clear: {simulator.Log.IsClear}, Waves: {simulator.Log.clearedWaveNumber}");

                // 전투 화면 렌더링 시작
                NcDebug.Log($"[InfiniteTowerTest] Preparing battle stage...");
                BattleRenderer.Instance.PrepareStage(simulator.Log);

                // 전투 화면으로 전환 확인
                NcDebug.Log($"[InfiniteTowerTest] Battle stage prepared, checking if battle started...");
            }
            catch (System.Exception e)
            {
                NcDebug.LogError($"[InfiniteTowerTest] Error: {e}");
                throw;
            }
        }
    }
}
