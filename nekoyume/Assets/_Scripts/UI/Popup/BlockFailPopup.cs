using Assets.SimpleLocalization;
using System.Collections;
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
            var errorMsg = string.Format(LocalizationManager.Localize("UI_ERROR_FORMAT"),
                LocalizationManager.Localize("BLOCK_DOWNLOAD"));

            base.Show(LocalizationManager.Localize("UI_ERROR"), errorMsg,
                LocalizationManager.Localize("UI_OK"), false);
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
