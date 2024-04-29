using System.Collections.Generic;
using Nekoyume.EnumType;
using mixpanel;
using Nekoyume.Game.Battle;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Nekoyume.L10n;

namespace Nekoyume.UI
{
    public class TitleOneButtonSystem : Alert
    {
        [SerializeField]
        private Button CloseButton;

        public override WidgetType WidgetType => WidgetType.System;

        public override void Show(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
            Analyzer.Instance.Track("Unity/SystemPopupImpression");

            var evt = new AirbridgeEvent("System_Popup_Impression");
            evt.SetValue(Game.Game.instance.Stage.stageId);
            AirbridgeUnity.TrackEvent(evt);

            if (BattleRenderer.Instance.IsOnBattle)
            {
                var props = new Dictionary<string, Value>()
                {
                    ["StageId"] = Game.Game.instance.Stage.stageId,
                };
                Analyzer.Instance.Track("Unity/Stage Exit Crash", props);

                var crashEvt = new AirbridgeEvent("Stage_Exit_Crash");
                evt.SetValue(Game.Game.instance.Stage.stageId);
                AirbridgeUnity.TrackEvent(crashEvt);
            }

            base.Show(title, content, labelOK, localize);
        }

        public void Set(string title, string content, bool hasCloseBtn, string labelOK = "UI_OK", bool localize = true, float blurSize = 1)
        {
            base.Set(title, content, labelOK, localize, blurSize);
            CloseButton.gameObject.SetActive(hasCloseBtn);
            CloseButton.onClick.AddListener(()=> {
                Close(true);
            });
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            CloseButton.onClick.RemoveAllListeners();
        }

        public void ShowAndQuit(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
#if UNITY_EDITOR
            SubmitCallback = UnityEditor.EditorApplication.ExitPlaymode;
#else
            SubmitCallback = UnityEngine.Application.Quit;
#endif
            Show(title, content, labelOK, localize);
        }
    }
}
