using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena
{
    using UniRx;

    public class ArenaJoinBottomButtons : MonoBehaviour
    {
        [SerializeField]
        private Button _joinButton;

        [SerializeField]
        private Button _paymentButton;

        [SerializeField]
        private Button _buyTicketsButton;

        [SerializeField]
        private Button _earlyPaymentButton;

        private void Awake()
        {
            _joinButton.onClick.AsObservable().Subscribe().AddTo(gameObject);
            _paymentButton.onClick.AsObservable().Subscribe().AddTo(gameObject);
            _buyTicketsButton.onClick.AsObservable().Subscribe().AddTo(gameObject);
            _earlyPaymentButton.onClick.AsObservable().Subscribe().AddTo(gameObject);
        }
    }
}
