using Nekoyume.EnumType;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Rank : Widget
    {
        public override WidgetType WidgetType => WidgetType.Tooltip;

        [SerializeField]
        private Button closeButton = null;

        [SerializeField]
        private RankPanel rankPanel = null;

        public override void Initialize()
        {
            base.Initialize();
            rankPanel.Initialize();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            rankPanel.Show();
        }
    }
}
