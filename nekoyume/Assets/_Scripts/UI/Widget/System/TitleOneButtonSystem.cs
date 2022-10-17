using System.Collections.Generic;
using Nekoyume.EnumType;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Nekoyume.UI
{
    public class TitleOneButtonSystem : Alert
    {
        public override WidgetType WidgetType => WidgetType.System;

        public override void Show(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
            Analyzer.Instance.Trace("Unity/SystemPopupImpression");
            if (Game.Game.instance.IsInWorld)
            {
                var props = new Dictionary<string, string>()
                {
                    ["StageId"] = Game.Game.instance.Stage.stageId.ToString(),
                };
                Analyzer.Instance.Trace("Unity/Stage Exit Crash", props);
            }

            base.Show(title, content, labelOK, localize);
        }

        public void ShowAndQuit(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
#if UNITY_EDITOR
            CloseCallback = UnityEditor.EditorApplication.ExitPlaymode;
#else
            CloseCallback = UnityEngine.Application.Quit;
#endif
            Show(title, content, labelOK, localize);
        }
    }
}
