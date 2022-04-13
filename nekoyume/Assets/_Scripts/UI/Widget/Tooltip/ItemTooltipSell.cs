using Nekoyume.Game.Controller;
using Nekoyume.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class ItemTooltipSell : MonoBehaviour
    {
        [SerializeField]
        private BlockTimer timer;

        [SerializeField]
        private Button retrieveButton;

        [SerializeField]
        private Button registerButton;

        public void Set(long expiredBlockIndex, System.Action onRetrieve, System.Action onRegister)
        {
            retrieveButton.onClick.RemoveAllListeners();
            retrieveButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                onRetrieve?.Invoke();
            });

            registerButton.onClick.RemoveAllListeners();
            registerButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                onRegister?.Invoke();
            });
            timer.UpdateTimer(expiredBlockIndex);
        }
    }
}
