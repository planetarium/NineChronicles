using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public sealed class InfiniteTowerResultPopup : PopupWidget
    {
        [Serializable]
        public sealed class Model
        {
            public bool IsClear;
            public int InfiniteTowerId;
            public int FloorId;
            public string FloorName;
            public IReadOnlyList<CountableItem> Rewards => _rewards;

            private readonly List<CountableItem> _rewards = new();

            public void AddReward(CountableItem reward)
            {
                if (reward.ItemBase.HasValue)
                {
                    var sameReward = _rewards.FirstOrDefault(e =>
                        e.ItemBase.Value.Equals(reward.ItemBase.Value));
                    if (sameReward is null)
                    {
                        _rewards.Add(reward);
                        return;
                    }

                    sameReward.Count.Value += reward.Count.Value;
                }
                else
                {
                    var sameReward = _rewards.FirstOrDefault(e =>
                        e.FungibleAssetValue.Value.Equals(reward.FungibleAssetValue.Value));
                    if (sameReward is null)
                    {
                        _rewards.Add(reward);
                        return;
                    }

                    sameReward.Count.Value += reward.Count.Value;
                }
            }
        }

        [Header("UI")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI floorText;

        [SerializeField]
        private GameObject rewardArea;

        [FormerlySerializedAs("rewardViews")]
        [SerializeField]
        private List<RuneStoneItem> runeRewardViews;

        [SerializeField]
        private List<SimpleCountableItemView> itemRewardViews;

        [Header("Buttons")]
        [SerializeField]
        private Button backButton;

        [SerializeField]
        private Button preparationButton;

        private Model _model;

        protected override void Awake()
        {
            base.Awake();

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnClickBack);
            }

            if (preparationButton != null)
            {
                preparationButton.onClick.AddListener(OnClickPreparation);
            }

            CloseWidget = () => backButton?.onClick.Invoke();
            SubmitWidget = () => backButton?.onClick.Invoke();
        }

        public void Show(Model model)
        {
            _model = model;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            titleText.text = model.IsClear
                ? L10nManager.Localize("UI_INFINITETOWER_RESULT_CLEAR")
                : L10nManager.Localize("UI_INFINITETOWER_RESULT_FAIL");

            floorText.text = string.IsNullOrEmpty(model.FloorName)
                ? L10nManager.Localize("UI_INFINITETOWER_FLOOR_NUMBER", model.FloorId)
                : model.FloorName;

            // Buttons
            backButton.gameObject.SetActive(true);
            preparationButton.gameObject.SetActive(!model.IsClear);

            // Rewards
            var showRewards = model.IsClear && model.Rewards != null && model.Rewards.Count > 0;
            rewardArea.SetActive(showRewards);
            if (showRewards)
            {
                SetRewards(model.Rewards);
            }

            base.Show();
        }

        private void SetRewards(IReadOnlyList<CountableItem> rewards)
        {
            foreach (var view in itemRewardViews)
            {
                view.gameObject.SetActive(false);
            }

            for (var i = 0; i < rewards.Count; i++)
            {
                var item = rewards[i];
                if (item.ItemBase.HasValue)
                {
                    var itemBase = item.ItemBase.Value;
                    itemRewardViews[i].SetData(item, () => ShowTooltip(itemBase));
                    itemRewardViews[i].gameObject.SetActive(true);
                }
                else
                {
                    var fav = item.FungibleAssetValue.Value;
                    itemRewardViews[i].SetData(fav);
                }
            }
        }

        private static void ShowTooltip(ItemBase itemBase)
        {
            AudioController.PlayClick();
            var tooltip = ItemTooltip.Find(itemBase.ItemType);
            tooltip.Show(itemBase, string.Empty, false, null);
        }

        private void OnClickBack()
        {
            AudioController.PlayClick();
            GoToInfiniteTower();
        }

        private void OnClickPreparation()
        {
            AudioController.PlayClick();
            GoToPreparation();
        }

        private void CloseWithBattle()
        {
            Lobby.Enter(true);
            Close();
        }

        private void GoToInfiniteTower()
        {
            CloseWithBattle();

            var loading = Widget.Find<LoadingScreen>();
            loading.Show(LoadingScreen.LoadingType.InfiniteTower);

            Game.Game.instance.Lobby.OnLobbyEnterEnd.First().Subscribe(_ =>
            {
                try
                {
                    CloseWithOtherWidgets();
                    var infiniteTower = Widget.Find<InfiniteTower>();
                    if (infiniteTower != null)
                    {
                        infiniteTower.Show(true);
                    }
                }
                finally
                {
                    loading.Close(true);
                }
            });
        }

        private void GoToPreparation()
        {
            CloseWithBattle();

            var loading = Widget.Find<LoadingScreen>();
            loading.Show(LoadingScreen.LoadingType.InfiniteTower);

            Game.Game.instance.Lobby.OnLobbyEnterEnd.First().Subscribe(_ =>
            {
                try
                {
                    CloseWithOtherWidgets();

                    var worldMap = Widget.Find<WorldMap>();
                    worldMap?.Close(true);

                    var prepare = Widget.Find<InfiniteTowerPreparation>();
                    if (prepare != null)
                    {
                        // 재진입 시 View만 갱신해도 충분. 외부에서 필요한 모델은 별도로 셋업될 수 있음.
                        prepare.UpdateInventoryView();
                        prepare.Show(
                            L10nManager.Localize("UI_BACK"),
                            0,
                            null,
                            null,
                            null,
                            _model?.InfiniteTowerId ?? 0,
                            _model?.FloorId ?? 0,
                            true);
                    }
                }
                finally
                {
                    loading.Close(true);
                }
            });
        }
    }
}
