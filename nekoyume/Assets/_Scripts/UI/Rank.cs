using Nekoyume.EnumType;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Rank : Widget
    {
        public override WidgetType WidgetType => WidgetType.Tooltip;

        [SerializeField]
        private Button closeButton = null;


    }
}
