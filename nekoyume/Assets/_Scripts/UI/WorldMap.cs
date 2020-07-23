using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using mixpanel;

namespace Nekoyume.UI
{
    public class WorldMap : Widget
    {
        public class ViewModel
        {
            public readonly ReactiveProperty<bool> IsWorldShown = new ReactiveProperty<bool>(false);
            public readonly ReactiveProperty<int> SelectedWorldId = new ReactiveProperty<int>(1);
            public readonly ReactiveProperty<int> SelectedStageId = new ReactiveProperty<int>(1);

            public WorldInformation WorldInformation;
        }

        public List<WorldMapWorld> worlds = new List<WorldMapWorld>();

        [SerializeField]
        private GameObject worldMapRoot = null;

        private readonly List<IDisposable> _disposablesAtShow = new List<IDisposable>();

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

            CloseWidget = null;
            _worldButtons = GetComponentsInChildren<WorldButton>();
        }

        public override void Initialize()
        {
            base.Initialize();
            var firstStageId = Game.Game.instance.TableSheets.StageWaveSheet.First?.StageId ?? 1;
            SharedViewModel = new ViewModel();
            SharedViewModel.SelectedStageId.Value = firstStageId;
            var sheet = Game.Game.instance.TableSheets.WorldSheet;
            foreach (var worldButton in _worldButtons)
            {
                if (!sheet.TryGetByName(worldButton.WorldName, out var row))
                {
                    throw new SheetRowNotFoundException("WorldSheet", "Name", worldButton.WorldName);
                }

                worldButton.Set(row);
                worldButton.OnClickSubject
                    .Subscribe(_ => ShowWorld(row.Id));
            }
        }

        #endregion

        public void Show(WorldInformation worldInformation)
        {
            HasNotification = false;
            SharedViewModel.WorldInformation = worldInformation;
            if (worldInformation is null)
            {
                return;
            }

            foreach (var worldButton in _worldButtons)
            {
                var worldId = worldButton.Id;
                if (!worldInformation.TryGetWorld(worldId, out var worldModel))
                    throw new Exception(nameof(worldId));

                UpdateNotificationInfo();

                var isIncludedInQuest = StageIdToNotify >= worldButton.StageBegin && StageIdToNotify <= worldButton.StageEnd;

                if (worldModel.IsUnlocked)
                {
                    worldButton.HasNotification.Value = isIncludedInQuest;
                    worldButton.Unlock();
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
            var bottomMenu = Find<BottomMenu>();
            bottomMenu.Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick);
            var status = Find<Status>();

            status.Close(true);
            Show();
        }

        public void Show(int worldId, int stageId, bool showWorld, bool callByShow = false)
        {
            var bottomMenu = Find<BottomMenu>();
            bottomMenu.Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.WorldMap);
            bottomMenu.worldMapButton.OnClick
                .Subscribe(_ => SharedViewModel.IsWorldShown.SetValueAndForceNotify(true))
                .AddTo(_disposablesAtShow);

            ShowWorld(worldId, stageId, showWorld, callByShow);
            Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            Find<BottomMenu>().Close(true);
            base.Close(ignoreCloseAnimation);
        }

        private void ShowWorld(int worldId)
        {
            if (!SharedViewModel.WorldInformation.TryGetWorld(worldId, out var world))
                throw new ArgumentException(nameof(worldId));

            if (worldId == 1)
            {
                Mixpanel.Track("Unity/Click Yggdrasil");
            }

            CloseWidget = () =>
            {
                var button = Find<BottomMenu>().worldMapButton;
                button.OnClick.OnNext(button);
            };
            CloseWidget += Pop;
            CloseWidget += () => CloseWidget = null;
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

            var stageInfo = Find<UI.StageInformation>();
            SharedViewModel.WorldInformation.TryGetWorld(worldId, out var world);
            stageInfo.Show(SharedViewModel, worldRow);
        }

        public void UpdateNotificationInfo()
        {
            var questStageId = Game.Game.instance.States
                .CurrentAvatarState.questList
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

            var bottomMenu = Find<BottomMenu>();
            bottomMenu.worldMapButton.Hide();
            bottomMenu.backButton.Show();
            status.Close(true);
            worldMapRoot.SetActive(true);
        }

        private void UpdateWorld(bool active)
        {
            var status = Find<Status>();

            if (active)
            {
                var bottomMenu = Find<BottomMenu>();
                bottomMenu.worldMapButton.Hide();
                bottomMenu.backButton.Show();
                status.Close(true);
                worldMapRoot.SetActive(true);
            }
            else
            {
                var bottomMenu = Find<BottomMenu>();
                bottomMenu.Show(UINavigator.NavigationType.Back, SubscribeBackButtonClick, true,
                    BottomMenu.ToggleableType.WorldMap);
                bottomMenu.worldMapButton.Show();
                bottomMenu.backButton.Hide();
                bottomMenu.ToggleGroup?.SetToggledOffAll();
                status.Show();
            }
        }

        public void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            if (!CanClose)
            {
                return;
            }

            SharedViewModel.IsWorldShown.SetValueAndForceNotify(false);
            Close();
            Game.Event.OnRoomEnter.Invoke(true);
        }
    }
}
