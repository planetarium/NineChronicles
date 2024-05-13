using Nekoyume.UI.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
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
        private Color bountyRedColor;
        private Color bountyDefaultColor;

        private void Awake()
        {
            BountyInputArea.onValueChanged.AddListener(OnBountyInputAreaValueChanged);
            BountyInputArea.onEndEdit.AddListener(OnBountyInputAreaValueChanged);
            bountyDefaultColor = BountyInputArea.textComponent.color;
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
                    BountyInputArea.textComponent.color = bountyDefaultColor;
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
            base.Show(ignoreShowAnimation);
        }

        public void OnClickConfirm()
        {
            NcDebug.Log("OnClickConfirm");
        }
    }
}

