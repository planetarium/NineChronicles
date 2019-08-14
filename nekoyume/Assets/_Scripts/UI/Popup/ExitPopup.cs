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
            CloseCallback = Application.Quit;
            _index = idx;
            base.Show();
            StartCoroutine(CoCheckBlockIndex());
        }

        private IEnumerator CoCheckBlockIndex()
        {
            yield return new WaitWhile(() => AgentController.Agent.BlockIndex == _index);
            CloseCallback = null;
            Close();
        }
    }
}
