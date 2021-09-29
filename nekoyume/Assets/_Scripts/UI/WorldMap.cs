using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.Model.Quest;
using Nekoyume.UI.Module;
using UnityEngine;
using mixpanel;
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
            SharedViewModel = new ViewModel();
            SharedViewModel.SelectedStageId.Value = firstStageId;
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
                if (!worldButton.IsShown)
                {
                    continue;
                }

                var worldId = worldButton.Id;
                var worldIsUnlocked =
                    worldInformation.TryGetWorld(worldId, out var worldModel) &&
                    worldModel.IsUnlocked;

                UpdateNotificationInfo();

                var isIncludedInQuest = StageIdToNotify >= worldButton.StageBegin && StageIdToNotify <= worldButton.StageEnd;

                if (worldIsUnlocked)
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

            var status = Find<Status>();
            status.Close(true);
            Show(true);
            HelpPopup.HelpMe(100002, true);
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

        private void ShowWorld(int worldId)
        {
            if (!SharedViewModel.WorldInformation.TryGetWorld(worldId, out var world))
                throw new ArgumentException(nameof(worldId));

            if (worldId == 1)
            {
                Mixpanel.Track("Unity/Click Yggdrasil");
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

            var stageInfo = Find<UI.StageInformation>();
            SharedViewModel.WorldInformation.TryGetWorld(worldId, out var world);
            stageInfo.Show(SharedViewModel, worldRow, StageInformation.StageType.Quest);
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
    }
}
