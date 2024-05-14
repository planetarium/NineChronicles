using Nekoyume.UI.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Libplanet.Types.Assets;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Blockchain;
    using Nekoyume.State;
    using UniRx;
    public class AdventureBossEnterBountyPopup : PopupWidget
    {
        [SerializeField]
        private TMP_InputField BountyInputArea;
        [SerializeField]
        private GameObject InputCountObj;
        [SerializeField]
        private GameObject InputWarning;
        [SerializeField]
        private ConditionalButton ConfirmButton;

        [SerializeField]
        private GameObject StakingWarningMassage;
        [SerializeField]
        private GameObject ShowDetailButton;

        [SerializeField]
        private Color bountyRedColor;

        private Color _bountyDefaultColor;

        private void Awake()
        {
            BountyInputArea.onValueChanged.AddListener(OnBountyInputAreaValueChanged);
            BountyInputArea.onEndEdit.AddListener(OnBountyInputAreaValueChanged);
            _bountyDefaultColor = BountyInputArea.textComponent.color;
            ConfirmButton.OnSubmitSubject.Subscribe(_ => OnClickConfirm()).AddTo(gameObject);
        }

        private void OnBountyInputAreaValueChanged(string input)
        {
            
            if (string.IsNullOrEmpty(input))
            {
                InputCountObj.SetActive(false);
            }
            else
            {
                InputCountObj.SetActive(true);
            }
            if (int.TryParse(input, out int bounty))
            {
                if (bounty < 100)
                {
                    BountyInputArea.textComponent.color = bountyRedColor;
                    InputWarning.SetActive(true);
                    ConfirmButton.Interactable = false;
                }
                else
                {
                    BountyInputArea.textComponent.color = _bountyDefaultColor;
                    InputWarning.SetActive(false);
                    ConfirmButton.Interactable = true;
                }
            }
            else
            {
                ConfirmButton.Interactable = false;
            }
        }

        public void ClearBountyInputField()
        {
            BountyInputArea.text = "";
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if(States.Instance.StakingLevel < Wanted.RequiredStakingLevel)
            {
                StakingWarningMassage.SetActive(true);
                BountyInputArea.gameObject.SetActive(false);
                return;
            }
            else
            {
                StakingWarningMassage.SetActive(false);
                BountyInputArea.gameObject.SetActive(true);
            }

            switch (Game.Game.instance.AdventureBossData.CurrentState.Value)
            {
                case Model.AdventureBossData.AdventureBossSeasonState.Ready:
                    ShowDetailButton.SetActive(false);
                    break;
                case Model.AdventureBossData.AdventureBossSeasonState.Progress:
                    ShowDetailButton.SetActive(true);
                    break;
                case Model.AdventureBossData.AdventureBossSeasonState.None:
                case Model.AdventureBossData.AdventureBossSeasonState.End:
                default:
                    NcDebug.LogError("[AdventureBossEnterBountyPopup] Show: Invalid state");
                    return;
            }

            base.Show(ignoreShowAnimation);
        }

        public void OnClickConfirm()
        {
            if (!int.TryParse(BountyInputArea.text, out int bounty))
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
                    ActionManager.Instance.Wanted(Game.Game.instance.AdventureBossData.LatestSeason.Value.SeasonId + 1, new FungibleAssetValue(ActionRenderHandler.Instance.GoldCurrency, bounty, 0));
                    break;
                case Model.AdventureBossData.AdventureBossSeasonState.Progress:
                    ActionManager.Instance.Wanted(Game.Game.instance.AdventureBossData.LatestSeason.Value.SeasonId, new FungibleAssetValue(ActionRenderHandler.Instance.GoldCurrency, bounty, 0));
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

