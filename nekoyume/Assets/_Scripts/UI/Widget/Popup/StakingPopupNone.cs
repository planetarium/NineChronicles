using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StakingPopupNone : PopupWidget
    {
        [SerializeField] private ConditionalButton uploadButton;
        [SerializeField] private Button closeButton;

        protected override void Awake()
        {
            base.Awake();

            uploadButton.OnSubmitSubject.Subscribe(_ =>
            {
                // Do Something
                AudioController.PlayClick();
            }).AddTo(gameObject);

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });
        }
    }
}
