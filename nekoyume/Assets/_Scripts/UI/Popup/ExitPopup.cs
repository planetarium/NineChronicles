using System.Collections;
using Nekoyume.BlockChain;
using UnityEngine;

namespace Nekoyume.UI
{
    public class ExitPopup : SystemPopup
    {
        private long _index;
        private Coroutine _coroutine;

        public void Show(long idx)
        {
            CloseCallback = Application.Quit;
            _index = idx;
            base.Show();
            _coroutine = StartCoroutine(CoCheckBlockIndex());
        }

        private IEnumerator CoCheckBlockIndex()
        {
            yield return new WaitWhile(() => AgentController.Agent.BlockIndex == _index);
            CloseCallback = null;
            Close();
        }

        public override void Close()
        {
            if (!(_coroutine is null))
                StopCoroutine(_coroutine);

            base.Close();
        }
    }
}
