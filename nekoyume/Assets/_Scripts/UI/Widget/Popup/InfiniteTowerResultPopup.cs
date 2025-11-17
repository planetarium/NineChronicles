using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
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

        private static readonly int ClearedWave = Animator.StringToHash("ClearedWave");
        private readonly WaitForSeconds _battleWinVFXYield = new(0.2f);

        [Header("UI")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private TextMeshProUGUI floorText;

        [SerializeField]
        private GameObject rewardArea;

        [FormerlySerializedAs("rewardViews")]
        [SerializeField]
        private List<SimpleCountableItemView> itemRewardViews;

        [Header("Buttons")]
        [SerializeField]
        private Button backButton;

        [SerializeField]
        private Button preparationButton;

        [SerializeField]
        private GameObject victoryImageContainer;

        [SerializeField]
        private GameObject defeatImageContainer;

        [SerializeField]
        private Animator _victoryImageAnimator;

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
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            _model = model;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            base.Show();

            var isClear = _model.IsClear;

            floorText.text = string.IsNullOrEmpty(model.FloorName)
                ? L10nManager.Localize("UI_INFINITETOWER_FLOOR_NUMBER", model.FloorId)
                : model.FloorName;

            // Buttons
            backButton.gameObject.SetActive(true);
            preparationButton.gameObject.SetActive(!isClear);

            // Rewards
            var showRewards = isClear && model.Rewards != null && model.Rewards.Count > 0;
            rewardArea.SetActive(showRewards);
            if (showRewards)
            {
                SetRewards(model.Rewards);
            }

            StartCoroutine(isClear ? CoUpdateViewWin() : CoUpdateViewLose());
        }

        private IEnumerator CoUpdateViewWin()
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.Win, 0.3f);
            StartCoroutine(EmitBattleWinVFX());
            victoryImageContainer.SetActive(true);
            defeatImageContainer.SetActive(false);
            _victoryImageAnimator.SetInteger(ClearedWave, 3);
            rewardArea.SetActive(true);

            yield return null;
        }

        private IEnumerator CoUpdateViewLose()
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.Lose);

            victoryImageContainer.SetActive(false);
            defeatImageContainer.SetActive(true);
            rewardArea.SetActive(false);

            yield return null;
        }

        private IEnumerator EmitBattleWinVFX()
        {
            yield return _battleWinVFXYield;
            AudioController.instance.PlaySfx(AudioController.SfxCode.Win);
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
            GoToInfiniteTowerWithCallback((infiniteTower, loading) =>
            {
                // InfiniteTower를 동기적으로 표시
                infiniteTower.Show(true);
            });
        }

        private void GoToPreparation()
        {
            GoToInfiniteTowerWithCallback((infiniteTower, loading) =>
            {
                // InfiniteTower를 표시
                infiniteTower.Show(true);

                // InfiniteTower 위젯의 ShowPreparationForFloor 메서드를 사용하여
                // floorData, battleConditions, buffConditions를 자동으로 로드하고 preparation을 엽니다
                if (_model != null && _model.FloorId > 0)
                {
                    infiniteTower.ShowPreparationForFloor(_model.FloorId);
                }
                else
                {
                    NcDebug.LogError("[InfiniteTowerResultPopup] Invalid FloorId in model");
                }
            });
        }

        private void GoToInfiniteTowerWithCallback(Action<InfiniteTower, LoadingScreen> onLobbyEnterEnd)
        {
            CloseWithBattle();

            var infiniteTower = Find<InfiniteTower>();

            Game.Game.instance.Lobby.OnLobbyEnterEnd.First().Subscribe(_ =>
            {
                var loading = Find<LoadingScreen>();
                loading.Show(LoadingScreen.LoadingType.InfiniteTower);
                try
                {
                    CloseWithOtherWidgets();

                    // 로비 진입이 완료된 후 월드맵 표시
                    Find<WorldMap>().Show(States.Instance.CurrentAvatarState.worldInformation, true);

                    // 콜백 실행
                    onLobbyEnterEnd(infiniteTower, loading);
                }
                finally
                {
                    loading.Close(true);
                }
            });
        }

    }
}
