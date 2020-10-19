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
        [SerializeField] private Canvas sortingGroup = null;
        [SerializeField] private TextMeshProUGUI count;

        private System.Action effectHandler;

        protected override void Awake()
        {
            base.Awake();
            sortingGroup.sortingLayerName = "UI";
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                effectHandler?.Invoke();
            }).AddTo(gameObject);
        }

        public void Show(System.Action handler, int rewardCount)
        {
            effectHandler = handler;
            count.text = rewardCount.ToString();
            base.Show();
        }
    }
}
