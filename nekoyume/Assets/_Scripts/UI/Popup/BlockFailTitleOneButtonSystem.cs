using System.Collections;
using Nekoyume.L10n;
using UnityEngine;

namespace Nekoyume.UI
{
    public class BlockFailTitleOneButtonSystem : TitleOneButtonSystem
    {
        protected override void Awake()
        {
            base.Awake();

            SubmitWidget = () => Close();
        }

        public void Show(long idx)
        {
            var errorMsg = string.Format(L10nManager.Localize("UI_ERROR_FORMAT"),
                L10nManager.Localize("BLOCK_DOWNLOAD"));

            base.Show(L10nManager.Localize("UI_ERROR"), errorMsg,
                L10nManager.Localize("UI_OK"), false);
            StartCoroutine(CoCheckBlockIndex(idx));
#if UNITY_EDITOR
            CloseCallback = UnityEditor.EditorApplication.ExitPlaymode;
#else
            CloseCallback = () => Application.Quit(21);
#endif
        }

        private IEnumerator CoCheckBlockIndex(long blockIndex)
        {
            yield return new WaitWhile(() => Game.Game.instance.Agent.BlockIndex == blockIndex);
            CloseCallback = null;
            Close();
        }
    }
}
