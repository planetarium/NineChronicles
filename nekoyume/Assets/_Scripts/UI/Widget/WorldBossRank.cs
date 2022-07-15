using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class WorldBossRank : Widget
    {
        [SerializeField]
        private Button backButton;

        protected override void Awake()
        {
            base.Awake();

            backButton.OnClickAsObservable().Subscribe(_ =>
            {
                Close(true);
            }).AddTo(gameObject);

            CloseWidget = () =>
            {
                Close(true);
            };
        }
    }
}
