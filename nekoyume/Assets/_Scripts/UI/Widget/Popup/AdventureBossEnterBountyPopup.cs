using Nekoyume.UI.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Cysharp.Threading.Tasks;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Action.Exceptions.AdventureBoss;
    using Nekoyume.Blockchain;
    using Nekoyume.L10n;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.Model.Mail;
    using Nekoyume.State;
    using System.Runtime.CompilerServices;
    using UniRx;
    public class AdventureBossEnterBountyPopup : PopupWidget
    {
        [SerializeField]
        private TMP_InputField bountyInputArea;
        [SerializeField]
        private GameObject inputCountObj;
        [SerializeField]
        private GameObject inputWarning;
        [SerializeField]
        private ConditionalButton confirmButton;

        [SerializeField]
        private GameObject stakingWarningMassage;
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

        private Color _bountyDefaultColor;
        private readonly List<System.IDisposable> _disposablesByEnable = new();

        protected override void Awake()
        {
            bountyInputArea.onValueChanged.AddListener(OnBountyInputAreaValueChanged);
            bountyInputArea.onEndEdit.AddListener(OnBountyInputAreaValueChanged);
            _bountyDefaultColor = bountyInputArea.textComponent.color;
            confirmButton.OnSubmitSubject.Subscribe(_ => OnClickConfirm()).AddTo(gameObject);
            base.Awake();
        }

        private void OnBountyInputAreaValueChanged(string input)
        {
            
            if (string.IsNullOrEmpty(input))
            {
                inputCountObj.SetActive(false);
                additionalBountyObj.SetActive(false);
            }
            else
            {
                inputCountObj.SetActive(true);
                additionalBountyObj.SetActive(true);
            }
            if (int.TryParse(input, out int bounty))
            {
                if (bounty < 100)
                {
                    bountyInputArea.textComponent.color = bountyRedColor;
                    inputWarning.SetActive(true);
                    confirmButton.Interactable = false;
                }
                else
                {
                    bountyInputArea.textComponent.color = _bountyDefaultColor;
                    inputWarning.SetActive(false);
                    confirmButton.Interactable = true;
                }
                additionalBountyPrice.text = $"+ {bounty.ToString("#,0")}";
            }
            else
            {
                confirmButton.Interactable = false;
            }
        }

        public void ClearBountyInputField()
        {
            bountyInputArea.text = "";
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if(States.Instance.StakingLevel < Wanted.RequiredStakingLevel)
            {
                stakingWarningMassage.SetActive(true);
                bountyInputArea.gameObject.SetActive(false);
            }
            else
            {
                stakingWarningMassage.SetActive(false);
                bountyInputArea.gameObject.SetActive(true);
            }
            bountyedPrice.text = "-";
            totalBountyPrice.text = "-";
            bountyCount.text = "-";
            ClearBountyInputField();
            switch (Game.Game.instance.AdventureBossData.CurrentState.Value)
            {
                case Model.AdventureBossData.AdventureBossSeasonState.Ready:
                    bountyedPrice.text = "0";
                    totalBountyPrice.text = "0";
                    bountyCount.text = $"The bounty I placed ({0}/3)";
                    showDetailButton.SetActive(false);
                    break;
                case Model.AdventureBossData.AdventureBossSeasonState.Progress:
                    showDetailButton.SetActive(true);
                    var currentBountyInfo = Game.Game.instance.AdventureBossData.GetCurrentInvestorInfo();
                    if (currentBountyInfo != null)
                    {
                        bountyedPrice.text = currentBountyInfo.Price.ToCurrencyNotation();
                        bountyCount.text = $"The bounty I placed ({currentBountyInfo.Count}/3)";
                    }
                    else
                    {
                        bountyedPrice.text = "0";
                        bountyCount.text = $"The bounty I placed ({currentBountyInfo.Count}/3)";
                    }
                    totalBountyPrice.text = Game.Game.instance.AdventureBossData.GetCurrentBountyPrice().MajorUnit.ToString("#,0");
                    break;
                case Model.AdventureBossData.AdventureBossSeasonState.None:
                case Model.AdventureBossData.AdventureBossSeasonState.End:
                default:
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_INVALID"), Scroller.NotificationCell.NotificationType.Alert);  
                    NcDebug.LogError("[AdventureBossEnterBountyPopup] Show: Invalid state");
                    return;
            }

            base.Show(ignoreShowAnimation);

            Game.Game.instance.AdventureBossData.CurrentState.Subscribe(state =>
            {
                if(state == Model.AdventureBossData.AdventureBossSeasonState.None ||
                    state == Model.AdventureBossData.AdventureBossSeasonState.End)
                {
                    Close();
                }
            }).AddTo(_disposablesByEnable);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesByEnable.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        public void OnClickConfirm()
        {
            if (!int.TryParse(bountyInputArea.text, out int bounty))
            {
                NcDebug.LogError("[AdventureBossEnterBountyPopup] OnClickConfirm: Invalid bounty");
                return;
            }

            if(States.Instance.StakingLevel < Wanted.RequiredStakingLevel)
            {
                NcDebug.LogError("[AdventureBossEnterBountyPopup] OnClickConfirm: Staking level is not enough");
                return;
            }

            switch (Game.Game.instance.AdventureBossData.CurrentState.Value)
            {
                case Model.AdventureBossData.AdventureBossSeasonState.Ready:
                    Find<WorldMap>().SetAdventureBossButtonLoading(true);
                    try
                    {
                        ActionManager.Instance.Wanted(Game.Game.instance.AdventureBossData.LatestSeason.Value.SeasonId + 1, new FungibleAssetValue(ActionRenderHandler.Instance.GoldCurrency, bounty, 0))
                            .Subscribe(eval =>
                            {
                                if(eval.Exception != null)
                                {
                                    NcDebug.LogError(eval.Exception);
                                    Find<WorldMap>().SetAdventureBossButtonLoading(false);
                                    if(eval.Exception.InnerException is PreviousBountyException)
                                    {
                                        OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_PREVIUS_BOUNTY_EXCEPTION"), Scroller.NotificationCell.NotificationType.Alert);
                                    }
                                    else
                                    {
                                        OneLineSystem.Push(MailType.System, eval.Exception.InnerException.Message, Scroller.NotificationCell.NotificationType.Alert);
                                    }
                                    return;
                                }

                                Game.Game.instance.AdventureBossData.RefreshAllByCurrentState().ContinueWith(() =>
                                {
                                    Find<WorldMap>().SetAdventureBossButtonLoading(false);
                                });
                            });
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
                        ActionManager.Instance.Wanted(Game.Game.instance.AdventureBossData.LatestSeason.Value.SeasonId, new FungibleAssetValue(ActionRenderHandler.Instance.GoldCurrency, bounty, 0))
                            .Subscribe(eval =>
                            {
                                if (eval.Exception != null)
                                {
                                    NcDebug.LogError(eval.Exception);
                                    Find<AdventureBoss>().SetBountyLoadingIndicator(false);
                                    if (eval.Exception is PreviousBountyException)
                                    {
                                        OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_PREVIUS_BOUNTY_EXCEPTION"), Scroller.NotificationCell.NotificationType.Alert);
                                    }
                                    else
                                    {
                                        OneLineSystem.Push(MailType.System, eval.Exception.ToString(), Scroller.NotificationCell.NotificationType.Alert);
                                    }
                                    return;
                                }
                                Game.Game.instance.AdventureBossData.RefreshAllByCurrentState().ContinueWith(() =>
                                {
                                    Find<AdventureBoss>().SetBountyLoadingIndicator(false);
                                });
                            });
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

