using System.Collections;
using Nekoyume.BlockChain;
using UnityEngine;

namespace Nekoyume.UI
{
    public class ExitPopup : SystemPopup
    {
        private long _index;

        public void Show(long idx)
        {
            SubmitCallback = Application.Quit;
            _index = idx;
            base.Show();
            StartCoroutine(CoCheckBlockIndex());
        }

        private IEnumerator CoCheckBlockIndex()
        {
            yield return new WaitWhile(() => Game.Game.instance.agent.BlockIndex == _index);
            SubmitCallback = null;
            Close();
        }
    }
}
