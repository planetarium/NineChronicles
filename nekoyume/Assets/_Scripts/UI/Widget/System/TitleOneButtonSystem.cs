using Nekoyume.EnumType;
using mixpanel;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Nekoyume.UI
{
    public class TitleOneButtonSystem : Alert
    {
        public override WidgetType WidgetType => WidgetType.System;

        [SerializeField]
        private Button bgImageButton = null;

        protected override void Awake()
        {
            base.Awake();
            bgImageButton.OnClickAsObservable()
                .Subscribe(_ => Close())
                .AddTo(gameObject);
        }

        public override void Show(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
            Analyzer.Instance.Track("Unity/SystemPopupImpression");
            if (Game.Game.instance.Stage.IsInStage)
            {
                var props = new Value
                {
                    ["StageId"] = Game.Game.instance.Stage.stageId,
                };
                Analyzer.Instance.Track("Unity/Stage Exit Crash", props);
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
