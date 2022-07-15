using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class WorldBossReward : Widget
    {
        [SerializeField]
        private Button backButton;

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = () =>
            {
                Close(true);
            };

            backButton.OnClickAsObservable().Subscribe(_ =>
            {
                Close(true);
            }).AddTo(gameObject);
        }
    }
}
