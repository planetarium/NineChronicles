using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CodeRewardButton : Widget
    {
        [SerializeField] private Button button = null;
        [SerializeField] private TextMeshProUGUI count = null;

        private System.Action _effectHandler;

        protected override void Awake()
        {
            base.Awake();
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                _effectHandler?.Invoke();
            }).AddTo(gameObject);
        }

        public void Show(System.Action handler, int rewardCount)
        {
            _effectHandler = handler;
            count.text = rewardCount.ToString();
            base.Show();
        }
    }
}
