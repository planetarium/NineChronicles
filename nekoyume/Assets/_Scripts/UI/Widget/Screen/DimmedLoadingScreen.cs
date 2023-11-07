using System;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class DimmedLoadingScreen : ScreenWidget
    {
        public override WidgetType WidgetType => WidgetType.System;

        [SerializeField]
        private TMP_Text messageText;
    }
}
