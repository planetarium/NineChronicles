using System;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;
    public class HelpButton : MonoBehaviour
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private int helpId = default;

        [SerializeField]
        private bool showOnceForEachAgentAddress = default;

        public readonly Subject<HelpButton> OnClick = new Subject<HelpButton>();

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

        private void Awake()
        {
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClick.OnNext(this);
            }).AddTo(gameObject);

            OnClick
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ => HelpTooltip.HelpMe(helpId, showOnceForEachAgentAddress))
                .AddTo(gameObject);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
