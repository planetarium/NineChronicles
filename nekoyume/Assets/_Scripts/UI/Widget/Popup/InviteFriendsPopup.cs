using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class InviteFriendsPopup : PopupWidget
    {
        [Serializable]
        private struct EnterReferralCodePopup
        {
            public GameObject gameObject;
            public Button closeButton;
            public Button enterButton;
            public TMP_InputField inputField;
        }

        [Serializable]
        private struct ErrorPopup
        {
            public GameObject gameObject;
            public Button closeButton;
            public TextMeshProUGUI text;
        }

        [SerializeField]
        private TextMeshProUGUI referralRewardText;

        [SerializeField]
        private TextMeshProUGUI referralCodeText;

        [SerializeField]
        private Button copyReferralCodeButton;

        [SerializeField]
        private Button shareReferralCodeButton;

        [SerializeField]
        private Button enterReferralCodeButton;

        [SerializeField]
        private EnterReferralCodePopup enterReferralCodePopup;

        [SerializeField]
        private ErrorPopup errorPopup;

    }
}
