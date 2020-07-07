using System;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class HelpButton : NormalButton
    {
        [SerializeField]
        private int helpId = default;

        [SerializeField]
        private bool showOnceForEachAgentAddress = default;

        public int HelpId
        {
            get => helpId;
            set => helpId = value;
        }

        public bool ShowOnceForEachAgentAddress
        {
            get => showOnceForEachAgentAddress;
            set => showOnceForEachAgentAddress = value;
        }

        protected override void Awake()
        {
            base.Awake();
            OnClick
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ => HelpPopup.HelpMe(helpId, showOnceForEachAgentAddress))
                .AddTo(gameObject);
        }
    }
}
