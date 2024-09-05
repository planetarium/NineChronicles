using Nekoyume.UI.Module;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace Nekoyume.UI
{
    using Cysharp.Threading.Tasks;
    using Libplanet.Types.Assets;
    using Blockchain;
    using Data;
    using Game;
    using Game.Controller;
    using Helper;
    using L10n;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using State;
    using System.Linq;
    using UniRx;

    public class AdventureBossEnterBountyPopup : PopupWidget
    {
        [SerializeField]
        private TMP_InputField bountyInputArea;

        [SerializeField]
        private TextMeshProUGUI bountyInputPlaceholder;

        [SerializeField]
        private GameObject inputCountObj;

        [SerializeField]
        private GameObject inputPlaceholderObj;

        [SerializeField]
        private GameObject inputWarning;

        [SerializeField]
        private TextMeshProUGUI inputWarningText;

        [SerializeField]
        private ConditionalButton confirmButton;

        [SerializeField]
        private GameObject stakingWarningMassage;

        [SerializeField]
        private TextMeshProUGUI stakingWarningMassageText;

        [SerializeField]
        private GameObject showDetailButton;

        [SerializeField]
        private TextMeshProUGUI bountyedPrice;

        [SerializeField]
        private TextMeshProUGUI bountyCount;

        [SerializeField]
        private TextMeshProUGUI additionalBountyPrice;

        [SerializeField]
        private GameObject additionalBountyObj;

        [SerializeField]
        private TextMeshProUGUI totalBountyPrice;

        [SerializeField]
        private Color bountyRedColor;

        [SerializeField]
        private Transform bossImgRoot;

        [SerializeField]
        private TextMeshProUGUI bossName;

        [SerializeField]
        private GameObject[] firstBountyObjs;

        [SerializeField]
        private GameObject[] secondBountyObjs;

        [SerializeField]
        private BaseItemView[] expectedRewardItems;

        [SerializeField]
        private BountyBossCell[] bountyBossCells;

        [SerializeField]
        private ConditionalButton bountyViewAllButton;

        [SerializeField]
        private Button stakingWarningButton;

        private Color _bountyDefaultColor;
        private readonly List<System.IDisposable> _disposablesByEnable = new();
        private GameObject _bossImage;
        private int _bossId;

        protected override void Awake()
        {
            stakingWarningButton.onClick.AddListener(LackStakingLevelPopup);
            bountyInputPlaceholder.text = L10nManager.Localize("ADVENTURE_BOSS_BOUNTY_INPUT_PLACEHOLDER", States.Instance.GameConfigState.AdventureBossMinBounty);
            bountyInputArea.onSelect.AddListener(OnBountyInputAreaFocus);
            bountyInputArea.onValueChanged.AddListener(OnBountyInputAreaValueChanged);
            bountyInputArea.onEndEdit.AddListener(OnBountyInputAreaValueChanged);
            _bountyDefaultColor = bountyInputArea.textComponent.color;
            confirmButton.OnSubmitSubject.Subscribe(_ => OnClickConfirm()).AddTo(gameObject);
            bountyViewAllButton.OnSubmitSubject.Subscribe(_ =>
                Find<AdventureBossFullBountyStatusPopup>().Show()
            ).AddTo(gameObject);
            base.Awake();
        }

        private void LackStakingLevelPopup()
        {
            var paymentPopup = Find<PaymentPopup>();
            paymentPopup.ShowLackMonsterCollection(States.Instance.GameConfigState.AdventureBossWantedRequiredStakingLevel);
            Close(true);
        }

        private void OnBountyInputAreaFocus(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                inputCountObj.SetActive(true);
                inputPlaceholderObj.SetActive(false);
            }
        }

        private void OnBountyInputAreaValueChanged(string input)
        {
            var adventureBossData = Game.instance.AdventureBossData;
            if (string.IsNullOrEmpty(input))
            {
                inputCountObj.SetActive(false);
                inputPlaceholderObj.SetActive(true);
                var bountyRewards = adventureBossData.GetCurrentBountyRewards();
                RefreshRewards(bountyRewards);
            }
            else
            {
                inputCountObj.SetActive(true);
                //additionalBountyObj.SetActive(true);
            }

            if (long.TryParse(input, out var bounty))
            {
                if (bounty < States.Instance.GameConfigState.AdventureBossMinBounty)
                {
                    bountyInputArea.textComponent.color = bountyRedColor;
                    inputWarning.SetActive(true);
                    inputWarningText.text = L10nManager.Localize("ADVENTURE_BOSS_BOUNTY_INPUT_WARNING", States.Instance.GameConfigState.AdventureBossMinBounty);
                    confirmButton.Interactable = false;
                }
                else
                {
                    bountyInputArea.textComponent.color = _bountyDefaultColor;
                    inputWarning.SetActive(false);
                    confirmButton.Interactable = true;
                }

                if (bounty > Game.instance.States?.GoldBalanceState?.Gold.MajorUnit)
                {
                    bountyInputArea.text = Game.instance.States?.GoldBalanceState?.Gold.MajorUnit.ToString();
                    return;
                }

                if (adventureBossData != null && adventureBossData.CurrentState.Value == Model.AdventureBossData.AdventureBossSeasonState.Progress)
                {
                    var bountyRewards = adventureBossData.GetCurrentBountyRewards(bounty);
                    RefreshRewards(bountyRewards);
                }

                additionalBountyPrice.text = $"+ {bounty.ToString("#,0")}";
            }
            else
            {
                confirmButton.Interactable = false;
                var bountyRewards = adventureBossData.GetCurrentBountyRewards();
                RefreshRewards(bountyRewards);
            }
        }

        public void ClearBountyInputField()
        {
            if (string.IsNullOrEmpty(bountyInputArea.text))
            {
                OnBountyInputAreaValueChanged("");
                bountyInputArea.ReleaseSelection();
                var eventSystem = EventSystem.current;
                if (eventSystem != null && eventSystem.currentSelectedGameObject == bountyInputArea.gameObject)
                {
                    eventSystem.SetSelectedGameObject(null);
                }

                return;
            }

            bountyInputArea.text = "";
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (Nekoyume.Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                return;
            }

            if (States.Instance.StakingLevel < States.Instance.GameConfigState.AdventureBossWantedRequiredStakingLevel)
            {
                stakingWarningMassage.SetActive(true);
                bountyInputArea.gameObject.SetActive(false);
                stakingWarningMassageText.text = L10nManager.Localize("ADVENTURE_BOSS_BOUNTY_INPUT_STAKING_LEVEL_WARNING", States.Instance.GameConfigState.AdventureBossWantedRequiredStakingLevel);
            }
            else
            {
                stakingWarningMassage.SetActive(false);
                bountyInputArea.gameObject.SetActive(true);
            }

            confirmButton.Interactable = false;
            inputCountObj.SetActive(false);

            bountyedPrice.text = "-";
            totalBountyPrice.text = "-";
            bountyCount.text = "-";
            ClearBountyInputField();
            var adventureBossData = Game.instance.AdventureBossData;
            var tableSheets = TableSheets.Instance;
            switch (adventureBossData.CurrentState.Value)
            {
                case Model.AdventureBossData.AdventureBossSeasonState.Ready:
                    bountyedPrice.text = "0";
                    totalBountyPrice.text = "0";
                    bountyCount.text = "(0/3)";
                    showDetailButton.SetActive(false);
                    foreach (var item in firstBountyObjs)
                    {
                        item.SetActive(true);
                    }

                    foreach (var item in secondBountyObjs)
                    {
                        item.SetActive(false);
                    }

                    var rewards = tableSheets.AdventureBossWantedRewardSheet.Values.ToList();
                    for (var bossIndex = 0; bossIndex < bountyBossCells.Length; bossIndex++)
                    {
                        if (bossIndex >= rewards.Count)
                        {
                            bountyBossCells[bossIndex].gameObject.SetActive(false);
                            break;
                        }

                        bountyBossCells[bossIndex].gameObject.SetActive(true);
                        bountyBossCells[bossIndex].SetData(rewards[bossIndex]);
                    }

                    bossName.text = string.Empty;
                    break;
                case Model.AdventureBossData.AdventureBossSeasonState.Progress:
                    showDetailButton.SetActive(true);
                    foreach (var item in firstBountyObjs)
                    {
                        item.SetActive(false);
                    }

                    foreach (var item in secondBountyObjs)
                    {
                        item.SetActive(true);
                    }

                    var currentBountyInfo = adventureBossData.GetCurrentInvestorInfo();
                    if (currentBountyInfo != null)
                    {
                        bountyedPrice.text = currentBountyInfo.Price.ToCurrencyNotation();
                        bountyCount.text = $"({currentBountyInfo.Count}/3)";
                    }
                    else
                    {
                        bountyedPrice.text = "0";
                        bountyCount.text = "(0/3)";
                    }

                    totalBountyPrice.text = adventureBossData.GetCurrentBountyPrice().MajorUnit.ToString("#,0");
                    var bountyRewards = adventureBossData.GetCurrentBountyRewards();
                    RefreshRewards(bountyRewards);
                    bossName.text = L10nManager.LocalizeCharacterName(adventureBossData.SeasonInfo.Value.BossId);
                    SetBossData(adventureBossData.SeasonInfo.Value.BossId);
                    break;
                case Model.AdventureBossData.AdventureBossSeasonState.None:
                case Model.AdventureBossData.AdventureBossSeasonState.End:
                default:
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_INVALID"), Scroller.NotificationCell.NotificationType.Alert);
                    NcDebug.LogError("[AdventureBossEnterBountyPopup] Show: Invalid state");
                    return;
            }

            base.Show(ignoreShowAnimation);

            adventureBossData.CurrentState.Subscribe(state =>
            {
                if (state == Model.AdventureBossData.AdventureBossSeasonState.None ||
                    state == Model.AdventureBossData.AdventureBossSeasonState.End)
                {
                    Close();
                }
            }).AddTo(_disposablesByEnable);
        }

        private void RefreshRewards(AdventureBossGameData.ClaimableReward bountyRewards)
        {
            foreach (var item in expectedRewardItems)
            {
                item.gameObject.SetActive(false);
            }

            var i = 0;
            foreach (var item in bountyRewards.ItemReward)
            {
                expectedRewardItems[i].gameObject.SetActive(true);
                expectedRewardItems[i].gameObject.transform.DORewind();
                expectedRewardItems[i].gameObject.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
                expectedRewardItems[i].ItemViewSetItemData(item.Key, item.Value);
                i++;
            }

            foreach (var item in bountyRewards.FavReward)
            {
                expectedRewardItems[i].gameObject.SetActive(true);
                if (expectedRewardItems[i].ItemViewSetCurrencyData(item.Key, item.Value))
                {
                    expectedRewardItems[i].gameObject.transform.DORewind();
                    expectedRewardItems[i].gameObject.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
                    i++;
                }
            }
        }

        private void SetBossData(int bossId)
        {
            if (_bossId != bossId)
            {
                if (_bossImage != null)
                {
                    DestroyImmediate(_bossImage);
                }

                _bossId = bossId;
                _bossImage = Instantiate(SpriteHelper.GetBigCharacterIconBody(_bossId),
                    bossImgRoot);
                _bossImage.transform.localPosition = Vector3.zero;
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesByEnable.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        public void OnClickConfirm()
        {
            if (!int.TryParse(bountyInputArea.text, out var bounty))
            {
                NcDebug.LogError("[AdventureBossEnterBountyPopup] OnClickConfirm: Invalid bounty");
                return;
            }

            if (States.Instance.StakingLevel < States.Instance.GameConfigState.AdventureBossWantedRequiredStakingLevel)
            {
                NcDebug.LogError("[AdventureBossEnterBountyPopup] OnClickConfirm: Staking level is not enough");
                return;
            }

            AudioController.instance.PlaySfx(AudioController.SfxCode.Cash);
            switch (Game.instance.AdventureBossData.CurrentState.Value)
            {
                case Model.AdventureBossData.AdventureBossSeasonState.Ready:
                    Find<WorldMap>().SetAdventureBossButtonLoading(true);
                    try
                    {
                        ActionManager.Instance.Wanted(Game.instance.AdventureBossData.SeasonInfo.Value.Season + 1, new FungibleAssetValue(ActionRenderHandler.Instance.GoldCurrency, bounty, 0));
                    }
                    catch
                    {
                        Find<WorldMap>().SetAdventureBossButtonLoading(false);
                    }

                    Close();
                    break;
                case Model.AdventureBossData.AdventureBossSeasonState.Progress:
                    Find<AdventureBoss>().SetBountyLoadingIndicator(true);
                    try
                    {
                        ActionManager.Instance.Wanted(Game.instance.AdventureBossData.SeasonInfo.Value.Season, new FungibleAssetValue(ActionRenderHandler.Instance.GoldCurrency, bounty, 0));
                    }
                    catch
                    {
                        Find<AdventureBoss>().SetBountyLoadingIndicator(false);
                    }

                    Close();
                    break;
                case Model.AdventureBossData.AdventureBossSeasonState.None:
                case Model.AdventureBossData.AdventureBossSeasonState.End:
                default:
                    NcDebug.LogError("[AdventureBossEnterBountyPopup] Show: Invalid state");
                    return;
            }
        }
    }
}
