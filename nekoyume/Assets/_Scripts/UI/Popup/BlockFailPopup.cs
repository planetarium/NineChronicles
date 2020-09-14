using System.Collections;
using Nekoyume.L10n;
using UnityEngine;

namespace Nekoyume.UI
{
    public class BlockFailPopup : SystemPopup
    {
        private long _index;

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
            _index = idx;
            StartCoroutine(CoCheckBlockIndex());
        }

        private IEnumerator CoCheckBlockIndex()
        {
            yield return new WaitWhile(() => Game.Game.instance.Agent.BlockIndex == _index);
            CloseCallback = null;
            Close();
        }
    }
}
