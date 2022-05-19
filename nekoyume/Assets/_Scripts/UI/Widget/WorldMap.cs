using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.Model.Quest;
using Nekoyume.UI.Module;
using UnityEngine;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class WorldMap : Widget
    {
        public class ViewModel
        {
            public readonly ReactiveProperty<bool> IsWorldShown = new ReactiveProperty<bool>(false);
            public readonly ReactiveProperty<int> SelectedWorldId = new ReactiveProperty<int>(1);
            public readonly ReactiveProperty<int> SelectedStageId = new ReactiveProperty<int>(1);

            public WorldInformation WorldInformation;
            public List<int> UnlockedWorldIds;
        }

        [SerializeField] private GameObject worldMapRoot = null;
        [SerializeField] private Button closeButton;

        private WorldButton[] _worldButtons;
        public ViewModel SharedViewModel { get; private set; }

        public int SelectedWorldId
        {
            get => SharedViewModel.SelectedWorldId.Value;
            private set => SharedViewModel.SelectedWorldId.SetValueAndForceNotify(value);
        }

        public int SelectedStageId
        {
            get => SharedViewModel.SelectedStageId.Value;
            private set => SharedViewModel.SelectedStageId.SetValueAndForceNotify(value);
        }

        private int SelectedWorldStageBegin { get; set; }

        public bool HasNotification { get; private set; }

        public int StageIdToNotify { get; private set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close();
                Game.Event.OnRoomEnter.Invoke(true);
            });

            CloseWidget = () =>
            {
                Close();
                Game.Event.OnRoomEnter.Invoke(true);
            };
            _worldButtons = GetComponentsInChildren<WorldButton>();
        }

        public override void Initialize()
        {
            base.Initialize();
            var firstStageId = Game.Game.instance.TableSheets.StageWaveSheet.First?.StageId ?? 1;
            SharedViewModel = new ViewModel
            {
                SelectedStageId =
                {
                    Value = firstStageId
                }
            };
            var sheet = Game.Game.instance.TableSheets.WorldSheet;
            foreach (var worldButton in _worldButtons)
            {
                if (!sheet.TryGetByName(worldButton.WorldName, out var row))
                {
                    worldButton.Hide();
                    continue;
                }

                worldButton.Set(row);
                worldButton.Show();
                worldButton.OnClickSubject
                    .Subscribe(world =>
                    {
                        if (world.IsUnlockable)
                        {
                            if (!ShowManyWorldUnlockPopup(SharedViewModel.WorldInformation))
                            {
                                ShowWorldUnlockPopup(row.Id);
                            }
                        }
                        else
                        {
                            ShowWorld(row.Id);
                        }
                    }).AddTo(gameObject);
            }

            AgentStateSubject.Crystal.Subscribe(crystal =>
            {
                foreach (var worldButton in _worldButtons)
                {
                    worldButton.SetOpenCostTextColor(crystal.MajorUnit);
                }
            }).AddTo(gameObject);
        }

        #endregion

        public void Show(WorldInformation worldInformation)
        {
            HasNotification = false;
            SetWorldInformation(worldInformation);

            var status = Find<Status>();
            status.Close(true);
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
            Show(true);
            HelpTooltip.HelpMe(100002, true);
            ShowManyWorldUnlockPopup(worldInformation);
        }

        public void Show(int worldId, int stageId, bool showWorld, bool callByShow = false)
        {
            ShowWorld(worldId, stageId, showWorld, callByShow);
            Show(true);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(true);
        }

        public void SetWorldInformation(WorldInformation worldInformation)
        {
            SharedViewModel.WorldInformation = worldInformation;
            if (worldInformation is null)
            {
                return;
            }

            foreach (var worldButton in _worldButtons)
            {
                if (!worldButton.IsShown)
                {
                    continue;
                }

                var buttonWorldId = worldButton.Id;
                var worldIsUnlocked =
                    worldInformation.TryGetWorld(buttonWorldId, out var worldModel) &&
                    worldModel.IsUnlocked;

                UpdateNotificationInfo();

                var isIncludedInQuest = StageIdToNotify >= worldButton.StageBegin && StageIdToNotify <= worldButton.StageEnd;

                if (worldIsUnlocked)
                {
                    worldButton.HasNotification.Value = isIncludedInQuest;
                    worldButton.Unlock(!SharedViewModel.UnlockedWorldIds.Contains(worldButton.Id));
                }
                else
                {
                    worldButton.Lock();
                }
            }

            if (!worldInformation.TryGetFirstWorld(out var firstWorld))
            {
                throw new Exception("worldInformation.TryGetFirstWorld() failed!");
            }
        }

        private void ShowWorld(int worldId)
        {
            if (!SharedViewModel.WorldInformation.TryGetWorld(worldId, out var world))
                throw new ArgumentException(nameof(worldId));

            if (worldId == 1)
            {
                Analyzer.Instance.Track("Unity/Click Yggdrasil");
            }

            Push();
            ShowWorld(world.Id, world.GetNextStageId(), false);
        }

        private void ShowWorld(int worldId, int stageId, bool showWorld, bool callByShow = false)
        {
            if (callByShow)
            {
                CallByShowUpdateWorld();
            }
            else
            {
                SharedViewModel.IsWorldShown.SetValueAndForceNotify(showWorld);
            }

            SelectedWorldId = worldId;
            Game.Game.instance.TableSheets.WorldSheet.TryGetValue(SelectedWorldId, out var worldRow, true);
            SelectedWorldStageBegin = worldRow.StageBegin;
            SelectedStageId = stageId;

            var stageInfo = Find<StageInformation>();
            stageInfo.Show(SharedViewModel, worldRow, StageType.HackAndSlash);
        }

        public void UpdateNotificationInfo()
        {
            var questStageId = Game.Game.instance.States.CurrentAvatarState.questList?
                .OfType<WorldQuest>()
                .Where(x => !x.Complete)
                .OrderBy(x => x.Goal)
                .FirstOrDefault()?
                .Goal ?? -1;
            StageIdToNotify = questStageId;

            HasNotification = questStageId > 0;
        }

        private void CallByShowUpdateWorld()
        {
            var status = Find<Status>();
            status.Close(true);
            worldMapRoot.SetActive(true);
        }

        private void OnAttractInPaymentPopup()
        {
            Close(true);
            Find<Grind>().Show();
        }

        private void ShowWorldUnlockPopup(int worldId)
        {
            var cost = CrystalCalculator.CalculateWorldUnlockCost(new[] {worldId},
                    Game.TableSheets.Instance.WorldUnlockSheet)
                .MajorUnit;
            var balance = States.Instance.CrystalBalance;
            var usageMessage = L10nManager.Localize(
                "UI_UNLOCK_WORLD_FORMAT",
                L10nManager.LocalizeWorldName(worldId));
            Find<PaymentPopup>().Show(
                CostType.Crystal,
                balance.MajorUnit,
                cost,
                balance.GetPaymentFormatText(usageMessage, cost),
                L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL"),
                () =>
                {
                    Find<UnlockWorldLoadingScreen>().Show();
                    ActionManager.Instance.UnlockWorld(new List<int> {worldId}).Subscribe();
                },
                OnAttractInPaymentPopup);
        }

        private bool ShowManyWorldUnlockPopup(WorldInformation worldInformation)
        {
            if (worldInformation.TryGetLastClearedStageId(out var stageId))
            {
                var tableSheets = Game.TableSheets.Instance;
                var countOfCanUnlockWorld = Math.Min(stageId / 50,
                    tableSheets.WorldUnlockSheet.Count - 1);
                var worldIdListForUnlock = Enumerable.Range(2, countOfCanUnlockWorld)
                    .Where(i => !SharedViewModel.UnlockedWorldIds.Contains(i))
                    .ToList();

                if (worldIdListForUnlock.Count > 1)
                {
                    var paymentPopup = Find<PaymentPopup>();
                    var cost = CrystalCalculator.CalculateWorldUnlockCost(worldIdListForUnlock,
                        tableSheets.WorldUnlockSheet).MajorUnit;
                    paymentPopup.Show(
                        CostType.Crystal,
                        States.Instance.CrystalBalance.MajorUnit,
                        cost,
                        L10nManager.Localize(
                            "CRYSTAL_MIGRATION_WORLD_ALL_OPEN_FORMAT", cost),
                        L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL"),
                        () =>
                        {
                            Find<UnlockWorldLoadingScreen>().Show();
                            ActionManager.Instance.UnlockWorld(worldIdListForUnlock).Subscribe();
                        },
                        OnAttractInPaymentPopup);
                    return true;
                }
            }

            return false;
        }
    }
}
