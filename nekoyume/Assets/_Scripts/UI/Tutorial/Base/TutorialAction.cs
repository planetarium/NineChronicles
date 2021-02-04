using System.Reflection;
using UnityEngine;

namespace Nekoyume.UI
{
    public class TutorialAction
    {
        public Widget ActionWidget { get; }
        public MethodInfo ActionMethodInfo { get; }

        public TutorialAction(Widget widget, MethodInfo methodInfo)
        {
            ActionWidget = widget;
            ActionMethodInfo = methodInfo;
        }
    }
}
